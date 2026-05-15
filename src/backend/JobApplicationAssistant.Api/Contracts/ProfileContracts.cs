namespace JobApplicationAssistant.Api.Contracts;

public sealed record ProfileRequest(
    string FullName,
    string Email,
    string? Phone,
    string? Location,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? PortfolioUrl,
    string DefaultLanguage,
    string? DanishTone,
    string? EnglishTone);

public sealed record ProfileResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? Location,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? PortfolioUrl,
    string DefaultLanguage,
    string? DanishTone,
    string? EnglishTone,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProfileFactRequest(
    string? Type,
    string? Title,
    string? Summary,
    string? Status,
    string? FactItems,
    string? Technologies,
    string? AllowedClaims,
    string? ForbiddenClaims);

public sealed record ProfileFactResponse(
    Guid Id,
    string Type,
    string Title,
    string Summary,
    string Status,
    string FactItems,
    string Technologies,
    string AllowedClaims,
    string ForbiddenClaims,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string SourceDocumentIds = "[]",
    string? OriginalImportedSnapshot = null,
    bool ManuallyEdited = false);

public sealed record AssistedProfileImportResponse(
    Guid ImportSessionId,
    string FileName,
    int ImportedFactCount,
    IReadOnlyList<ProfileFactResponse> ProfileFacts,
    ImportedDraftFactReviewQueueResponse ReviewQueue,
    string ReviewUrl);

public sealed record ImportedDraftFactReviewQueueResponse(
    Guid ImportSessionId,
    string FileName,
    int DraftFactCount,
    IReadOnlyList<ImportedDraftFactReviewGroupResponse> Groups);

public sealed record ImportedDraftFactReviewGroupResponse(
    string Key,
    string Label,
    int DraftFactCount,
    IReadOnlyList<ImportedDraftFactReviewItemResponse> Facts);

public sealed record ImportedDraftFactReviewItemResponse(
    ProfileFactResponse ProfileFact,
    string SourceContext,
    bool HasDuplicateIndicators,
    IReadOnlyList<ImportedDraftFactDuplicateIndicatorResponse> DuplicateIndicators);

public sealed record ImportedDraftFactDuplicateIndicatorResponse(
    string Scope,
    Guid ProfileFactId,
    string ProfileFactTitle,
    string Reason);
