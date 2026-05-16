using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Domain;
using JobApplicationAssistant.Api.Imports;
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

            ApplyProfileFactRequest(fact, request, markEdited: true);
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

        group.MapPost("/imports/pdf-cv", async Task<IResult> (IFormFile? file, ApplicationDbContext db, IAiProvider aiProvider, IPdfTextExtractor pdfTextExtractor, AiOptions aiOptions, CancellationToken ct) =>
        {
            var errors = ValidatePdfImport(file);
            if (errors.Count > 0)
            {
                return Results.BadRequest(ApiError.Validation(errors));
            }

            await using var stream = file!.OpenReadStream();
            var extraction = await pdfTextExtractor.ExtractAsync(stream, ct);
            if (!extraction.Succeeded)
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(file)] = [extraction.Error ?? "Uploaded PDF could not be imported."]
                }));
            }

            var extractedText = extraction.Text;
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(file)] = ["Uploaded PDF did not contain importable text."]
                }));
            }

            var now = DateTimeOffset.UtcNow;
            var run = new AiRun
            {
                Id = Guid.NewGuid(),
                Step = "AssistedProfileImport",
                Provider = aiOptions.Provider,
                Model = aiOptions.Model,
                Status = "Running",
                StartedAt = now,
                InputSummary = $"PDF CV import from {file.FileName} ({extractedText.Length} extracted characters)."
            };
            db.AiRuns.Add(run);

            AssistedProfileImportResult result;
            try
            {
                result = await aiProvider.ImportProfileFactsAsync(new AssistedProfileImportInput(file.FileName, extractedText), ct);
                ValidateAssistedProfileImportResult(result);
            }
            catch (AiProviderException exception)
            {
                RecordFailedRun(run, exception);
                await db.SaveChangesAsync(ct);

                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    ["AiProvider"] = [exception.Message]
                }));
            }

            var importSessionId = Guid.NewGuid();
            var sourceDocumentIds = JsonSerializer.Serialize(new[] { importSessionId.ToString("N") }, JsonOptions);
            var existingFacts = await db.ProfileFacts.AsNoTracking().ToListAsync(ct);
            var facts = result.Facts
                .Select(importedFact => ToDraftImportedFact(importedFact, file.FileName, extractedText, importSessionId, sourceDocumentIds, now))
                .ToList();
            var reviewQueue = ToImportedDraftFactReviewQueue(importSessionId, file.FileName, facts, existingFacts);

            run.Status = "Succeeded";
            run.AttemptCount = result.AttemptCount;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.OutputSummary = $"Imported {facts.Count} draft profile fact(s).";

            db.ProfileFacts.AddRange(facts);
            await db.SaveChangesAsync(ct);

            return Results.Created(
                $"/api/profile/imports/{importSessionId:N}",
                new AssistedProfileImportResponse(
                    importSessionId,
                    file.FileName,
                    facts.Count,
                    facts.Select(ToResponse).ToList(),
                    reviewQueue,
                    "/profile/facts?status=Draft"));
        }).DisableAntiforgery();

        group.MapGet("/imports/{importSessionId:guid}/draft-facts", async Task<IResult> (Guid importSessionId, ApplicationDbContext db, CancellationToken ct) =>
        {
            var facts = await db.ProfileFacts
                .AsNoTracking()
                .OrderBy(fact => fact.Type)
                .ThenBy(fact => fact.Title)
                .ToListAsync(ct);
            var importFacts = facts
                .Where(fact => fact.Status == ProfileFactStatus.Draft && ImportedFromSession(fact, importSessionId))
                .ToList();

            if (importFacts.Count == 0)
            {
                return Results.NotFound(ApiError.NotFound("Imported draft facts were not found for this import session."));
            }

            var fileName = ImportSnapshot(importFacts[0])?.FileName ?? "Imported CV";
            return Results.Ok(ToImportedDraftFactReviewQueue(importSessionId, fileName, importFacts, facts));
        });

        group.MapPost("/imports/{importSessionId:guid}/draft-facts/{factId:guid}/decision", async Task<IResult> (Guid importSessionId, Guid factId, ImportedDraftFactDecisionRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var decision = NormalizeOptional(request.Decision)?.ToLowerInvariant();
            if (decision is not ("approve" or "archive" or "reject"))
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(request.Decision)] = ["Decision must be approve, archive, or reject."]
                }));
            }

            var fact = await db.ProfileFacts.FindAsync([factId], ct);
            if (fact is null || fact.Status != ProfileFactStatus.Draft || !ImportedFromSession(fact, importSessionId))
            {
                return Results.NotFound(ApiError.NotFound("Imported draft fact was not found for this import session."));
            }

            if (request.ProfileFact is not null)
            {
                var errors = Validate(request.ProfileFact);
                if (errors.Count > 0)
                {
                    return Results.BadRequest(ApiError.Validation(errors));
                }

                ApplyProfileFactRequest(fact, request.ProfileFact, markEdited: true);
            }

            fact.Status = decision switch
            {
                "approve" => ProfileFactStatus.Approved,
                "archive" => ProfileFactStatus.Archived,
                _ => ProfileFactStatus.Rejected
            };
            fact.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);

            var facts = await db.ProfileFacts
                .AsNoTracking()
                .OrderBy(existingFact => existingFact.Type)
                .ThenBy(existingFact => existingFact.Title)
                .ToListAsync(ct);
            var remainingImportFacts = facts
                .Where(existingFact => existingFact.Status == ProfileFactStatus.Draft && ImportedFromSession(existingFact, importSessionId))
                .ToList();
            var fileName = ImportSnapshot(fact)?.FileName ?? "Imported CV";

            return Results.Ok(new ImportedDraftFactDecisionResponse(
                ToResponse(fact),
                ToImportedDraftFactReviewQueue(importSessionId, fileName, remainingImportFacts, facts)));
        });

        group.MapPost("/imports/{importSessionId:guid}/draft-facts/bulk-decision", async Task<IResult> (Guid importSessionId, ImportedDraftFactBulkDecisionRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var decision = NormalizeOptional(request.Decision)?.ToLowerInvariant();
            if (decision is not ("approve" or "archive"))
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(request.Decision)] = ["Decision must be approve or archive."]
                }));
            }

            var selectedIds = request.ProfileFactIds?.Distinct().ToList() ?? new List<Guid>();
            if (selectedIds.Count == 0)
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(request.ProfileFactIds)] = ["At least one imported draft fact is required."]
                }));
            }

            var selectedFacts = await db.ProfileFacts
                .Where(fact => selectedIds.Contains(fact.Id))
                .ToListAsync(ct);
            if (selectedFacts.Count != selectedIds.Count || selectedFacts.Any(fact => fact.Status != ProfileFactStatus.Draft || !ImportedFromSession(fact, importSessionId)))
            {
                return Results.NotFound(ApiError.NotFound("One or more imported draft facts were not found for this import session."));
            }

            var now = DateTimeOffset.UtcNow;
            var status = decision == "approve" ? ProfileFactStatus.Approved : ProfileFactStatus.Archived;
            foreach (var fact in selectedFacts)
            {
                fact.Status = status;
                fact.UpdatedAt = now;
            }

            await db.SaveChangesAsync(ct);

            var facts = await LoadFactsForImportReviewAsync(db, ct);
            var fileName = ImportSnapshot(selectedFacts[0])?.FileName ?? "Imported CV";
            return Results.Ok(new ImportedDraftFactBulkDecisionResponse(
                selectedFacts.Select(ToResponse).ToList(),
                ToImportedDraftFactReviewQueue(importSessionId, fileName, DraftImportFacts(facts, importSessionId), facts)));
        });

        group.MapPost("/imports/{importSessionId:guid}/draft-facts/merge", async Task<IResult> (Guid importSessionId, ImportedDraftFactMergeRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var selectedIds = request.ProfileFactIds?.Distinct().ToList() ?? new List<Guid>();
            if (selectedIds.Count < 2)
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(request.ProfileFactIds)] = ["At least two imported draft facts are required."]
                }));
            }

            if (request.ProfileFact is not null)
            {
                var errors = Validate(request.ProfileFact);
                if (errors.Count > 0)
                {
                    return Results.BadRequest(ApiError.Validation(errors));
                }
            }

            var selectedFacts = await db.ProfileFacts
                .Where(fact => selectedIds.Contains(fact.Id))
                .ToListAsync(ct);
            if (selectedFacts.Count != selectedIds.Count || selectedFacts.Any(fact => fact.Status != ProfileFactStatus.Draft || !ImportedFromSession(fact, importSessionId)))
            {
                return Results.NotFound(ApiError.NotFound("One or more imported draft facts were not found for this import session."));
            }

            var orderedFacts = selectedFacts.OrderBy(fact => selectedIds.IndexOf(fact.Id)).ToList();
            var now = DateTimeOffset.UtcNow;
            var mergedFact = orderedFacts[0];
            ApplyMergedImportedFact(mergedFact, orderedFacts, request.ProfileFact, now);

            foreach (var archivedFact in orderedFacts.Skip(1))
            {
                archivedFact.Status = ProfileFactStatus.Archived;
                archivedFact.UpdatedAt = now;
            }

            await db.SaveChangesAsync(ct);

            var facts = await LoadFactsForImportReviewAsync(db, ct);
            var fileName = ImportSnapshot(mergedFact)?.FileName ?? "Imported CV";
            return Results.Ok(new ImportedDraftFactMergeResponse(
                ToResponse(mergedFact),
                ToImportedDraftFactReviewQueue(importSessionId, fileName, DraftImportFacts(facts, importSessionId), facts)));
        });

        group.MapPost("/imports/{importSessionId:guid}/draft-facts/{factId:guid}/split", async Task<IResult> (Guid importSessionId, Guid factId, ImportedDraftFactSplitRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (request.ProfileFacts is null || request.ProfileFacts.Count < 2)
            {
                return Results.BadRequest(ApiError.Validation(new Dictionary<string, string[]>
                {
                    [nameof(request.ProfileFacts)] = ["At least two split imported draft facts are required."]
                }));
            }

            for (var index = 0; index < request.ProfileFacts.Count; index++)
            {
                var errors = Validate(request.ProfileFacts[index]);
                if (errors.Count > 0)
                {
                    return Results.BadRequest(ApiError.Validation(errors.ToDictionary(
                        pair => $"{nameof(request.ProfileFacts)}[{index}].{pair.Key}",
                        pair => pair.Value)));
                }
            }

            var fact = await db.ProfileFacts.FindAsync([factId], ct);
            if (fact is null || fact.Status != ProfileFactStatus.Draft || !ImportedFromSession(fact, importSessionId))
            {
                return Results.NotFound(ApiError.NotFound("Imported draft fact was not found for this import session."));
            }

            var now = DateTimeOffset.UtcNow;
            fact.Status = ProfileFactStatus.Archived;
            fact.UpdatedAt = now;

            var splitFacts = request.ProfileFacts
                .Select(splitRequest => ToSplitImportedFact(fact, splitRequest, now))
                .ToList();
            db.ProfileFacts.AddRange(splitFacts);

            await db.SaveChangesAsync(ct);

            var facts = await LoadFactsForImportReviewAsync(db, ct);
            var fileName = ImportSnapshot(fact)?.FileName ?? "Imported CV";
            return Results.Ok(new ImportedDraftFactSplitResponse(
                splitFacts.Select(ToResponse).ToList(),
                ToImportedDraftFactReviewQueue(importSessionId, fileName, DraftImportFacts(facts, importSessionId), facts)));
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

    private static ProfileFact ToSplitImportedFact(ProfileFact originalFact, ProfileFactRequest request, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Type = request.Type!.Trim(),
            Title = request.Title!.Trim(),
            Summary = request.Summary!.Trim(),
            Status = ProfileFactStatus.Draft,
            FactItems = NormalizeJsonArray(request.FactItems),
            Technologies = NormalizeJsonArray(request.Technologies),
            AllowedClaims = NormalizeJsonArray(request.AllowedClaims),
            ForbiddenClaims = NormalizeJsonArray(request.ForbiddenClaims),
            SourceDocumentIds = originalFact.SourceDocumentIds,
            OriginalImportedSnapshot = originalFact.OriginalImportedSnapshot,
            ManuallyEdited = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static void ApplyMergedImportedFact(ProfileFact mergedFact, IReadOnlyList<ProfileFact> sourceFacts, ProfileFactRequest? request, DateTimeOffset now)
    {
        if (request is not null)
        {
            ApplyProfileFactRequest(mergedFact, request, markEdited: true);
            mergedFact.Status = ProfileFactStatus.Draft;
        }
        else
        {
            mergedFact.Type = MostCommonValue(sourceFacts.Select(fact => fact.Type), "ImportedCv", 80);
            mergedFact.Title = NormalizeRequired(string.Join(" + ", sourceFacts.Select(fact => fact.Title).Distinct(StringComparer.OrdinalIgnoreCase)), "Merged imported CV fact", 200);
            mergedFact.Summary = NormalizeRequired(string.Join("\n\n", sourceFacts.Select(fact => fact.Summary).Where(summary => !string.IsNullOrWhiteSpace(summary)).Distinct(StringComparer.OrdinalIgnoreCase)), "Merged imported CV fact.", 4000);
            mergedFact.FactItems = SerializeJsonArray(sourceFacts.SelectMany(fact => JsonArrayValues(fact.FactItems)).ToList());
            mergedFact.Technologies = SerializeJsonArray(sourceFacts.SelectMany(fact => JsonArrayValues(fact.Technologies)).ToList());
            mergedFact.AllowedClaims = SerializeJsonArray(sourceFacts.SelectMany(fact => JsonArrayValues(fact.AllowedClaims)).ToList());
            mergedFact.ForbiddenClaims = SerializeJsonArray(sourceFacts.SelectMany(fact => JsonArrayValues(fact.ForbiddenClaims)).ToList());
            mergedFact.ManuallyEdited = true;
        }

        mergedFact.SourceDocumentIds = SerializeJsonArray(sourceFacts.SelectMany(fact => JsonArrayValues(fact.SourceDocumentIds)).ToList());
        mergedFact.OriginalImportedSnapshot = MergedImportSnapshot(sourceFacts);
        mergedFact.UpdatedAt = now;
    }

    private static string MergedImportSnapshot(IReadOnlyList<ProfileFact> sourceFacts)
    {
        var firstSnapshot = sourceFacts.Select(ImportSnapshot).FirstOrDefault(snapshot => snapshot is not null);
        var sourceContexts = sourceFacts
            .Select(SourceContext)
            .Where(context => !string.IsNullOrWhiteSpace(context))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var extractedTextPreviews = sourceFacts
            .Select(fact => ImportSnapshot(fact)?.ExtractedTextPreview)
            .Where(preview => !string.IsNullOrWhiteSpace(preview))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        return JsonSerializer.Serialize(new
        {
            importSessionId = firstSnapshot?.ImportSessionId ?? Guid.Empty,
            fileName = firstSnapshot?.FileName ?? "Imported CV",
            importedAt = firstSnapshot?.ImportedAt ?? DateTimeOffset.UtcNow,
            sourceContext = string.Join("\n\n", sourceContexts),
            extractedTextPreview = string.Join("\n\n", extractedTextPreviews)
        }, JsonOptions);
    }

    private static string MostCommonValue(IEnumerable<string> values, string fallback, int maxLength) =>
        NormalizeRequired(
            values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => group.Key)
                .FirstOrDefault() ?? fallback,
            fallback,
            maxLength);

    private static async Task<List<ProfileFact>> LoadFactsForImportReviewAsync(ApplicationDbContext db, CancellationToken ct) =>
        await db.ProfileFacts
            .AsNoTracking()
            .OrderBy(fact => fact.Type)
            .ThenBy(fact => fact.Title)
            .ToListAsync(ct);

    private static List<ProfileFact> DraftImportFacts(IReadOnlyList<ProfileFact> facts, Guid importSessionId) =>
        facts
            .Where(fact => fact.Status == ProfileFactStatus.Draft && ImportedFromSession(fact, importSessionId))
            .ToList();

    private static void ValidateAssistedProfileImportResult(AssistedProfileImportResult result)
    {
        if (result.Facts is null || result.Facts.Count == 0)
        {
            throw new AiInvalidOutputException("AI provider returned assisted profile import without facts.", result.AttemptCount);
        }

        if (result.Facts.Any(fact =>
            string.IsNullOrWhiteSpace(fact.Type) ||
            string.IsNullOrWhiteSpace(fact.Title) ||
            string.IsNullOrWhiteSpace(fact.Summary) ||
            fact.FactItems is null ||
            fact.Technologies is null ||
            fact.AllowedClaims is null ||
            fact.ForbiddenClaims is null))
        {
            throw new AiInvalidOutputException("AI provider returned structurally invalid assisted profile import facts.", result.AttemptCount);
        }
    }

    private static ImportedDraftFactReviewQueueResponse ToImportedDraftFactReviewQueue(
        Guid importSessionId,
        string fileName,
        IReadOnlyList<ProfileFact> importFacts,
        IReadOnlyList<ProfileFact> comparisonFacts)
    {
        var importFactIds = importFacts.Select(fact => fact.Id).ToHashSet();
        var duplicateIndicators = importFacts.ToDictionary(
            fact => fact.Id,
            fact => DuplicateIndicators(fact, importFacts, comparisonFacts.Where(other => !importFactIds.Contains(other.Id)).ToList()));

        var groups = importFacts
            .GroupBy(fact => string.IsNullOrWhiteSpace(fact.Type) ? "ImportedCv" : fact.Type.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key)
            .Select(group => new ImportedDraftFactReviewGroupResponse(
                group.Key,
                group.Key,
                group.Count(),
                group
                    .OrderBy(fact => fact.Title)
                    .Select(fact =>
                    {
                        var indicators = duplicateIndicators[fact.Id];
                        return new ImportedDraftFactReviewItemResponse(
                            ToReviewResponse(fact),
                            SourceContext(fact),
                            indicators.Count > 0,
                            indicators);
                    })
                    .ToList()))
            .ToList();

        return new ImportedDraftFactReviewQueueResponse(importSessionId, fileName, importFacts.Count, groups);
    }

    private static List<ImportedDraftFactDuplicateIndicatorResponse> DuplicateIndicators(
        ProfileFact fact,
        IReadOnlyList<ProfileFact> importFacts,
        IReadOnlyList<ProfileFact> existingFacts)
    {
        var indicators = new List<ImportedDraftFactDuplicateIndicatorResponse>();

        foreach (var other in importFacts.Where(other => other.Id != fact.Id))
        {
            var reason = DuplicateReason(fact, other);
            if (reason is not null)
            {
                indicators.Add(new ImportedDraftFactDuplicateIndicatorResponse("ImportBatch", other.Id, other.Title, reason));
            }
        }

        foreach (var other in existingFacts)
        {
            var reason = DuplicateReason(fact, other);
            if (reason is not null)
            {
                indicators.Add(new ImportedDraftFactDuplicateIndicatorResponse("ExistingProfileFact", other.Id, other.Title, reason));
            }
        }

        return indicators
            .GroupBy(indicator => new { indicator.Scope, indicator.ProfileFactId })
            .Select(group => group.First())
            .OrderBy(indicator => indicator.Scope)
            .ThenBy(indicator => indicator.ProfileFactTitle)
            .ToList();
    }

    private static ProfileFactResponse ToReviewResponse(ProfileFact fact) =>
        ToResponse(fact) with { OriginalImportedSnapshot = null };

    private static string? DuplicateReason(ProfileFact left, ProfileFact right)
    {
        if (NormalizeComparison(left.Title) == NormalizeComparison(right.Title))
        {
            return "Title matches another fact.";
        }

        if (NormalizeComparison(left.Summary) == NormalizeComparison(right.Summary))
        {
            return "Summary matches another fact.";
        }

        var sharedTechnologies = JsonArrayValues(left.Technologies)
            .Intersect(JsonArrayValues(right.Technologies), StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (sharedTechnologies.Count >= 2)
        {
            return $"Shares technologies: {string.Join(", ", sharedTechnologies.Take(4))}.";
        }

        var leftTerms = ComparisonTerms(string.Join(" ", left.Title, left.Summary, string.Join(" ", JsonArrayValues(left.FactItems))));
        var rightTerms = ComparisonTerms(string.Join(" ", right.Title, right.Summary, string.Join(" ", JsonArrayValues(right.FactItems))));
        var sharedTerms = leftTerms.Intersect(rightTerms, StringComparer.OrdinalIgnoreCase).ToList();
        return sharedTerms.Count >= 5 ? "Content substantially overlaps another fact." : null;
    }

    private static ImportedFactSnapshot? ImportSnapshot(ProfileFact fact)
    {
        if (string.IsNullOrWhiteSpace(fact.OriginalImportedSnapshot))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ImportedFactSnapshot>(fact.OriginalImportedSnapshot, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool ImportedFromSession(ProfileFact fact, Guid importSessionId) =>
        ImportSnapshot(fact)?.ImportSessionId == importSessionId;

    private static string SourceContext(ProfileFact fact) =>
        NormalizeRequired(ImportSnapshot(fact)?.SourceContext ?? string.Empty, "No source context captured.", 700);

    private static string NormalizeComparison(string value) =>
        new(value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : ' ')
            .ToArray());

    private static IReadOnlyList<string> ComparisonTerms(string value) =>
        NormalizeComparison(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length >= 4)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IReadOnlyList<string> JsonArrayValues(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(value, JsonOptions)?
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

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
            errors[nameof(request.Status)] = ["Status must be Draft, Approved, Archived, or Rejected."];
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
        if (file is null)
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

    private const int MaxAiRunErrorMessageLength = 4000;

    private static void RecordFailedRun(AiRun run, AiProviderException exception)
    {
        run.Status = "Failed";
        run.ErrorCode = exception.ErrorCode;
        run.ErrorMessage = TruncateAiRunErrorMessage(exception.Message);
        run.AttemptCount = exception.AttemptCount;
        run.CompletedAt = DateTimeOffset.UtcNow;
    }

    private static string TruncateAiRunErrorMessage(string message) =>
        message.Length <= MaxAiRunErrorMessageLength
            ? message
            : string.Concat(
                message.AsSpan(0, MaxAiRunErrorMessageLength - 34),
                " ... [truncated for AiRun limit]");

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

    private static void ApplyProfileFactRequest(ProfileFact fact, ProfileFactRequest request, bool markEdited)
    {
        fact.Type = request.Type!.Trim();
        fact.Title = request.Title!.Trim();
        fact.Summary = request.Summary!.Trim();
        fact.Status = ParseStatus(request.Status!);
        fact.FactItems = NormalizeJsonArray(request.FactItems);
        fact.Technologies = NormalizeJsonArray(request.Technologies);
        fact.AllowedClaims = NormalizeJsonArray(request.AllowedClaims);
        fact.ForbiddenClaims = NormalizeJsonArray(request.ForbiddenClaims);
        fact.ManuallyEdited = markEdited || fact.ManuallyEdited;
    }

    private static string NormalizeJsonArray(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "[]" : value.Trim();

    private static string NormalizeRequired(string value, string fallback, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string SerializeJsonArray(IReadOnlyList<string> values) =>
        JsonSerializer.Serialize(values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList(), JsonOptions);

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

    private sealed record ImportedFactSnapshot(
        Guid ImportSessionId,
        string FileName,
        DateTimeOffset ImportedAt,
        string SourceContext,
        string? ExtractedTextPreview);
}
