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
    DateTimeOffset UpdatedAt);
