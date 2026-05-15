using System.Text;
using System.Text.RegularExpressions;
using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobApplicationAssistant.Api.Endpoints;

public static class ProfileEndpoints
{
    private const long MaxImportFileBytes = 10 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        group.MapPost("/imports/pdf-cv", async Task<IResult> (IFormFile? file, ApplicationDbContext db, IAiProvider aiProvider, CancellationToken ct) =>
        {
            var errors = ValidatePdfImport(file);
            if (errors.Count > 0)
            {
                return Results.BadRequest(ApiError.Validation(errors));
            }

            await using var stream = file!.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, ct);
            var pdfBytes = memory.ToArray();

            if (!HasPdfHeader(pdfBytes))
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(file)] = ["Uploaded CV must be a PDF file."]
                }));
            }

            var extractedText = ExtractPdfText(pdfBytes);
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(file)] = ["Uploaded PDF did not contain extractable CV text."]
                }));
            }

            AssistedProfileImportResult result;
            try
            {
                result = await aiProvider.ImportProfileFactsAsync(new AssistedProfileImportInput(file.FileName, extractedText), ct);
            }
            catch (AiProviderException exception)
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    ["AiProvider"] = [exception.Message]
                }));
            }

            var now = DateTimeOffset.UtcNow;
            var importSessionId = Guid.NewGuid();
            var sourceDocumentIds = JsonSerializer.Serialize(new[] { importSessionId.ToString("N") }, JsonOptions);
            var facts = result.Facts
                .Select(importedFact => ToDraftImportedFact(importedFact, file.FileName, extractedText, importSessionId, sourceDocumentIds, now))
                .ToList();

            db.ProfileFacts.AddRange(facts);
            await db.SaveChangesAsync(ct);

            return Results.Created(
                $"/api/profile/imports/{importSessionId:N}",
                new AssistedProfileImportResponse(
                    importSessionId,
                    file.FileName,
                    facts.Count,
                    facts.Select(ToResponse).ToList(),
                    "/profile/facts?status=Draft"));
        }).DisableAntiforgery();

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
            fact.UpdatedAt,
            fact.SourceDocumentIds,
            fact.OriginalImportedSnapshot,
            fact.ManuallyEdited);

    private static ProfileFact ToDraftImportedFact(
        AssistedProfileImportFact importedFact,
        string fileName,
        string extractedText,
        Guid importSessionId,
        string sourceDocumentIds,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Type = NormalizeRequired(importedFact.Type, "ImportedCv", 80),
            Title = NormalizeRequired(importedFact.Title, "Imported CV fact", 200),
            Summary = NormalizeRequired(importedFact.Summary, "Draft fact imported from PDF CV.", 4000),
            Status = ProfileFactStatus.Draft,
            FactItems = SerializeJsonArray(importedFact.FactItems),
            Technologies = SerializeJsonArray(importedFact.Technologies),
            AllowedClaims = SerializeJsonArray(importedFact.AllowedClaims),
            ForbiddenClaims = SerializeJsonArray(importedFact.ForbiddenClaims),
            SourceDocumentIds = sourceDocumentIds,
            OriginalImportedSnapshot = JsonSerializer.Serialize(new
            {
                importSessionId,
                fileName,
                importedAt = now,
                sourceContext = importedFact.SourceContext,
                extractedTextPreview = extractedText.Length <= 4000 ? extractedText : extractedText[..4000]
            }, JsonOptions),
            ManuallyEdited = false,
            CreatedAt = now,
            UpdatedAt = now
        };

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

    private static Dictionary<string, string[]> ValidatePdfImport(IFormFile? file)
    {
        var errors = new Dictionary<string, string[]>();
        if (file is null || file.Length == 0)
        {
            errors[nameof(file)] = ["A PDF CV file is required."];
            return errors;
        }

        if (file.Length > MaxImportFileBytes)
        {
            errors[nameof(file)] = ["Uploaded CV must be 10 MB or smaller."];
        }

        var hasPdfExtension = Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!hasPdfExtension)
        {
            errors[nameof(file)] = ["Uploaded CV must be a PDF file."];
        }

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

    private static string NormalizeRequired(string value, string fallback, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string SerializeJsonArray(IReadOnlyList<string> values) =>
        JsonSerializer.Serialize(values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList(), JsonOptions);

    private static bool HasPdfHeader(byte[] bytes) =>
        bytes.Length >= 5 && Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-";

    private static string ExtractPdfText(byte[] bytes)
    {
        var text = Encoding.Latin1.GetString(bytes);
        var values = Regex.Matches(text, @"\((?<text>(?:\\.|[^\\)])*)\)")
            .Select(match => DecodePdfLiteralString(match.Groups["text"].Value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        if (values.Count == 0)
        {
            values = Regex.Matches(text, @"[A-Za-z0-9][A-Za-z0-9 .,;:/+#@&_\-]{8,}")
                .Select(match => match.Value.Trim())
                .Where(value => !value.StartsWith("obj", StringComparison.OrdinalIgnoreCase) &&
                    !value.StartsWith("endobj", StringComparison.OrdinalIgnoreCase) &&
                    !value.StartsWith("stream", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Regex.Replace(string.Join(' ', values), @"\s+", " ").Trim();
    }

    private static string DecodePdfLiteralString(string value) =>
        Regex.Replace(value, @"\\([nrtbf\\()])", match => match.Groups[1].Value switch
        {
            "n" => "\n",
            "r" => "\r",
            "t" => "\t",
            "b" => "\b",
            "f" => "\f",
            "\\" => "\\",
            "(" => "(",
            ")" => ")",
            _ => match.Value
        });

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
