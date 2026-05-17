using System.Net.Http.Json;
using System.Text.Json;

namespace JobApplicationAssistant.Api.Ai;

public sealed class OllamaAiProvider : IAiProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string JobAnalysisPrompt = LoadPrompt("job-analysis.md");
    private static readonly string EvidenceMatchingPrompt = LoadPrompt("evidence-matching.md");
    private static readonly string DraftGenerationPrompt = LoadPrompt("draft-generation.md");
    private static readonly string ClaimAuditPrompt = LoadPrompt("claim-audit.md");
    private static readonly string CandidateFitBriefPrompt = LoadPrompt("candidate-fit-brief.md");
    private static readonly string ApplicationStrategyPrompt = LoadPrompt("application-strategy.md");
    private static readonly string AssistedProfileImportPrompt = LoadPrompt("assisted-profile-import.md");

    private readonly HttpClient httpClient;
    private readonly AiOptions options;

    public OllamaAiProvider(HttpClient httpClient, AiOptions options)
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
        var checks = new List<AiDiagnosticCheck>();

        try
        {
            using var response = await httpClient.GetAsync("/api/tags", ct);
            if (!response.IsSuccessStatusCode)
            {
                var message = $"Ollama returned {(int)response.StatusCode} {response.ReasonPhrase}.";
                checks.Add(new AiDiagnosticCheck("connectivity", "unavailable", message));
                return Unavailable(message, checks);
            }

            var tags = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(cancellationToken: ct);
            checks.Add(new AiDiagnosticCheck("connectivity", "ok", "Ollama endpoint is reachable."));

            if (tags?.Models.Any(model => string.Equals(model.Name, options.Model, StringComparison.OrdinalIgnoreCase)) == true)
            {
                checks.Add(new AiDiagnosticCheck("model", "ok", $"Model {options.Model} is available."));
                return new AiDiagnosticsResult(
                    "Ollama",
                    options.Model,
                    options.Endpoint,
                    true,
                    "Ollama provider is available.",
                    checks);
            }

            var modelMessage = $"Model {options.Model} was not found in Ollama.";
            checks.Add(new AiDiagnosticCheck("model", "unavailable", modelMessage));
            return Unavailable(modelMessage, checks);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            const string message = "Ollama endpoint is unavailable.";
            checks.Add(new AiDiagnosticCheck("connectivity", "unavailable", message));
            return Unavailable(message, checks);
        }
        catch (HttpRequestException)
        {
            const string message = "Ollama endpoint is unavailable.";
            checks.Add(new AiDiagnosticCheck("connectivity", "unavailable", message));
            return Unavailable(message, checks);
        }
    }

    public async Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisInput input, CancellationToken ct)
    {
        var responseText = await GenerateAsync(BuildJobAnalysisPrompt(input), attemptCount: 1, ct);
        try
        {
            return ParseJobAnalysis(responseText, attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await GenerateAsync(BuildRepairPrompt(responseText, firstFailure.Message), attemptCount: 2, ct);
            return ParseJobAnalysis(repairedText, attemptCount: 2);
        }
    }

    public async Task<EvidenceMatchResult> MatchEvidenceAsync(EvidenceMatchInput input, CancellationToken ct)
    {
        var responseText = await GenerateAsync(BuildEvidenceMatchingPrompt(input), attemptCount: 1, ct);
        try
        {
            return ParseEvidenceMatching(responseText, input, attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await GenerateAsync(BuildEvidenceMatchingRepairPrompt(responseText, firstFailure.Message, input), attemptCount: 2, ct);
            return ParseEvidenceMatching(repairedText, input, attemptCount: 2);
        }
    }

    public async Task<DraftGenerationResult> GenerateDraftAsync(DraftGenerationInput input, CancellationToken ct)
    {
        var responseText = await GenerateAsync(BuildDraftGenerationPrompt(input), attemptCount: 1, ct);
        try
        {
            return ParseDraftGeneration(responseText, attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await GenerateAsync(BuildDraftGenerationRepairPrompt(responseText, firstFailure.Message), attemptCount: 2, ct);
            return ParseDraftGeneration(repairedText, attemptCount: 2);
        }
    }

    public async Task<ClaimAuditResult> AuditClaimsAsync(ClaimAuditInput input, CancellationToken ct)
    {
        var responseText = await GenerateAsync(BuildClaimAuditPrompt(input), attemptCount: 1, ct);
        try
        {
            return ParseClaimAudit(responseText, input, attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await GenerateAsync(BuildClaimAuditRepairPrompt(responseText, firstFailure.Message), attemptCount: 2, ct);
            return ParseClaimAudit(repairedText, input, attemptCount: 2);
        }
    }

    public async Task<CandidateFitBriefResult> GenerateCandidateFitBriefAsync(CandidateFitBriefInput input, CancellationToken ct)
    {
        var responseText = await GenerateAsync(BuildCandidateFitBriefPrompt(input), attemptCount: 1, ct);
        try
        {
            return CandidateFitBriefJsonParser.Parse(responseText, input, "Ollama", attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await GenerateAsync(BuildCandidateFitBriefRepairPrompt(responseText, firstFailure.Message), attemptCount: 2, ct);
            return CandidateFitBriefJsonParser.Parse(repairedText, input, "Ollama", attemptCount: 2);
        }
    }

    public async Task<ApplicationStrategyResult> GenerateApplicationStrategyAsync(ApplicationStrategyInput input, CancellationToken ct)
    {
        var responseText = await GenerateAsync(BuildApplicationStrategyPrompt(input), attemptCount: 1, ct);
        try
        {
            return ApplicationStrategyJsonParser.Parse(responseText, input, "Ollama", attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await GenerateAsync(BuildApplicationStrategyRepairPrompt(responseText, firstFailure.Message, input), attemptCount: 2, ct);
            return ApplicationStrategyJsonParser.Parse(repairedText, input, "Ollama", attemptCount: 2);
        }
    }

    public async Task<AssistedProfileImportResult> ImportProfileFactsAsync(AssistedProfileImportInput input, CancellationToken ct)
    {
        var responseText = await GenerateAsync(BuildAssistedProfileImportPrompt(input), attemptCount: 1, ct);
        try
        {
            return ParseAssistedProfileImport(responseText, attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await GenerateAsync(BuildRepairPrompt(responseText, firstFailure.Message), attemptCount: 2, ct);
            return ParseAssistedProfileImport(repairedText, attemptCount: 2);
        }
    }

    private AiDiagnosticsResult Unavailable(string message, IReadOnlyList<AiDiagnosticCheck> checks) =>
        new("Ollama", options.Model, options.Endpoint, false, message, checks);

    private async Task<string> GenerateAsync(string prompt, int attemptCount, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "/api/generate",
                new
                {
                    model = options.Model,
                    prompt,
                    stream = false,
                    format = "json"
                },
                JsonOptions,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new AiProviderUnavailableException(
                    $"Ollama returned {(int)response.StatusCode} {response.ReasonPhrase}.",
                    attemptCount);
            }

            OllamaGenerateResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(JsonOptions, ct);
            }
            catch (JsonException exception)
            {
                throw new AiInvalidOutputException("Ollama returned malformed generate JSON.", attemptCount, exception);
            }

            if (payload is null || string.IsNullOrWhiteSpace(payload.Response))
            {
                throw new AiInvalidOutputException("Ollama returned an empty response.", attemptCount);
            }

            return payload.Response;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new AiProviderUnavailableException("Ollama endpoint is unavailable.", attemptCount);
        }
        catch (HttpRequestException exception)
        {
            throw new AiProviderUnavailableException("Ollama endpoint is unavailable.", attemptCount, exception);
        }
    }

    private static JobAnalysisResult ParseJobAnalysis(string responseText, int attemptCount)
    {
        OllamaJobAnalysisResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OllamaJobAnalysisResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException("Ollama returned malformed job analysis JSON.", attemptCount, exception);
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
            throw new AiInvalidOutputException("Ollama returned structurally invalid job analysis JSON.", attemptCount);
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
            throw new AiInvalidOutputException("Ollama returned structurally invalid job signal JSON.", attemptCount);
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
            throw new AiInvalidOutputException("Ollama returned job analysis without any job signals.", attemptCount);
        }

        var document = new JobSignalsDocument(
            "Ollama",
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

    private static EvidenceMatchResult ParseEvidenceMatching(string responseText, EvidenceMatchInput input, int attemptCount)
    {
        OllamaEvidenceMatchingResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OllamaEvidenceMatchingResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException("Ollama returned malformed evidence matching JSON.", attemptCount, exception);
        }

        if (payload is null ||
            payload.EvidenceMatches is null ||
            payload.UnmatchedRequirements is null)
        {
            throw new AiInvalidOutputException("Ollama returned structurally invalid evidence matching JSON.", attemptCount);
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
                string.IsNullOrWhiteSpace(match.Quality) ||
                string.IsNullOrWhiteSpace(match.Reason) ||
                match.MatchedTerms is null ||
                match.MatchedTerms.Count == 0 ||
                match.MatchedTerms.All(string.IsNullOrWhiteSpace) ||
                !EvidenceQuality.IsValid(match.Quality.Trim()) ||
                !signalsById.TryGetValue(match.SignalId.Trim(), out var signal) ||
                !Guid.TryParse(match.ProfileFactId, out var profileFactId) ||
                !approvedFactsById.TryGetValue(profileFactId, out var fact))
            {
                throw new AiInvalidOutputException("Ollama returned structurally invalid evidence match JSON.", attemptCount);
            }

            if (!matchKeys.Add($"{signal.Id}:{fact.Id:N}"))
            {
                throw new AiInvalidOutputException("Ollama returned duplicate evidence matches for a job signal and profile fact.", attemptCount);
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
                match.MatchedTerms.Where(term => !string.IsNullOrWhiteSpace(term)).Select(term => term.Trim()).ToList(),
                match.Quality.Trim(),
                match.Reason.Trim()));
        }

        foreach (var requirement in payload.UnmatchedRequirements)
        {
            if (requirement is null ||
                string.IsNullOrWhiteSpace(requirement.SignalId) ||
                string.IsNullOrWhiteSpace(requirement.Recommendation) ||
                !signalsById.TryGetValue(requirement.SignalId.Trim(), out var signal))
            {
                throw new AiInvalidOutputException("Ollama returned structurally invalid unmatched requirement JSON.", attemptCount);
            }

            if (!unmatchedSignalIds.Add(signal.Id))
            {
                throw new AiInvalidOutputException("Ollama returned duplicate unmatched requirements for a job signal.", attemptCount);
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
            throw new AiInvalidOutputException("Ollama returned incomplete or conflicting evidence matching JSON.", attemptCount);
        }

        return new EvidenceMatchResult(matches, unmatched)
        {
            AttemptCount = attemptCount
        };
    }

    private static DraftGenerationResult ParseDraftGeneration(string responseText, int attemptCount)
    {
        OllamaDraftGenerationResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OllamaDraftGenerationResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException("Ollama returned malformed draft generation JSON.", attemptCount, exception);
        }

        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.CoverLetterText) ||
            string.IsNullOrWhiteSpace(payload.ShortMotivationText))
        {
            throw new AiInvalidOutputException("Ollama returned structurally invalid draft generation JSON.", attemptCount);
        }

        return new DraftGenerationResult(
            payload.CoverLetterText.Trim(),
            payload.ShortMotivationText.Trim())
        {
            AttemptCount = attemptCount
        };
    }

    private static ClaimAuditResult ParseClaimAudit(string responseText, ClaimAuditInput input, int attemptCount)
    {
        OllamaClaimAuditResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OllamaClaimAuditResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException("Ollama returned malformed claim audit JSON.", attemptCount, exception);
        }

        if (payload?.Claims is null)
        {
            throw new AiInvalidOutputException("Ollama returned structurally invalid claim audit JSON.", attemptCount);
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
                throw new AiInvalidOutputException("Ollama returned structurally invalid claim audit JSON.", attemptCount);
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

    private static AssistedProfileImportResult ParseAssistedProfileImport(string responseText, int attemptCount)
    {
        OllamaAssistedProfileImportResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OllamaAssistedProfileImportResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException("Ollama returned malformed assisted profile import JSON.", attemptCount, exception);
        }

        if (payload?.Facts is null || payload.Facts.Count == 0)
        {
            throw new AiInvalidOutputException("Ollama returned assisted profile import without facts.", attemptCount);
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
                throw new AiInvalidOutputException("Ollama returned structurally invalid assisted profile import fact JSON.", attemptCount);
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
        var candidateFitBrief = input.CandidateFitBrief is null
            ? null
            : new
            {
                input.CandidateFitBrief.CandidateSummary,
                input.CandidateFitBrief.SkillGroups,
                input.CandidateFitBrief.Competencies,
                input.CandidateFitBrief.RelevantProjects,
                input.CandidateFitBrief.TransferableStrengths,
                input.CandidateFitBrief.RiskNotes
            };

        return $"""
        {EvidenceMatchingPrompt}

        Job signals:
        {JsonSerializer.Serialize(signals, JsonOptions)}

        Approved profile facts:
        {JsonSerializer.Serialize(approvedFacts, JsonOptions)}

        Candidate fit brief context:
        {JsonSerializer.Serialize(candidateFitBrief, JsonOptions)}
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
            evidence.MatchedTerms,
            evidence.Quality,
            evidence.Reason,
            UseGuidance = EvidenceUseGuidance(evidence)
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

        Application strategy:
        {JsonSerializer.Serialize(input.ApplicationStrategy, JsonOptions)}
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

    private static string BuildCandidateFitBriefPrompt(CandidateFitBriefInput input)
    {
        var approvedFacts = input.ApprovedProfileFacts.Select(fact => new
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
        {CandidateFitBriefPrompt}

        Application metadata:
        {JsonSerializer.Serialize(new
        {
            input.CompanyName,
            input.RoleTitle,
            input.ApplicationUrl,
            input.Deadline,
            input.SelectedLanguage,
            input.TonePreference
        }, JsonOptions)}

        Job posting text:
        {input.JobPostingText ?? "(none supplied)"}

        Job signals:
        {JsonSerializer.Serialize(input.JobSignals, JsonOptions)}

        Approved profile facts:
        {JsonSerializer.Serialize(approvedFacts, JsonOptions)}
        """;
    }

    private static string BuildApplicationStrategyPrompt(ApplicationStrategyInput input)
    {
        var approvedEvidence = input.ApprovedEvidence.Select(evidence => new
        {
            evidence.Id,
            evidence.SignalId,
            evidence.Signal,
            evidence.Category,
            evidence.ProfileFactId,
            evidence.ProfileFactTitle,
            evidence.Summary,
            evidence.MatchedTerms,
            evidence.Quality,
            evidence.Reason
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
        {ApplicationStrategyPrompt}

        Application context:
        {JsonSerializer.Serialize(new
        {
            input.JobAnalysis.CompanyName,
            input.JobAnalysis.RoleTitle,
            input.JobAnalysis.DetectedLanguage,
            input.JobAnalysis.SelectedLanguage,
            RequestedLanguage = input.SelectedLanguage,
            input.TonePreference
        }, JsonOptions)}

        Job analysis:
        {JsonSerializer.Serialize(input.JobAnalysis.JobSignals, JsonOptions)}

        Candidate fit brief:
        {JsonSerializer.Serialize(input.CandidateFitBrief, JsonOptions)}

        Approved evidence:
        {JsonSerializer.Serialize(approvedEvidence, JsonOptions)}

        Unmatched requirements:
        {JsonSerializer.Serialize(input.UnmatchedRequirements, JsonOptions)}

        Gap decisions:
        {JsonSerializer.Serialize(input.GapDecisions, JsonOptions)}

        Approved job-local custom facts:
        {JsonSerializer.Serialize(approvedCustomFacts, JsonOptions)}
        """;
    }

    private static string EvidenceUseGuidance(EvidenceMatch evidence) =>
        evidence.Quality switch
        {
            EvidenceQuality.Weak => "Weak evidence must not support direct experience claims. Use only as adjacent context or omit it.",
            EvidenceQuality.Partial => "Partial evidence may guide cautious wording. Avoid claiming full direct experience.",
            _ => "Strong evidence may support direct experience claims when the summary supports them."
        };

    private static string BuildRepairPrompt(string invalidJson, string validationError) =>
        $"""
        Repair this job analysis JSON so it matches the required contract exactly.
        Return only strict JSON. Do not include markdown.

        Validation error:
        {validationError}

        Invalid JSON:
        {invalidJson}
        """;

    private static string BuildEvidenceMatchingRepairPrompt(string invalidJson, string validationError, EvidenceMatchInput input)
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
        var candidateFitBrief = input.CandidateFitBrief is null
            ? null
            : new
            {
                input.CandidateFitBrief.CandidateSummary,
                input.CandidateFitBrief.SkillGroups,
                input.CandidateFitBrief.Competencies,
                input.CandidateFitBrief.RelevantProjects,
                input.CandidateFitBrief.TransferableStrengths,
                input.CandidateFitBrief.RiskNotes
            };

        return
        $"""
        Repair this evidence matching JSON so it matches the required contract exactly.
        Return only strict JSON. Do not include markdown.
        Every job signal id listed below must appear exactly once: either in evidenceMatches or in unmatchedRequirements.
        Use only the listed approved profile fact ids for evidenceMatches.
        Evidence matches must include quality exactly as Strong, Partial, or Weak, and a reviewer-facing reason.
        For unmatchedRequirements, include signalId and recommendation; the API will derive display fields from the job signal.

        Validation error:
        {validationError}

        Job signals:
        {JsonSerializer.Serialize(signals, JsonOptions)}

        Approved profile facts:
        {JsonSerializer.Serialize(approvedFacts, JsonOptions)}

        Candidate fit brief context:
        {JsonSerializer.Serialize(candidateFitBrief, JsonOptions)}

        Invalid JSON:
        {invalidJson}
        """;
    }

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

    private static string BuildCandidateFitBriefRepairPrompt(string invalidJson, string validationError) =>
        $"""
        Repair this candidate fit brief JSON so it matches the required contract exactly.
        Return only strict JSON. Do not include markdown.

        Validation error:
        {validationError}

        Invalid JSON:
        {invalidJson}
        """;

    private static string BuildApplicationStrategyRepairPrompt(
        string invalidJson,
        string validationError,
        ApplicationStrategyInput input) =>
        $"""
        Repair this application strategy JSON so it matches the required contract exactly.
        Return only strict JSON. Do not include markdown.
        Use only approved evidence ids from the supplied approved evidence.
        Use profileFactIds only for narrative context traceability, and only when listed in approved evidence or candidate fit brief.
        All arrays are required, even when empty.

        Validation error:
        {validationError}

        Approved evidence ids:
        {JsonSerializer.Serialize(input.ApprovedEvidence.Select(evidence => evidence.Id), JsonOptions)}

        Valid profile fact ids:
        {JsonSerializer.Serialize(ValidApplicationStrategyProfileFactIds(input), JsonOptions)}

        Unmatched requirement ids:
        {JsonSerializer.Serialize(input.UnmatchedRequirements.Select(requirement => requirement.Id), JsonOptions)}

        Invalid JSON:
        {invalidJson}
        """;

    private static IEnumerable<Guid> ValidApplicationStrategyProfileFactIds(ApplicationStrategyInput input) =>
        input.ApprovedEvidence.Select(evidence => evidence.ProfileFactId)
            .Concat(input.CandidateFitBrief.SkillGroups.SelectMany(group => group.Items).SelectMany(item => item.SupportingProfileFactIds))
            .Concat(input.CandidateFitBrief.Competencies.SelectMany(item => item.SupportingProfileFactIds))
            .Concat(input.CandidateFitBrief.RelevantProjects.SelectMany(item => item.SupportingProfileFactIds))
            .Concat(input.CandidateFitBrief.TransferableStrengths.SelectMany(item => item.SupportingProfileFactIds))
            .Concat(input.CandidateFitBrief.RiskNotes.SelectMany(item => item.SupportingProfileFactIds))
            .Distinct();

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

    private sealed record OllamaTagsResponse(IReadOnlyList<OllamaModel> Models);

    private sealed record OllamaModel(string Name);

    private sealed record OllamaGenerateResponse(string Response);

    private sealed record OllamaJobAnalysisResponse(
        string CompanyName,
        string RoleTitle,
        string DetectedLanguage,
        string SelectedLanguage,
        OllamaJobSignalsResponse JobSignals);

    private sealed record OllamaJobSignalsResponse(
        IReadOnlyList<string> RequiredSkills,
        IReadOnlyList<string> PreferredSkills,
        IReadOnlyList<string> Responsibilities,
        IReadOnlyList<OllamaJobSignalResponse> Signals);

    private sealed record OllamaJobSignalResponse(
        string Id,
        string Label,
        string Category,
        IReadOnlyList<string> Keywords);

    private sealed record OllamaEvidenceMatchingResponse(
        IReadOnlyList<OllamaEvidenceMatchResponse> EvidenceMatches,
        IReadOnlyList<OllamaUnmatchedRequirementResponse> UnmatchedRequirements);

    private sealed record OllamaEvidenceMatchResponse(
        string SignalId,
        string ProfileFactId,
        string Summary,
        string Quality,
        string Reason,
        IReadOnlyList<string> MatchedTerms);

    private sealed record OllamaUnmatchedRequirementResponse(
        string SignalId,
        string Recommendation);

    private sealed record OllamaDraftGenerationResponse(
        string CoverLetterText,
        string ShortMotivationText);

    private sealed record OllamaClaimAuditResponse(
        IReadOnlyList<OllamaClaimAuditClaimResponse> Claims);

    private sealed record OllamaClaimAuditClaimResponse(
        string Id,
        string Text,
        string Status,
        IReadOnlyList<string> EvidenceIds);

    private sealed record OllamaAssistedProfileImportResponse(
        IReadOnlyList<OllamaAssistedProfileImportFactResponse> Facts);

    private sealed record OllamaAssistedProfileImportFactResponse(
        string Type,
        string Title,
        string Summary,
        IReadOnlyList<string> FactItems,
        IReadOnlyList<string> Technologies,
        IReadOnlyList<string> AllowedClaims,
        IReadOnlyList<string> ForbiddenClaims,
        string? SourceContext);
}
