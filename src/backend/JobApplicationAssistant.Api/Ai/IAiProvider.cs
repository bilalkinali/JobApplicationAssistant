using JobApplicationAssistant.Api.Domain;

namespace JobApplicationAssistant.Api.Ai;

public interface IAiProvider
{
    Task<AiProviderStatus> GetStatusAsync(CancellationToken ct);

    Task<AiDiagnosticsResult> RunDiagnosticsAsync(CancellationToken ct);

    Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisInput input, CancellationToken ct);

    Task<EvidenceMatchResult> MatchEvidenceAsync(EvidenceMatchInput input, CancellationToken ct);

    Task<DraftGenerationResult> GenerateDraftAsync(DraftGenerationInput input, CancellationToken ct);

    Task<ClaimAuditResult> AuditClaimsAsync(ClaimAuditInput input, CancellationToken ct);

    Task<CandidateFitBriefResult> GenerateCandidateFitBriefAsync(CandidateFitBriefInput input, CancellationToken ct);

    Task<ApplicationStrategyResult> GenerateApplicationStrategyAsync(ApplicationStrategyInput input, CancellationToken ct);

    Task<AssistedProfileImportResult> ImportProfileFactsAsync(AssistedProfileImportInput input, CancellationToken ct);
}

public sealed record AiProviderStatus(
    string Provider,
    string Model,
    string? Endpoint,
    bool IsAvailable,
    string Message);

public sealed record AiDiagnosticsResult(
    string Provider,
    string Model,
    string? Endpoint,
    bool IsAvailable,
    string Message,
    IReadOnlyList<AiDiagnosticCheck> Checks);

public sealed record AiDiagnosticCheck(
    string Name,
    string Status,
    string Message);

public sealed record JobAnalysisInput(
    string CompanyName,
    string RoleTitle,
    string? SelectedLanguage,
    string JobPostingText);

public sealed record JobAnalysisResult(
    string CompanyName,
    string RoleTitle,
    string DetectedLanguage,
    string SelectedLanguage,
    JobSignalsDocument JobSignals)
{
    public int AttemptCount { get; init; } = 1;
}

public sealed record EvidenceMatchInput(
    IReadOnlyList<JobSignal> Signals,
    IReadOnlyList<ProfileFact> ApprovedFacts,
    CandidateFitBriefResult? CandidateFitBrief = null);

public sealed record EvidenceMatchResult(
    IReadOnlyList<EvidenceMatch> EvidenceMatches,
    IReadOnlyList<UnmatchedRequirement> UnmatchedRequirements)
{
    public int AttemptCount { get; init; } = 1;
}

public sealed record DraftGenerationInput(
    string CompanyName,
    string RoleTitle,
    string? SelectedLanguage,
    string? ApplicantName,
    string? TonePreference,
    IReadOnlyList<EvidenceMatch> ApprovedEvidence,
    IReadOnlyList<UnmatchedRequirement> UnmatchedRequirements,
    IReadOnlyList<DraftGapDecision> GapDecisions,
    IReadOnlyList<DraftCustomFact> ApprovedCustomFacts,
    ApplicationStrategyResult? ApplicationStrategy = null,
    DraftCandidateFitBriefContext? CandidateFitBriefContext = null);

public sealed record DraftCandidateFitBriefContext(
    string CandidateSummary,
    IReadOnlyList<DraftCandidateFitBriefGroup> SkillGroups,
    IReadOnlyList<DraftCandidateFitBriefItem> Competencies,
    IReadOnlyList<DraftCandidateFitBriefItem> RelevantProjects,
    IReadOnlyList<DraftCandidateFitBriefItem> TransferableStrengths,
    IReadOnlyList<DraftCandidateFitBriefItem> RiskNotes);

public sealed record DraftCandidateFitBriefGroup(
    string Name,
    IReadOnlyList<DraftCandidateFitBriefItem> Items);

public sealed record DraftCandidateFitBriefItem(
    string Title,
    string Summary);

public sealed record DraftGapDecision(
    string UnmatchedRequirementId,
    string Decision,
    Guid? CustomFactId);

public sealed record DraftCustomFact(
    Guid Id,
    string UnmatchedRequirementId,
    string Title,
    string Summary,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<string> AllowedClaims);

public sealed record DraftGenerationResult(
    string CoverLetterText,
    string ShortMotivationText)
{
    public int AttemptCount { get; init; } = 1;
}

public sealed record ClaimAuditInput(
    string CoverLetterText,
    string ShortMotivationText,
    IReadOnlyList<EvidenceMatch> ApprovedEvidence,
    IReadOnlyList<DraftCustomFact> ApprovedCustomFacts,
    IReadOnlyList<DraftGapDecision> GapDecisions,
    IReadOnlyList<ClaimAuditFitBriefSupportMapping> CandidateFitBriefSupportMappings);

public sealed record ClaimAuditFitBriefSupportMapping(
    string Section,
    string Title,
    string Summary,
    IReadOnlyList<Guid> SupportingProfileFactIds);

