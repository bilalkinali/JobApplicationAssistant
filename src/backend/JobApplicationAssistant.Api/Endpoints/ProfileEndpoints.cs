using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobApplicationAssistant.Api.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile");

        group.MapGet(string.Empty, async (ApplicationDbContext db, CancellationToken ct) =>
        {
            var profile = await db.Profiles
                .OrderBy(profile => profile.CreatedAt)
                .FirstOrDefaultAsync(ct);

            return Results.Ok(profile is null ? EmptyProfile() : ToResponse(profile));
        });

        group.MapPut(string.Empty, async Task<IResult> (ProfileRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.BadRequest(ApiError.Validation(errors));
            }

            var now = DateTimeOffset.UtcNow;
            var profile = await db.Profiles
                .OrderBy(profile => profile.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (profile is null)
            {
                profile = new Profile
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = now
                };
                db.Profiles.Add(profile);
            }

            profile.FullName = request.FullName.Trim();
            profile.Email = request.Email.Trim();
            profile.Phone = NormalizeOptional(request.Phone);
            profile.Location = NormalizeOptional(request.Location);
            profile.LinkedInUrl = NormalizeOptional(request.LinkedInUrl);
            profile.GitHubUrl = NormalizeOptional(request.GitHubUrl);
            profile.PortfolioUrl = NormalizeOptional(request.PortfolioUrl);
            profile.DefaultLanguage = request.DefaultLanguage.Trim();
            profile.DanishTone = NormalizeOptional(request.DanishTone);
            profile.EnglishTone = NormalizeOptional(request.EnglishTone);
            profile.UpdatedAt = now;

            await db.SaveChangesAsync(ct);

            return Results.Ok(ToResponse(profile));
        });

        group.MapGet("/facts", async (ApplicationDbContext db, CancellationToken ct) =>
        {
            var facts = await db.ProfileFacts
                .OrderBy(fact => fact.Status)
                .ThenByDescending(fact => fact.UpdatedAt)
                .Select(fact => ToResponse(fact))
                .ToListAsync(ct);

            return Results.Ok(facts);
        });

        group.MapPost("/facts", async Task<IResult> (ProfileFactRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.BadRequest(ApiError.Validation(errors));
            }

            var now = DateTimeOffset.UtcNow;
            var fact = new ProfileFact
            {
                Id = Guid.NewGuid(),
                Type = request.Type!.Trim(),
                Title = request.Title!.Trim(),
                Summary = request.Summary!.Trim(),
                Status = ParseStatus(request.Status!),
                FactItems = NormalizeJsonArray(request.FactItems),
                Technologies = NormalizeJsonArray(request.Technologies),
                AllowedClaims = NormalizeJsonArray(request.AllowedClaims),
                ForbiddenClaims = NormalizeJsonArray(request.ForbiddenClaims),
                ManuallyEdited = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.ProfileFacts.Add(fact);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/profile/facts/{fact.Id}", ToResponse(fact));
        });

        group.MapPut("/facts/{id:guid}", async Task<IResult> (Guid id, ProfileFactRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.BadRequest(ApiError.Validation(errors));
            }

            var fact = await db.ProfileFacts.FindAsync([id], ct);
            if (fact is null)
            {
                return Results.NotFound(ApiError.NotFound("Profile fact was not found."));
            }

            fact.Type = request.Type!.Trim();
            fact.Title = request.Title!.Trim();
            fact.Summary = request.Summary!.Trim();
            fact.Status = ParseStatus(request.Status!);
            fact.FactItems = NormalizeJsonArray(request.FactItems);
            fact.Technologies = NormalizeJsonArray(request.Technologies);
            fact.AllowedClaims = NormalizeJsonArray(request.AllowedClaims);
            fact.ForbiddenClaims = NormalizeJsonArray(request.ForbiddenClaims);
            fact.ManuallyEdited = true;
            fact.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);

            return Results.Ok(ToResponse(fact));
        });

        group.MapDelete("/facts/{id:guid}", async Task<IResult> (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var fact = await db.ProfileFacts.FindAsync([id], ct);
            if (fact is null)
            {
                return Results.NotFound(ApiError.NotFound("Profile fact was not found."));
            }

            db.ProfileFacts.Remove(fact);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        return app;
    }

    private static ProfileResponse EmptyProfile() =>
        new(
            Guid.Empty,
            string.Empty,
            string.Empty,
            null,
            null,
            null,
            null,
            null,
            "English",
            null,
            null,
            DateTimeOffset.MinValue,
            DateTimeOffset.MinValue);

    private static ProfileResponse ToResponse(Profile profile) =>
        new(
            profile.Id,
            profile.FullName,
            profile.Email,
            profile.Phone,
            profile.Location,
            profile.LinkedInUrl,
            profile.GitHubUrl,
            profile.PortfolioUrl,
            profile.DefaultLanguage,
            profile.DanishTone,
            profile.EnglishTone,
            profile.CreatedAt,
            profile.UpdatedAt);

    private static ProfileFactResponse ToResponse(ProfileFact fact) =>
        new(
            fact.Id,
            fact.Type,
            fact.Title,
            fact.Summary,
            fact.Status.ToString(),
            fact.FactItems,
            fact.Technologies,
            fact.AllowedClaims,
            fact.ForbiddenClaims,
            fact.CreatedAt,
            fact.UpdatedAt);

    private static Dictionary<string, string[]> Validate(ProfileRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        AddRequired(errors, nameof(request.FullName), request.FullName, 200);
        AddRequired(errors, nameof(request.Email), request.Email, 320);
        AddRequired(errors, nameof(request.DefaultLanguage), request.DefaultLanguage, 40);
        AddMaxLength(errors, nameof(request.Phone), request.Phone, 80);
        AddMaxLength(errors, nameof(request.Location), request.Location, 200);
        AddMaxLength(errors, nameof(request.LinkedInUrl), request.LinkedInUrl, 500);
        AddMaxLength(errors, nameof(request.GitHubUrl), request.GitHubUrl, 500);
        AddMaxLength(errors, nameof(request.PortfolioUrl), request.PortfolioUrl, 500);
        AddMaxLength(errors, nameof(request.DanishTone), request.DanishTone, 2000);
        AddMaxLength(errors, nameof(request.EnglishTone), request.EnglishTone, 2000);

        if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Contains('@', StringComparison.Ordinal))
        {
            errors[nameof(request.Email)] = ["Email must contain @."];
        }

        AddAbsoluteUrlError(errors, nameof(request.LinkedInUrl), request.LinkedInUrl);
        AddAbsoluteUrlError(errors, nameof(request.GitHubUrl), request.GitHubUrl);
        AddAbsoluteUrlError(errors, nameof(request.PortfolioUrl), request.PortfolioUrl);

        return errors;
    }

    private static Dictionary<string, string[]> Validate(ProfileFactRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        AddRequired(errors, nameof(request.Type), request.Type, 80);
        AddRequired(errors, nameof(request.Title), request.Title, 200);
        AddRequired(errors, nameof(request.Summary), request.Summary, 4000);

        if (!Enum.TryParse<ProfileFactStatus>(request.Status, ignoreCase: true, out _))
        {
            errors[nameof(request.Status)] = ["Status must be Draft, Approved, or Archived."];
        }

        AddJsonArrayError(errors, nameof(request.FactItems), request.FactItems);
        AddJsonArrayError(errors, nameof(request.Technologies), request.Technologies);
        AddJsonArrayError(errors, nameof(request.AllowedClaims), request.AllowedClaims);
        AddJsonArrayError(errors, nameof(request.ForbiddenClaims), request.ForbiddenClaims);

        return errors;
    }

    private static void AddRequired(Dictionary<string, string[]> errors, string field, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"{field} is required."];
            return;
        }

        AddMaxLength(errors, field, value, maxLength);
    }

    private static void AddMaxLength(Dictionary<string, string[]> errors, string field, string? value, int maxLength)
    {
        if (value?.Length > maxLength)
        {
            errors[field] = [$"{field} must be {maxLength} characters or fewer."];
        }
    }

    private static void AddAbsoluteUrlError(Dictionary<string, string[]> errors, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            errors[field] = [$"{field} must be an absolute URL."];
        }
    }

    private static void AddJsonArrayError(Dictionary<string, string[]> errors, string field, string? value)
    {
        if (!IsJsonArray(value))
        {
            errors[field] = [$"{field} must be a JSON array."];
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProfileFactStatus ParseStatus(string status) =>
        Enum.Parse<ProfileFactStatus>(status, ignoreCase: true);

    private static string NormalizeJsonArray(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "[]" : value.Trim();

    private static bool IsJsonArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
