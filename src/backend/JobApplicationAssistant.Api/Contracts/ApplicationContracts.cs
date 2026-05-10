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
    string ApprovedEvidence,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ApprovedEvidenceRequest(string? ApprovedEvidence);

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
    DateTimeOffset UpdatedAt);
