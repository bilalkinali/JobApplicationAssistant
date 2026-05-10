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
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
