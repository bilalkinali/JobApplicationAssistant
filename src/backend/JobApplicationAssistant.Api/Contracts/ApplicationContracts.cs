namespace JobApplicationAssistant.Api.Contracts;

public sealed record ApplicationRequest(
    string CompanyName,
    string RoleTitle,
    string? ApplicationUrl,
    DateOnly? Deadline,
    string Status,
    string JobPostingText,
    string? DetectedLanguage,
    string? SelectedLanguage);

public sealed record ApplicationResponse(
    Guid Id,
    string CompanyName,
    string RoleTitle,
    string? ApplicationUrl,
    DateOnly? Deadline,
    string Status,
    string JobPostingText,
    string? DetectedLanguage,
    string? SelectedLanguage,
    string JobSignals,
    string EvidenceMatches,
    string UnmatchedRequirements,
    string CandidateFitBrief,
    string ApprovedEvidence,
    string GapDecisions,
    string CustomFacts,
    DateTimeOffset? LastPreparedAt,
    string PreparationStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    GeneratedDraftResponse? GeneratedDraft,
    bool HasGeneratedDraft,
    string AuditReadiness);

public sealed record PrepareApplicationResponse(
    ApplicationResponse Application,
    string NextCheckpoint,
    string Message);

public sealed record ApprovedEvidenceRequest(string? ApprovedEvidence);

public sealed record GapDecisionsRequest(string? GapDecisions);

public sealed record CustomFactRequest(
    string? UnmatchedRequirementId,
    string? Title,
    string? Summary,
    string[]? Technologies,
    string[]? AllowedClaims);

public sealed record CustomFactStatusRequest(string? Status);

public sealed record ApplicationStatusRequest(string Status);

public sealed record GeneratedDraftEditRequest(
    string CoverLetterText,
    string ShortMotivationText);

public sealed record GeneratedDraftResponse(
    Guid Id,
    Guid JobApplicationId,
    string CoverLetterText,
    string ShortMotivationText,
    string ClaimAudit,
    DateTimeOffset GeneratedAt,
    DateTimeOffset? LastEditedAt,
    DateTimeOffset? AuditUpdatedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsClaimAuditStale);
