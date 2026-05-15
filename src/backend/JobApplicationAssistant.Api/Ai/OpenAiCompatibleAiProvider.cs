using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobApplicationAssistant.Api.Ai;

public sealed class OpenAiCompatibleAiProvider : IAiProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string JobAnalysisPrompt = LoadPrompt("job-analysis.md");
    private static readonly string EvidenceMatchingPrompt = LoadPrompt("evidence-matching.md");
    private static readonly string DraftGenerationPrompt = LoadPrompt("draft-generation.md");
    private static readonly string ClaimAuditPrompt = LoadPrompt("claim-audit.md");
    private static readonly string AssistedProfileImportPrompt = LoadPrompt("assisted-profile-import.md");

    private readonly HttpClient httpClient;
    private readonly AiOptions options;

    public OpenAiCompatibleAiProvider(HttpClient httpClient, AiOptions options)
    {
        this.httpClient = httpClient;
        this.options = options;
    }

    public async Task<AiProviderStatus> GetStatusAsync(CancellationToken ct)
    {
        var diagnostics = await RunDiagnosticsAsync(ct);

        return new AiProviderStatus(
            diagnostics.Provider,
            diagnostics.Model,
            diagnostics.Endpoint,
            diagnostics.IsAvailable,
            diagnostics.Message);
    }

    public async Task<AiDiagnosticsResult> RunDiagnosticsAsync(CancellationToken ct)
    {
        var checks = new List<AiDiagnosticCheck>
        {
            new("provider", "ok", "OpenAI-compatible provider is selected."),
            new("rawPayloads", options.StoreRawPayloads ? "enabled" : "disabled", options.StoreRawPayloads
                ? "Raw payload storage is enabled."
                : "Raw payload storage is disabled.")
        };
        var configuredModel = options.Model.Trim();
        var isModelConfigured = !string.IsNullOrWhiteSpace(configuredModel);
        checks.Add(new AiDiagnosticCheck(
            "modelConfigured",
            isModelConfigured ? "ok" : "unavailable",
            isModelConfigured ? $"Model {configuredModel} is configured." : "Ai:Model is blank."));

        try
        {
            using var response = await httpClient.GetAsync("models", ct);
            checks.Add(new AiDiagnosticCheck("connectivity", "ok", "OpenAI-compatible endpoint is reachable."));

            if (!response.IsSuccessStatusCode)
            {
                checks.Add(new AiDiagnosticCheck(
                    "modelAvailability",
                    "unknown",
                    $"Model list could not be determined because /models returned {(int)response.StatusCode} {response.ReasonPhrase}."));

                return Result(
                    isModelConfigured,
                    isModelConfigured
                        ? "OpenAI-compatible endpoint is reachable; model availability could not be determined."
                        : "OpenAI-compatible endpoint is reachable, but Ai:Model is blank.",
                    checks);
            }

            OpenAiModelsResponse? models;
            try
            {
                models = await response.Content.ReadFromJsonAsync<OpenAiModelsResponse>(JsonOptions, ct);
            }
            catch (JsonException)
            {
                checks.Add(new AiDiagnosticCheck("modelAvailability", "unknown", "Model list response was malformed."));
                return Result(
                    isModelConfigured,
                    isModelConfigured
                        ? "OpenAI-compatible endpoint is reachable; model availability could not be determined."
                        : "OpenAI-compatible endpoint is reachable, but Ai:Model is blank.",
                    checks);
            }

            if (models?.Data is null)
            {
                checks.Add(new AiDiagnosticCheck("modelAvailability", "unknown", "Model list response did not include a data array."));
                return Result(
                    isModelConfigured,
                    isModelConfigured
                        ? "OpenAI-compatible endpoint is reachable; model availability could not be determined."
                        : "OpenAI-compatible endpoint is reachable, but Ai:Model is blank.",
                    checks);
            }

            if (!isModelConfigured)
            {
                checks.Add(new AiDiagnosticCheck("modelAvailability", "unknown", "Model availability was not checked because Ai:Model is blank."));
                return Result(false, "OpenAI-compatible endpoint is reachable, but Ai:Model is blank.", checks);
            }

            var isModelAvailable = models.Data.Any(model => string.Equals(model?.Id, configuredModel, StringComparison.OrdinalIgnoreCase));
            checks.Add(new AiDiagnosticCheck(
                "modelAvailability",
                isModelAvailable ? "ok" : "unavailable",
                isModelAvailable ? $"Model {configuredModel} is available." : $"Model {configuredModel} was not found."));

            return Result(
                isModelConfigured && isModelAvailable,
                isModelAvailable
                    ? "OpenAI-compatible provider is available."
                    : isModelConfigured
                        ? $"Model {configuredModel} was not found."
                        : "OpenAI-compatible endpoint is reachable, but Ai:Model is blank.",
                checks);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            const string message = "OpenAI-compatible endpoint is unavailable.";
            checks.Add(new AiDiagnosticCheck("connectivity", "unavailable", message));
            return Result(false, message, checks);
        }
        catch (HttpRequestException)
        {
            const string message = "OpenAI-compatible endpoint is unavailable.";
            checks.Add(new AiDiagnosticCheck("connectivity", "unavailable", message));
            return Result(false, message, checks);
        }
    }

    public async Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisInput input, CancellationToken ct)
    {
        var responseText = await ChatAsync(BuildJobAnalysisPrompt(input), attemptCount: 1, ct);
        try
        {
            return ParseJobAnalysis(responseText, attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await ChatAsync(BuildRepairPrompt(responseText, firstFailure.Message), attemptCount: 2, ct);
            return ParseJobAnalysis(repairedText, attemptCount: 2);
        }
    }

    public async Task<EvidenceMatchResult> MatchEvidenceAsync(EvidenceMatchInput input, CancellationToken ct)
    {
        var responseText = await ChatAsync(BuildEvidenceMatchingPrompt(input), attemptCount: 1, ct);
        try
        {
            return ParseEvidenceMatching(responseText, input, attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await ChatAsync(BuildEvidenceMatchingRepairPrompt(responseText, firstFailure.Message), attemptCount: 2, ct);
            return ParseEvidenceMatching(repairedText, input, attemptCount: 2);
        }
    }

    public async Task<DraftGenerationResult> GenerateDraftAsync(DraftGenerationInput input, CancellationToken ct)
    {
        var responseText = await ChatAsync(BuildDraftGenerationPrompt(input), attemptCount: 1, ct);
        try
        {
            return ParseDraftGeneration(responseText, attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await ChatAsync(BuildDraftGenerationRepairPrompt(responseText, firstFailure.Message), attemptCount: 2, ct);
            return ParseDraftGeneration(repairedText, attemptCount: 2);
        }
    }

    public async Task<ClaimAuditResult> AuditClaimsAsync(ClaimAuditInput input, CancellationToken ct)
    {
        var responseText = await ChatAsync(BuildClaimAuditPrompt(input), attemptCount: 1, ct);
        try
        {
            return ParseClaimAudit(responseText, input, attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await ChatAsync(BuildClaimAuditRepairPrompt(responseText, firstFailure.Message), attemptCount: 2, ct);
            return ParseClaimAudit(repairedText, input, attemptCount: 2);
        }
    }

    public async Task<AssistedProfileImportResult> ImportProfileFactsAsync(AssistedProfileImportInput input, CancellationToken ct)
    {
        var responseText = await ChatAsync(BuildAssistedProfileImportPrompt(input), attemptCount: 1, ct);
        try
        {
            return ParseAssistedProfileImport(responseText, attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await ChatAsync(BuildRepairPrompt(responseText, firstFailure.Message), attemptCount: 2, ct);
            return ParseAssistedProfileImport(repairedText, attemptCount: 2);
        }
    }

    private AiDiagnosticsResult Result(bool isAvailable, string message, IReadOnlyList<AiDiagnosticCheck> checks) =>
        new("OpenAiCompatible", options.Model, options.Endpoint, isAvailable, message, checks);

    private async Task<string> ChatAsync(string prompt, int attemptCount, CancellationToken ct)
    {
        var request = new OpenAiChatCompletionRequest(
            options.Model,
            [
                new OpenAiChatMessage("system", "Return only strict JSON. Do not include markdown."),
                new OpenAiChatMessage("user", prompt)
            ],
            new OpenAiResponseFormat("text"),
            Temperature: 0,
            Stream: false);
        var rawRequest = JsonSerializer.Serialize(request, JsonOptions);

        try
        {
            using var response = await httpClient.PostAsJsonAsync("chat/completions", request, JsonOptions, ct);
            var rawResponse = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new AiProviderUnavailableException(
                    AppendRawContext(
                        $"OpenAI-compatible endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}.",
                        rawRequest,
                        rawResponse),
                    attemptCount);
            }

            OpenAiChatCompletionResponse? payload;
            try
            {
                payload = JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(rawResponse, JsonOptions);
            }
            catch (JsonException exception)
            {
                throw new AiInvalidOutputException(
                    AppendRawPayload("OpenAI-compatible endpoint returned malformed chat completion JSON.", rawResponse),
                    attemptCount,
                    exception);
            }

            var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new AiInvalidOutputException(
                    AppendRawPayload("OpenAI-compatible endpoint returned empty assistant content.", rawResponse),
                    attemptCount);
            }

            return content;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new AiProviderUnavailableException(
                AppendRawContext("OpenAI-compatible endpoint is unavailable.", rawRequest, errorContext: "Request timed out."),
                attemptCount);
        }
        catch (HttpRequestException exception)
        {
            throw new AiProviderUnavailableException(
                AppendRawContext("OpenAI-compatible endpoint is unavailable.", rawRequest, errorContext: exception.Message),
                attemptCount,
                exception);
        }
    }

    private string AppendRawPayload(string message, string rawPayload) =>
        options.StoreRawPayloads && !string.IsNullOrWhiteSpace(rawPayload)
            ? $"{message} Raw payload: {rawPayload}"
            : message;

    private string AppendRawContext(string message, string rawRequest, string? rawResponse = null, string? errorContext = null)
    {
        if (!options.StoreRawPayloads)
        {
            return message;
        }

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(rawRequest))
        {
            details.Add($"Raw request: {rawRequest}");
        }

        if (!string.IsNullOrWhiteSpace(rawResponse))
        {
            details.Add($"Raw response: {rawResponse}");
        }

        if (!string.IsNullOrWhiteSpace(errorContext))
        {
            details.Add($"Error context: {errorContext}");
        }

        return details.Count == 0
            ? message
            : $"{message} {string.Join(' ', details)}";
    }

    private JobAnalysisResult ParseJobAnalysis(string responseText, int attemptCount)
    {
        OpenAiJobAnalysisResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OpenAiJobAnalysisResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned malformed job analysis JSON.", responseText),
                attemptCount,
                exception);
        }

        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.CompanyName) ||
            string.IsNullOrWhiteSpace(payload.RoleTitle) ||
            string.IsNullOrWhiteSpace(payload.DetectedLanguage) ||
            string.IsNullOrWhiteSpace(payload.SelectedLanguage) ||
            !IsSupportedLanguage(payload.DetectedLanguage) ||
            !IsSupportedLanguage(payload.SelectedLanguage) ||
            payload.JobSignals is null ||
            payload.JobSignals.RequiredSkills is null ||
            payload.JobSignals.PreferredSkills is null ||
            payload.JobSignals.Responsibilities is null ||
            payload.JobSignals.Signals is null)
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned structurally invalid job analysis JSON.", responseText),
                attemptCount);
        }

        var now = DateTimeOffset.UtcNow;
        if (payload.JobSignals.Signals.Any(signal =>
            signal is null ||
            string.IsNullOrWhiteSpace(signal.Id) ||
            string.IsNullOrWhiteSpace(signal.Label) ||
            string.IsNullOrWhiteSpace(signal.Category) ||
            !IsSupportedSignalCategory(signal.Category) ||
            signal.Keywords is null ||
            signal.Keywords.Count == 0 ||
            signal.Keywords.All(string.IsNullOrWhiteSpace)))
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned structurally invalid job signal JSON.", responseText),
                attemptCount);
        }

        var signals = payload.JobSignals.Signals
            .Select(signal => new JobSignal(
                signal.Id.Trim(),
                signal.Label.Trim(),
                signal.Category.Trim(),
                signal.Keywords.Where(keyword => !string.IsNullOrWhiteSpace(keyword)).Select(keyword => keyword.Trim()).ToList()))
            .ToList();

        if (signals.Count == 0)
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned job analysis without any job signals.", responseText),
                attemptCount);
        }

        var document = new JobSignalsDocument(
            "OpenAiCompatible",
            now,
            payload.JobSignals.RequiredSkills.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList(),
            payload.JobSignals.PreferredSkills.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList(),
            payload.JobSignals.Responsibilities.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList(),
            signals);

        return new JobAnalysisResult(
            payload.CompanyName.Trim(),
            payload.RoleTitle.Trim(),
            NormalizeLanguage(payload.DetectedLanguage),
            NormalizeLanguage(payload.SelectedLanguage),
            document)
        {
            AttemptCount = attemptCount
        };
    }

    private EvidenceMatchResult ParseEvidenceMatching(string responseText, EvidenceMatchInput input, int attemptCount)
    {
        OpenAiEvidenceMatchingResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OpenAiEvidenceMatchingResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned malformed evidence matching JSON.", responseText),
                attemptCount,
                exception);
        }

        if (payload is null ||
            payload.EvidenceMatches is null ||
            payload.UnmatchedRequirements is null)
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned structurally invalid evidence matching JSON.", responseText),
                attemptCount);
        }

        var signalsById = input.Signals.ToDictionary(signal => signal.Id, StringComparer.OrdinalIgnoreCase);
        var approvedFactsById = input.ApprovedFacts.ToDictionary(fact => fact.Id);
        var matches = new List<EvidenceMatch>();
        var unmatched = new List<UnmatchedRequirement>();
        var matchedSignalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unmatchedSignalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in payload.EvidenceMatches)
        {
            if (match is null ||
                string.IsNullOrWhiteSpace(match.SignalId) ||
                string.IsNullOrWhiteSpace(match.ProfileFactId) ||
                string.IsNullOrWhiteSpace(match.Summary) ||
                match.MatchedTerms is null ||
                match.MatchedTerms.Count == 0 ||
                match.MatchedTerms.All(string.IsNullOrWhiteSpace) ||
                !signalsById.TryGetValue(match.SignalId.Trim(), out var signal) ||
                !Guid.TryParse(match.ProfileFactId, out var profileFactId) ||
                !approvedFactsById.TryGetValue(profileFactId, out var fact))
            {
                throw new AiInvalidOutputException(
                    AppendRawPayload("OpenAI-compatible endpoint returned structurally invalid evidence match JSON.", responseText),
                    attemptCount);
            }

            if (!matchKeys.Add($"{signal.Id}:{fact.Id:N}"))
            {
                throw new AiInvalidOutputException(
                    AppendRawPayload("OpenAI-compatible endpoint returned duplicate evidence matches for a job signal and profile fact.", responseText),
                    attemptCount);
            }

            matchedSignalIds.Add(signal.Id);
            matches.Add(new EvidenceMatch(
                $"match-{signal.Id}-{fact.Id:N}",
                signal.Id,
                signal.Label,
                signal.Category,
                fact.Id,
                fact.Title,
                match.Summary.Trim(),
                match.MatchedTerms.Where(term => !string.IsNullOrWhiteSpace(term)).Select(term => term.Trim()).ToList()));
        }

        foreach (var requirement in payload.UnmatchedRequirements)
        {
            if (requirement is null ||
                string.IsNullOrWhiteSpace(requirement.SignalId) ||
                string.IsNullOrWhiteSpace(requirement.Recommendation) ||
                !signalsById.TryGetValue(requirement.SignalId.Trim(), out var signal))
            {
                throw new AiInvalidOutputException(
                    AppendRawPayload("OpenAI-compatible endpoint returned structurally invalid unmatched requirement JSON.", responseText),
                    attemptCount);
            }

            if (!unmatchedSignalIds.Add(signal.Id))
            {
                throw new AiInvalidOutputException(
                    AppendRawPayload("OpenAI-compatible endpoint returned duplicate unmatched requirements for a job signal.", responseText),
                    attemptCount);
            }

            unmatched.Add(new UnmatchedRequirement(
                $"unmatched-{signal.Id}",
                signal.Id,
                signal.Label,
                signal.Category,
                requirement.Recommendation.Trim()));
        }

        if (matchedSignalIds.Overlaps(unmatchedSignalIds) ||
            input.Signals.Any(signal => !matchedSignalIds.Contains(signal.Id) && !unmatchedSignalIds.Contains(signal.Id)))
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned incomplete or conflicting evidence matching JSON.", responseText),
                attemptCount);
        }

        return new EvidenceMatchResult(matches, unmatched)
        {
            AttemptCount = attemptCount
        };
    }

    private DraftGenerationResult ParseDraftGeneration(string responseText, int attemptCount)
    {
        OpenAiDraftGenerationResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OpenAiDraftGenerationResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned malformed draft generation JSON.", responseText),
                attemptCount,
                exception);
        }

        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.CoverLetterText) ||
            string.IsNullOrWhiteSpace(payload.ShortMotivationText))
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned structurally invalid draft generation JSON.", responseText),
                attemptCount);
        }

        return new DraftGenerationResult(
            payload.CoverLetterText.Trim(),
            payload.ShortMotivationText.Trim())
        {
            AttemptCount = attemptCount
        };
    }

    private ClaimAuditResult ParseClaimAudit(string responseText, ClaimAuditInput input, int attemptCount)
    {
        OpenAiClaimAuditResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OpenAiClaimAuditResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned malformed claim audit JSON.", responseText),
                attemptCount,
                exception);
        }

        if (payload?.Claims is null)
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned structurally invalid claim audit JSON.", responseText),
                attemptCount);
        }

        var approvedEvidenceIds = input.ApprovedEvidence
            .Select(evidence => evidence.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var claims = new List<ClaimAuditClaim>();
        var claimIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in payload.Claims)
        {
            if (claim is null ||
                string.IsNullOrWhiteSpace(claim.Id) ||
                string.IsNullOrWhiteSpace(claim.Text) ||
                string.IsNullOrWhiteSpace(claim.Status) ||
                claim.EvidenceIds is null ||
                !IsSupportedClaimStatus(claim.Status) ||
                claim.EvidenceIds.Any(evidenceId => string.IsNullOrWhiteSpace(evidenceId) || !approvedEvidenceIds.Contains(evidenceId.Trim())) ||
                !claimIds.Add(claim.Id.Trim()))
            {
                throw new AiInvalidOutputException(
                    AppendRawPayload("OpenAI-compatible endpoint returned structurally invalid claim audit JSON.", responseText),
                    attemptCount);
            }

            claims.Add(new ClaimAuditClaim(
                claim.Id.Trim(),
                claim.Text.Trim(),
                NormalizeClaimStatus(claim.Status),
                claim.EvidenceIds.Select(evidenceId => evidenceId.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()));
        }

        return new ClaimAuditResult(claims)
        {
            AttemptCount = attemptCount
        };
    }

    private AssistedProfileImportResult ParseAssistedProfileImport(string responseText, int attemptCount)
    {
        OpenAiAssistedProfileImportResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OpenAiAssistedProfileImportResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned malformed assisted profile import JSON.", responseText),
                attemptCount,
                exception);
        }

        if (payload?.Facts is null || payload.Facts.Count == 0)
        {
            throw new AiInvalidOutputException(
                AppendRawPayload("OpenAI-compatible endpoint returned assisted profile import without facts.", responseText),
                attemptCount);
        }

        var facts = payload.Facts.Select(fact =>
        {
            if (fact is null ||
                string.IsNullOrWhiteSpace(fact.Type) ||
                string.IsNullOrWhiteSpace(fact.Title) ||
                string.IsNullOrWhiteSpace(fact.Summary) ||
                fact.FactItems is null ||
                fact.Technologies is null ||
                fact.AllowedClaims is null ||
                fact.ForbiddenClaims is null)
            {
                throw new AiInvalidOutputException(
                    AppendRawPayload("OpenAI-compatible endpoint returned structurally invalid assisted profile import fact JSON.", responseText),
                    attemptCount);
            }

            return new AssistedProfileImportFact(
                fact.Type.Trim(),
                fact.Title.Trim(),
                fact.Summary.Trim(),
                TrimStrings(fact.FactItems),
                TrimStrings(fact.Technologies),
                TrimStrings(fact.AllowedClaims),
                TrimStrings(fact.ForbiddenClaims),
                fact.SourceContext?.Trim() ?? string.Empty);
        }).ToList();

        return new AssistedProfileImportResult(facts)
        {
            AttemptCount = attemptCount
        };
    }

    private static string BuildJobAnalysisPrompt(JobAnalysisInput input) =>
        $"""
        {JobAnalysisPrompt}

        Existing application metadata:
        Company name: {input.CompanyName}
        Role title: {input.RoleTitle}
        Selected language: {input.SelectedLanguage ?? "(none)"}

        Job posting:
        {input.JobPostingText}
        """;

    private static string BuildEvidenceMatchingPrompt(EvidenceMatchInput input)
    {
        var signals = input.Signals.Select(signal => new
        {
            signal.Id,
            signal.Label,
            signal.Category,
            signal.Keywords
        });
        var approvedFacts = input.ApprovedFacts.Select(fact => new
        {
            fact.Id,
            fact.Type,
            fact.Title,
            fact.Summary,
            fact.FactItems,
            fact.Technologies,
            fact.AllowedClaims
        });

        return $"""
        {EvidenceMatchingPrompt}

        Job signals:
        {JsonSerializer.Serialize(signals, JsonOptions)}

        Approved profile facts:
        {JsonSerializer.Serialize(approvedFacts, JsonOptions)}
        """;
    }

    private static string BuildDraftGenerationPrompt(DraftGenerationInput input)
    {
        var approvedEvidence = input.ApprovedEvidence.Select(evidence => new
        {
            evidence.Id,
            evidence.SignalId,
            evidence.Signal,
            evidence.Category,
            evidence.ProfileFactTitle,
            evidence.Summary,
            evidence.MatchedTerms
        });
        var unmatchedRequirements = input.UnmatchedRequirements.Select(requirement => new
        {
            requirement.Id,
            requirement.SignalId,
            requirement.Requirement,
            requirement.Category,
            requirement.Recommendation
        });
        var gapDecisions = input.GapDecisions.Select(decision => new
        {
            decision.UnmatchedRequirementId,
            decision.Decision,
            decision.CustomFactId
        });
        var approvedCustomFacts = input.ApprovedCustomFacts.Select(fact => new
        {
            fact.Id,
            fact.UnmatchedRequirementId,
            fact.Title,
            fact.Summary,
            fact.Technologies,
            fact.AllowedClaims
        });

        return $"""
        {DraftGenerationPrompt}

        Application context:
        {JsonSerializer.Serialize(new
        {
            input.CompanyName,
            input.RoleTitle,
            input.SelectedLanguage,
            input.ApplicantName,
            input.TonePreference
        }, JsonOptions)}

        Approved evidence:
        {JsonSerializer.Serialize(approvedEvidence, JsonOptions)}

        Unmatched requirements:
        {JsonSerializer.Serialize(unmatchedRequirements, JsonOptions)}

        Gap decisions:
        {JsonSerializer.Serialize(gapDecisions, JsonOptions)}

        Approved job-local custom facts:
        {JsonSerializer.Serialize(approvedCustomFacts, JsonOptions)}
        """;
    }

    private static string BuildClaimAuditPrompt(ClaimAuditInput input)
    {
        var approvedEvidence = input.ApprovedEvidence.Select(evidence => new
        {
            evidence.Id,
            evidence.SignalId,
            evidence.Signal,
            evidence.Category,
            evidence.ProfileFactTitle,
            evidence.Summary,
            evidence.MatchedTerms
        });

        return $"""
        {ClaimAuditPrompt}

        Draft:
        {JsonSerializer.Serialize(new
        {
            input.CoverLetterText,
            input.ShortMotivationText
        }, JsonOptions)}

        Approved evidence:
        {JsonSerializer.Serialize(approvedEvidence, JsonOptions)}
        """;
    }

    private static string BuildAssistedProfileImportPrompt(AssistedProfileImportInput input) =>
        $"""
        {AssistedProfileImportPrompt}

        File name: {input.FileName}

        CV text:
        {input.ExtractedText}
        """;

    private static string BuildRepairPrompt(string invalidJson, string validationError) =>
        $"""
        Repair this job analysis JSON so it matches the required contract exactly.
        Return only strict JSON. Do not include markdown.

        Validation error:
        {validationError}

        Invalid JSON:
        {invalidJson}
        """;

    private static string BuildEvidenceMatchingRepairPrompt(string invalidJson, string validationError) =>
        $"""
        Repair this evidence matching JSON so it matches the required contract exactly.
        Return only strict JSON. Do not include markdown.

        Validation error:
        {validationError}

        Invalid JSON:
        {invalidJson}
        """;

    private static string BuildDraftGenerationRepairPrompt(string invalidJson, string validationError) =>
        $"""
        Repair this draft generation JSON so it matches the required contract exactly.
        Return only strict JSON. Do not include markdown.

        Validation error:
        {validationError}

        Invalid JSON:
        {invalidJson}
        """;

    private static string BuildClaimAuditRepairPrompt(string invalidJson, string validationError) =>
        $"""
        Repair this claim audit JSON so it matches the required contract exactly.
        Return only strict JSON. Do not include markdown.

        Validation error:
        {validationError}

        Invalid JSON:
        {invalidJson}
        """;

    private static bool IsSupportedLanguage(string language) =>
        string.Equals(language.Trim(), "English", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(language.Trim(), "Danish", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeLanguage(string language) =>
        string.Equals(language.Trim(), "Danish", StringComparison.OrdinalIgnoreCase) ? "Danish" : "English";

    private static bool IsSupportedSignalCategory(string category) =>
        string.Equals(category.Trim(), "RequiredSkill", StringComparison.Ordinal) ||
        string.Equals(category.Trim(), "PreferredSkill", StringComparison.Ordinal) ||
        string.Equals(category.Trim(), "Responsibility", StringComparison.Ordinal);

    private static bool IsSupportedClaimStatus(string status) =>
        string.Equals(status.Trim(), "Supported", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status.Trim(), "Unsupported", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status.Trim(), "NeedsReview", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeClaimStatus(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            "supported" => "Supported",
            "unsupported" => "Unsupported",
            _ => "NeedsReview"
        };

    private static string LoadPrompt(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Ai", "Prompts", fileName);
        return File.ReadAllText(path);
    }

    private static IReadOnlyList<string> TrimStrings(IReadOnlyList<string> values) =>
        values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList();

    private sealed record OpenAiModelsResponse(IReadOnlyList<OpenAiModel?>? Data);

    private sealed record OpenAiModel(string Id);

    private sealed record OpenAiChatCompletionRequest(
        string Model,
        IReadOnlyList<OpenAiChatMessage> Messages,
        [property: JsonPropertyName("response_format")] OpenAiResponseFormat ResponseFormat,
        decimal Temperature,
        bool Stream);

    private sealed record OpenAiChatMessage(string Role, string Content);

    private sealed record OpenAiResponseFormat(string Type);

    private sealed record OpenAiChatCompletionResponse(IReadOnlyList<OpenAiChoice>? Choices);

    private sealed record OpenAiChoice(OpenAiChatMessage? Message);

    private sealed record OpenAiJobAnalysisResponse(
        string CompanyName,
        string RoleTitle,
        string DetectedLanguage,
        string SelectedLanguage,
        OpenAiJobSignalsResponse JobSignals);

    private sealed record OpenAiJobSignalsResponse(
        IReadOnlyList<string> RequiredSkills,
        IReadOnlyList<string> PreferredSkills,
        IReadOnlyList<string> Responsibilities,
        IReadOnlyList<OpenAiJobSignalResponse> Signals);

    private sealed record OpenAiJobSignalResponse(
        string Id,
        string Label,
        string Category,
        IReadOnlyList<string> Keywords);

    private sealed record OpenAiEvidenceMatchingResponse(
        IReadOnlyList<OpenAiEvidenceMatchResponse> EvidenceMatches,
        IReadOnlyList<OpenAiUnmatchedRequirementResponse> UnmatchedRequirements);

    private sealed record OpenAiEvidenceMatchResponse(
        string SignalId,
        string ProfileFactId,
        string Summary,
        IReadOnlyList<string> MatchedTerms);

    private sealed record OpenAiUnmatchedRequirementResponse(
        string SignalId,
        string Recommendation);

    private sealed record OpenAiDraftGenerationResponse(
        string CoverLetterText,
        string ShortMotivationText);

    private sealed record OpenAiClaimAuditResponse(
        IReadOnlyList<OpenAiClaimAuditClaimResponse> Claims);

    private sealed record OpenAiClaimAuditClaimResponse(
        string Id,
        string Text,
        string Status,
        IReadOnlyList<string> EvidenceIds);

    private sealed record OpenAiAssistedProfileImportResponse(
        IReadOnlyList<OpenAiAssistedProfileImportFactResponse> Facts);

    private sealed record OpenAiAssistedProfileImportFactResponse(
        string Type,
        string Title,
        string Summary,
        IReadOnlyList<string> FactItems,
        IReadOnlyList<string> Technologies,
        IReadOnlyList<string> AllowedClaims,
        IReadOnlyList<string> ForbiddenClaims,
        string? SourceContext);
}
