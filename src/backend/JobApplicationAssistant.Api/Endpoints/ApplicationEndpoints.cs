using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobApplicationAssistant.Api.Endpoints;

public static class ApplicationEndpoints
{
    private static readonly string[] ValidStatuses = ["Draft", "PostingCaptured", "ReadyForReview", "Applied", "Archived"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/applications");

        group.MapGet(string.Empty, async (ApplicationDbContext db, CancellationToken ct) =>
        {
            var applications = await db.JobApplications
                .Include(application => application.GeneratedDraft)
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
                Status = NormalizeStatus(request.Status),
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
            var application = await db.JobApplications
                .Include(application => application.GeneratedDraft)
                .FirstOrDefaultAsync(application => application.Id == id, ct);
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
            application.Status = NormalizeStatus(request.Status);
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

        group.MapPost("/{id:guid}/analyze-job", async Task<IResult> (Guid id, ApplicationDbContext db, IAiProvider aiProvider, AiOptions aiOptions, CancellationToken ct) =>
        {
            var application = await db.JobApplications.FindAsync([id], ct);
            if (application is null)
            {
                return Results.NotFound(ApiError.NotFound("Application session was not found."));
            }

            if (string.IsNullOrWhiteSpace(application.JobPostingText))
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(application.JobPostingText)] = ["Job posting text is required before analysis."]
                }));
            }

            var now = DateTimeOffset.UtcNow;
            var run = new AiRun
            {
                Id = Guid.NewGuid(),
                JobApplicationId = application.Id,
                Step = "JobAnalysis",
                Provider = aiOptions.Provider,
                Model = aiOptions.Model,
                Status = "Running",
                AttemptCount = 1,
                StartedAt = now,
                InputSummary = JsonSerializer.Serialize(new
                {
                    application.CompanyName,
                    application.RoleTitle,
                    PostingLength = application.JobPostingText.Length
                }, JsonOptions)
            };
            db.AiRuns.Add(run);

