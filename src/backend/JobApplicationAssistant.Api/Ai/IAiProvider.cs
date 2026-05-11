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
    IReadOnlyList<ProfileFact> ApprovedFacts);

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
    IReadOnlyList<UnmatchedRequirement> UnmatchedRequirements);

public sealed record DraftGenerationResult(
    string CoverLetterText,
    string ShortMotivationText)
{
    public int AttemptCount { get; init; } = 1;
}

public sealed record ClaimAuditInput(
    string CoverLetterText,
    string ShortMotivationText,
    IReadOnlyList<EvidenceMatch> ApprovedEvidence);

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
    IReadOnlyList<string> MatchedTerms);

public sealed record UnmatchedRequirement(
    string Id,
    string SignalId,
    string Requirement,
    string Category,
    string Recommendation);
