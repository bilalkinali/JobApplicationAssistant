namespace JobApplicationAssistant.Api.Domain;

public sealed class JobApplication
{
    public Guid Id { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string RoleTitle { get; set; } = string.Empty;

    public string? ApplicationUrl { get; set; }

    public DateOnly? Deadline { get; set; }

    public string Status { get; set; } = string.Empty;

    public string JobPostingText { get; set; } = string.Empty;

    public string? DetectedLanguage { get; set; }

    public string? SelectedLanguage { get; set; }

    public string JobSignals { get; set; } = "{}";

    public string EvidenceMatches { get; set; } = "[]";

    public string UnmatchedRequirements { get; set; } = "[]";

    public string ApprovedEvidence { get; set; } = "[]";

    public string CustomFacts { get; set; } = "[]";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public GeneratedDraft? GeneratedDraft { get; set; }

    public ICollection<AiRun> AiRuns { get; set; } = [];
}
