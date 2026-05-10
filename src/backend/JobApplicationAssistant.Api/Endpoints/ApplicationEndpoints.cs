using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationAssistant.Api.Endpoints;

public static class ApplicationEndpoints
{
    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/applications");

        group.MapGet(string.Empty, async (ApplicationDbContext db, CancellationToken ct) =>
        {
            var applications = await db.JobApplications
                .OrderByDescending(application => application.UpdatedAt)
                .Select(application => ToResponse(application))
                .ToListAsync(ct);

            return Results.Ok(applications);
        });

        group.MapPost(string.Empty, async Task<IResult> (ApplicationRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.BadRequest(ApiError.Validation(errors));
            }

            var now = DateTimeOffset.UtcNow;
            var application = new JobApplication
            {
                Id = Guid.NewGuid(),
                CompanyName = request.CompanyName.Trim(),
                RoleTitle = request.RoleTitle.Trim(),
                ApplicationUrl = NormalizeOptional(request.ApplicationUrl),
                Deadline = request.Deadline,
                Status = request.Status.Trim(),
                JobPostingText = request.JobPostingText.Trim(),
                DetectedLanguage = NormalizeOptional(request.DetectedLanguage),
                SelectedLanguage = NormalizeOptional(request.SelectedLanguage),
                CreatedAt = now,
                UpdatedAt = now
            };

            db.JobApplications.Add(application);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/applications/{application.Id}", ToResponse(application));
        });

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var application = await db.JobApplications.FindAsync([id], ct);
            if (application is null)
            {
                return Results.NotFound(ApiError.NotFound("Application session was not found."));
            }

            return Results.Ok(ToResponse(application));
        });

        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, ApplicationRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.BadRequest(ApiError.Validation(errors));
            }

            var application = await db.JobApplications.FindAsync([id], ct);
            if (application is null)
            {
                return Results.NotFound(ApiError.NotFound("Application session was not found."));
            }

            application.CompanyName = request.CompanyName.Trim();
            application.RoleTitle = request.RoleTitle.Trim();
            application.ApplicationUrl = NormalizeOptional(request.ApplicationUrl);
            application.Deadline = request.Deadline;
            application.Status = request.Status.Trim();
            application.JobPostingText = request.JobPostingText.Trim();
            application.DetectedLanguage = NormalizeOptional(request.DetectedLanguage);
            application.SelectedLanguage = NormalizeOptional(request.SelectedLanguage);
            application.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);

            return Results.Ok(ToResponse(application));
        });

        group.MapDelete("/{id:guid}", async Task<IResult> (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var application = await db.JobApplications.FindAsync([id], ct);
            if (application is null)
            {
                return Results.NotFound(ApiError.NotFound("Application session was not found."));
            }

            db.JobApplications.Remove(application);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        return app;
    }

    private static ApplicationResponse ToResponse(JobApplication application) =>
        new(
            application.Id,
            application.CompanyName,
            application.RoleTitle,
            application.ApplicationUrl,
            application.Deadline,
            application.Status,
            application.JobPostingText,
            application.DetectedLanguage,
            application.SelectedLanguage,
            application.CreatedAt,
            application.UpdatedAt);

    private static Dictionary<string, string[]> Validate(ApplicationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        AddRequired(errors, nameof(request.CompanyName), request.CompanyName, 200);
        AddRequired(errors, nameof(request.RoleTitle), request.RoleTitle, 200);
        AddRequired(errors, nameof(request.Status), request.Status, 80);
        AddMaxLength(errors, nameof(request.ApplicationUrl), request.ApplicationUrl, 500);
        AddMaxLength(errors, nameof(request.DetectedLanguage), request.DetectedLanguage, 40);
        AddMaxLength(errors, nameof(request.SelectedLanguage), request.SelectedLanguage, 40);

        if (!string.IsNullOrWhiteSpace(request.ApplicationUrl) &&
            !Uri.TryCreate(request.ApplicationUrl, UriKind.Absolute, out _))
        {
            errors[nameof(request.ApplicationUrl)] = ["Application URL must be an absolute URL."];
        }

        return errors;
    }

    private static void AddRequired(Dictionary<string, string[]> errors, string field, string value, int maxLength)
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

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
