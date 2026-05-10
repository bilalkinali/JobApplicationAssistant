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