            JobAnalysisResult result;
            try
            {
                result = await aiProvider.AnalyzeJobAsync(
                    new JobAnalysisInput(
                        application.CompanyName,
                        application.RoleTitle,
                        application.SelectedLanguage,
                        application.JobPostingText),
                    ct);
            }
            catch (AiProviderException exception)
            {
                run.Status = "Failed";
                run.ErrorCode = exception.ErrorCode;
                run.ErrorMessage = exception.Message;
                run.AttemptCount = exception.AttemptCount;
                run.CompletedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);

                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    ["AiProvider"] = [exception.Message]
                }));
            }

            application.CompanyName = result.CompanyName;
            application.RoleTitle = result.RoleTitle;
            application.DetectedLanguage = result.DetectedLanguage;
            application.SelectedLanguage = result.SelectedLanguage;
            application.JobSignals = JsonSerializer.Serialize(result.JobSignals, JsonOptions);
            application.EvidenceMatches = "[]";
            application.UnmatchedRequirements = "[]";
            application.ApprovedEvidence = "[]";
            application.Status = application.Status == "Draft" ? "PostingCaptured" : application.Status;
            application.UpdatedAt = DateTimeOffset.UtcNow;
            run.Status = result.AttemptCount > 1 ? "RepairedSucceeded" : "Succeeded";
            run.AttemptCount = result.AttemptCount;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.OutputSummary = JsonSerializer.Serialize(new
            {
                result.CompanyName,
                result.RoleTitle,
                SignalCount = result.JobSignals.Signals.Count
            }, JsonOptions);

            await db.SaveChangesAsync(ct);

            return Results.Ok(ToResponse(application));
        });

        group.MapPost("/{id:guid}/match-evidence", async Task<IResult> (Guid id, ApplicationDbContext db, IAiProvider aiProvider, AiOptions aiOptions, CancellationToken ct) =>
        {
            var application = await db.JobApplications.FindAsync([id], ct);
            if (application is null)
            {
                return Results.NotFound(ApiError.NotFound("Application session was not found."));
            }

            var signals = ReadJobSignals(application.JobSignals);
            if (signals.Count == 0)
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(application.JobSignals)] = ["Run job analysis before matching evidence."]
                }));
            }

            var approvedFacts = await db.ProfileFacts
                .Where(fact => fact.Status == ProfileFactStatus.Approved)
                .ToListAsync(ct);

            if (approvedFacts.Count == 0)
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    ["ProfileFacts"] = ["At least one approved profile fact is required before matching evidence."]
                }));
            }

            var run = new AiRun
            {
                Id = Guid.NewGuid(),
                JobApplicationId = application.Id,
                Step = "EvidenceMatching",
                Provider = aiOptions.Provider,
                Model = aiOptions.Model,
                Status = "Running",
                AttemptCount = 1,
                StartedAt = DateTimeOffset.UtcNow,
                InputSummary = JsonSerializer.Serialize(new
                {
                    SignalCount = signals.Count,
                    ApprovedFactCount = approvedFacts.Count
                }, JsonOptions)
            };
            db.AiRuns.Add(run);

            EvidenceMatchResult result;
            try
            {
                result = await aiProvider.MatchEvidenceAsync(new EvidenceMatchInput(signals, approvedFacts), ct);
            }
            catch (AiProviderException exception)
            {
                run.Status = "Failed";
                run.ErrorCode = exception.ErrorCode;
                run.ErrorMessage = exception.Message;
                run.AttemptCount = exception.AttemptCount;
                run.CompletedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);

                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    ["AiProvider"] = [exception.Message]
                }));
            }

            application.EvidenceMatches = JsonSerializer.Serialize(result.EvidenceMatches, JsonOptions);
            application.UnmatchedRequirements = JsonSerializer.Serialize(result.UnmatchedRequirements, JsonOptions);
            application.ApprovedEvidence = "[]";
            application.Status = application.Status is "Draft" or "PostingCaptured" ? "ReadyForReview" : application.Status;
            application.UpdatedAt = DateTimeOffset.UtcNow;
            run.Status = result.AttemptCount > 1 ? "RepairedSucceeded" : "Succeeded";
            run.AttemptCount = result.AttemptCount;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.OutputSummary = JsonSerializer.Serialize(new
            {
                MatchCount = result.EvidenceMatches.Count,
                UnmatchedCount = result.UnmatchedRequirements.Count
            }, JsonOptions);

            await db.SaveChangesAsync(ct);

            return Results.Ok(ToResponse(application));
        });

        group.MapPut("/{id:guid}/approved-evidence", async Task<IResult> (Guid id, ApprovedEvidenceRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var application = await db.JobApplications.FindAsync([id], ct);
            if (application is null)
            {
                return Results.NotFound(ApiError.NotFound("Application session was not found."));
            }

            var validation = ValidateApprovedEvidence(request, application.EvidenceMatches);
            if (validation.Errors.Count > 0)
            {
                return Results.BadRequest(ApiError.Validation(validation.Errors));
            }

            application.ApprovedEvidence = JsonSerializer.Serialize(validation.ApprovedEvidence, JsonOptions);
            application.Status = application.Status is "Draft" or "PostingCaptured" ? "ReadyForReview" : application.Status;
            application.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);

            return Results.Ok(ToResponse(application));
        });

        group.MapPost("/{id:guid}/generate-draft", async Task<IResult> (Guid id, ApplicationDbContext db, IAiProvider aiProvider, AiOptions aiOptions, CancellationToken ct) =>
        {
            var application = await db.JobApplications
                .Include(application => application.GeneratedDraft)
                .FirstOrDefaultAsync(application => application.Id == id, ct);
            if (application is null)
            {
                return Results.NotFound(ApiError.NotFound("Application session was not found."));
            }

            if (string.IsNullOrWhiteSpace(application.JobPostingText))
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(application.JobPostingText)] = ["Job posting text is required before draft generation."]
                }));
            }

            var approvedEvidence = ReadEvidenceMatches(application.ApprovedEvidence);
            if (approvedEvidence.Count == 0)
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(application.ApprovedEvidence)] = ["Approved evidence is required before draft generation."]
                }));
            }

            var profile = await db.Profiles
                .OrderBy(profile => profile.CreatedAt)
                .FirstOrDefaultAsync(ct);
            var unmatchedRequirements = ReadUnmatchedRequirements(application.UnmatchedRequirements);
            var tonePreference = string.Equals(application.SelectedLanguage, "Danish", StringComparison.OrdinalIgnoreCase)
                ? profile?.DanishTone
                : profile?.EnglishTone;
            var run = new AiRun
            {
                Id = Guid.NewGuid(),
                JobApplicationId = application.Id,
                Step = "DraftGeneration",
                Provider = aiOptions.Provider,
                Model = aiOptions.Model,
                Status = "Running",
                AttemptCount = 1,
                StartedAt = DateTimeOffset.UtcNow,
                InputSummary = JsonSerializer.Serialize(new
                {
                    ApprovedEvidenceCount = approvedEvidence.Count,
                    UnmatchedRequirementCount = unmatchedRequirements.Count
                }, JsonOptions)
            };
            db.AiRuns.Add(run);

            DraftGenerationResult result;
            try
            {
                result = await aiProvider.GenerateDraftAsync(
                    new DraftGenerationInput(
                        application.CompanyName,
                        application.RoleTitle,
                        application.SelectedLanguage,
                        profile?.FullName,
                        tonePreference,
                        approvedEvidence,
                        unmatchedRequirements),
                    ct);
            }
            catch (AiProviderException exception)
            {
                run.Status = "Failed";
                run.ErrorCode = exception.ErrorCode;
                run.ErrorMessage = exception.Message;
                run.AttemptCount = exception.AttemptCount;
                run.CompletedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);

                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    ["AiProvider"] = [exception.Message]
                }));
            }

            var now = DateTimeOffset.UtcNow;
            var draft = application.GeneratedDraft;
            if (draft is null)
            {
                draft = new GeneratedDraft
                {
                    Id = Guid.NewGuid(),
                    JobApplicationId = application.Id,
                    CreatedAt = now
                };
                db.GeneratedDrafts.Add(draft);
            }

            draft.CoverLetterText = result.CoverLetterText;
            draft.ShortMotivationText = result.ShortMotivationText;
            draft.ClaimAudit = "{}";
            draft.GeneratedAt = now;
            draft.LastEditedAt = null;
            draft.AuditUpdatedAt = null;
            draft.IsClaimAuditStale = false;
            draft.UpdatedAt = now;
            application.UpdatedAt = now;
            run.Status = result.AttemptCount > 1 ? "RepairedSucceeded" : "Succeeded";
            run.AttemptCount = result.AttemptCount;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.OutputSummary = JsonSerializer.Serialize(new
            {
                CoverLetterLength = result.CoverLetterText.Length,
                ShortMotivationLength = result.ShortMotivationText.Length
            }, JsonOptions);

            await db.SaveChangesAsync(ct);

            return Results.Ok(ToResponse(draft));
        });

        group.MapPut("/{id:guid}/generated-draft", async Task<IResult> (Guid id, GeneratedDraftEditRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var application = await db.JobApplications
                .Include(application => application.GeneratedDraft)
                .FirstOrDefaultAsync(application => application.Id == id, ct);
            if (application is null)
            {
                return Results.NotFound(ApiError.NotFound("Application session was not found."));
            }

            if (application.GeneratedDraft is null)
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(ApplicationResponse.GeneratedDraft)] = ["Generate a draft before saving manual draft edits."]
                }));
            }

            var now = DateTimeOffset.UtcNow;
            var draft = application.GeneratedDraft;
            draft.CoverLetterText = request.CoverLetterText;
            draft.ShortMotivationText = request.ShortMotivationText;
            draft.LastEditedAt = now;
            draft.IsClaimAuditStale = draft.AuditUpdatedAt is not null || draft.ClaimAudit != "{}";
            draft.UpdatedAt = now;
            application.UpdatedAt = now;

            await db.SaveChangesAsync(ct);

            return Results.Ok(ToResponse(draft));
        });

        group.MapPost("/{id:guid}/audit-claims", async Task<IResult> (Guid id, ApplicationDbContext db, IAiProvider aiProvider, AiOptions aiOptions, CancellationToken ct) =>
        {
            var application = await db.JobApplications
                .Include(application => application.GeneratedDraft)
                .FirstOrDefaultAsync(application => application.Id == id, ct);
            if (application is null)
            {
                return Results.NotFound(ApiError.NotFound("Application session was not found."));
            }

            if (application.GeneratedDraft is null)
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(ApplicationResponse.GeneratedDraft)] = ["Generate a draft before running claim audit."]
                }));
            }

            var draft = application.GeneratedDraft;
            var approvedEvidence = ReadEvidenceMatches(application.ApprovedEvidence);
            var run = new AiRun
            {
                Id = Guid.NewGuid(),
                JobApplicationId = application.Id,
                Step = "ClaimAudit",
                Provider = aiOptions.Provider,
                Model = aiOptions.Model,
                Status = "Running",
                AttemptCount = 1,
                StartedAt = DateTimeOffset.UtcNow,
                InputSummary = JsonSerializer.Serialize(new
                {
                    ApprovedEvidenceCount = approvedEvidence.Count,
                    CoverLetterLength = draft.CoverLetterText.Length,
                    ShortMotivationLength = draft.ShortMotivationText.Length
                }, JsonOptions)
            };
            db.AiRuns.Add(run);

            ClaimAuditResult result;
            try
            {
                result = await aiProvider.AuditClaimsAsync(
                    new ClaimAuditInput(
                        draft.CoverLetterText,
                        draft.ShortMotivationText,
                        approvedEvidence),
                    ct);
            }
            catch (AiProviderException exception)
            {
                run.Status = "Failed";
                run.ErrorCode = exception.ErrorCode;
                run.ErrorMessage = exception.Message;
                run.AttemptCount = exception.AttemptCount;
                run.CompletedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);

                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    ["AiProvider"] = [exception.Message]
                }));
            }

            var now = DateTimeOffset.UtcNow;

            draft.ClaimAudit = JsonSerializer.Serialize(result, JsonOptions);
            draft.AuditUpdatedAt = now;
            draft.IsClaimAuditStale = false;
            draft.UpdatedAt = now;
            application.UpdatedAt = now;
            run.Status = result.AttemptCount > 1 ? "RepairedSucceeded" : "Succeeded";
            run.AttemptCount = result.AttemptCount;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.OutputSummary = JsonSerializer.Serialize(new
            {
                ClaimCount = result.Claims.Count
            }, JsonOptions);

            await db.SaveChangesAsync(ct);

            return Results.Ok(ToResponse(draft));
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
            application.JobSignals,
            application.EvidenceMatches,
            application.UnmatchedRequirements,
            application.ApprovedEvidence,
            application.CreatedAt,
            application.UpdatedAt,
            application.GeneratedDraft is null ? null : ToResponse(application.GeneratedDraft));

    private static GeneratedDraftResponse ToResponse(GeneratedDraft draft) =>
        new(
            draft.Id,
            draft.JobApplicationId,
            draft.CoverLetterText,
            draft.ShortMotivationText,
            draft.ClaimAudit,
            draft.GeneratedAt,
            draft.LastEditedAt,
            draft.AuditUpdatedAt,
            draft.CreatedAt,
            draft.UpdatedAt,
            draft.IsClaimAuditStale);

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

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !ValidStatuses.Contains(request.Status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            errors[nameof(request.Status)] = ["Status must be Draft, PostingCaptured, ReadyForReview, Applied, or Archived."];
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

    private static string NormalizeStatus(string status) =>
        ValidStatuses.First(validStatus => string.Equals(validStatus, status.Trim(), StringComparison.OrdinalIgnoreCase));

    private static ApprovedEvidenceValidation ValidateApprovedEvidence(ApprovedEvidenceRequest request, string evidenceMatchesJson)
    {
        var errors = new Dictionary<string, string[]>();
        var approvedIds = ReadApprovedEvidenceIds(request.ApprovedEvidence, errors);
        var currentMatches = ReadEvidenceMatches(evidenceMatchesJson);

        if (errors.Count > 0)
        {
            return new ApprovedEvidenceValidation(errors, []);
        }

        var currentById = currentMatches.ToDictionary(match => match.Id, StringComparer.OrdinalIgnoreCase);
        var missingIds = approvedIds
            .Where(id => !currentById.ContainsKey(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingIds.Count > 0)
        {
            errors[nameof(request.ApprovedEvidence)] = ["Approved evidence must come from the current evidence matches."];
            return new ApprovedEvidenceValidation(errors, []);
        }

        var approvedEvidence = approvedIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(id => currentById[id])
            .ToList();

        return new ApprovedEvidenceValidation(errors, approvedEvidence);
    }

    private static IReadOnlyList<string> ReadApprovedEvidenceIds(string? value, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                errors[nameof(ApprovedEvidenceRequest.ApprovedEvidence)] = ["Approved evidence must be a JSON array."];
                return [];
            }

            var ids = new List<string>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object ||
                    !element.TryGetProperty("id", out var idProperty) ||
                    idProperty.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(idProperty.GetString()))
                {
                    errors[nameof(ApprovedEvidenceRequest.ApprovedEvidence)] = ["Each approved evidence item must include an id."];
                    return [];
                }

                ids.Add(idProperty.GetString()!.Trim());
            }

            return ids;
        }
        catch (JsonException)
        {
            errors[nameof(ApprovedEvidenceRequest.ApprovedEvidence)] = ["Approved evidence must be a JSON array."];
            return [];
        }
    }

    private static IReadOnlyList<JobSignal> ReadJobSignals(string jobSignals)
    {
        try
        {
            var document = JsonSerializer.Deserialize<JobSignalsDocument>(jobSignals, JsonOptions);
            return document?.Signals ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<EvidenceMatch> ReadEvidenceMatches(string evidenceMatches)
    {
        try
        {
            return JsonSerializer.Deserialize<List<EvidenceMatch>>(evidenceMatches, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<UnmatchedRequirement> ReadUnmatchedRequirements(string unmatchedRequirements)
    {
        try
        {
            return JsonSerializer.Deserialize<List<UnmatchedRequirement>>(unmatchedRequirements, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record ApprovedEvidenceValidation(
        Dictionary<string, string[]> Errors,
        IReadOnlyList<EvidenceMatch> ApprovedEvidence);
}
