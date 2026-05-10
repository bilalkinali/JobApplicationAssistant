namespace JobApplicationAssistant.Api.Domain;

public sealed class Profile
{
    public Guid Id { get; set; }

    public int SingletonKey { get; private set; } = 1;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Location { get; set; }

    public string? LinkedInUrl { get; set; }

    public string? GitHubUrl { get; set; }

    public string? PortfolioUrl { get; set; }

    public string DefaultLanguage { get; set; } = string.Empty;

    public string? DanishTone { get; set; }

    public string? EnglishTone { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