public sealed record ClaimAuditResult(
    IReadOnlyList<ClaimAuditClaim> Claims)
{
    public int AttemptCount { get; init; } = 1;
}

public sealed record ClaimAuditClaim(
    string Id,
    string Text,
    string Status,
    IReadOnlyList<string> EvidenceIds);

public sealed record CandidateFitBriefInput(
    string CompanyName,
    string RoleTitle,
    string? ApplicationUrl,
    DateOnly? Deadline,
    string? SelectedLanguage,
    string? TonePreference,
    string? JobPostingText,
    JobSignalsDocument? JobSignals,
    IReadOnlyList<ProfileFact> ApprovedProfileFacts);

public sealed record CandidateFitBriefResult(
    string CandidateSummary,
    IReadOnlyList<CandidateFitSkillGroup> SkillGroups,
    IReadOnlyList<CandidateFitBriefItem> Competencies,
    IReadOnlyList<CandidateFitBriefItem> RelevantProjects,
    IReadOnlyList<CandidateFitBriefItem> TransferableStrengths,
    IReadOnlyList<CandidateFitBriefItem> RiskNotes)
{
    public int AttemptCount { get; init; } = 1;
}

public sealed record CandidateFitSkillGroup(
    string Name,
    IReadOnlyList<CandidateFitBriefItem> Items);

public sealed record CandidateFitBriefItem(
    string Title,
    string Summary,
    // Traceability only. These ids must never be treated as approved evidence for final claims,
    // evidence review, draft generation, or claim audit.
    IReadOnlyList<Guid> SupportingProfileFactIds);

public sealed record ApplicationStrategyInput(
    JobAnalysisResult JobAnalysis,
    CandidateFitBriefResult CandidateFitBrief,
    IReadOnlyList<EvidenceMatch> ApprovedEvidence,
    IReadOnlyList<UnmatchedRequirement> UnmatchedRequirements,
    IReadOnlyList<DraftGapDecision> GapDecisions,
    IReadOnlyList<DraftCustomFact> ApprovedCustomFacts,
    string? SelectedLanguage,
    string? TonePreference);

public sealed record ApplicationStrategyResult(
    IReadOnlyList<ApplicationStrategyAngle> PrimaryAngles,
    IReadOnlyList<ApplicationStrategyAngle> SecondaryAngles,
    IReadOnlyList<ApplicationStrategyGapGuidance> GapHandlingGuidance,
    IReadOnlyList<ApplicationStrategyClaimToAvoid> ClaimsToAvoid,
    string ToneGuidance,
    IReadOnlyList<ApplicationStrategyOutlineItem> DraftOutline)
{
    public int AttemptCount { get; init; } = 1;
}

public sealed record ApplicationStrategyAngle(
    string Title,
    string Rationale,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<Guid> ProfileFactIds);

public sealed record ApplicationStrategyGapGuidance(
    string UnmatchedRequirementId,
    string Guidance);

public sealed record ApplicationStrategyClaimToAvoid(
    string Claim,
    string Reason);

public sealed record ApplicationStrategyOutlineItem(
    string Section,
    string Guidance,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<Guid> ProfileFactIds);

public sealed record AssistedProfileImportInput(
    string FileName,
    string ExtractedText);

public sealed record AssistedProfileImportResult(
    IReadOnlyList<AssistedProfileImportFact> Facts)
{
    public int AttemptCount { get; init; } = 1;
}

public sealed record AssistedProfileImportFact(
    string Type,
    string Title,
    string Summary,
    IReadOnlyList<string> FactItems,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<string> AllowedClaims,
    IReadOnlyList<string> ForbiddenClaims,
    string SourceContext);

public sealed record JobSignalsDocument(
    string Provider,
    DateTimeOffset ExtractedAt,
    IReadOnlyList<string> RequiredSkills,
    IReadOnlyList<string> PreferredSkills,
    IReadOnlyList<string> Responsibilities,
    IReadOnlyList<JobSignal> Signals);

public sealed record JobSignal(
    string Id,
    string Label,
    string Category,
    IReadOnlyList<string> Keywords);

public sealed record EvidenceMatch(
    string Id,
    string SignalId,
    string Signal,
    string Category,
    Guid ProfileFactId,
    string ProfileFactTitle,
    string Summary,
    IReadOnlyList<string> MatchedTerms,
    string Quality = EvidenceQuality.Strong,
    string Reason = "Direct match against approved profile evidence.");

public static class EvidenceQuality
{
    public const string Strong = "Strong";
    public const string Partial = "Partial";
    public const string Weak = "Weak";

    public static bool IsValid(string? quality) =>
        string.Equals(quality, Strong, StringComparison.Ordinal) ||
        string.Equals(quality, Partial, StringComparison.Ordinal) ||
        string.Equals(quality, Weak, StringComparison.Ordinal);
}

public sealed record UnmatchedRequirement(
    string Id,
    string SignalId,
    string Requirement,
    string Category,
    string Recommendation);
