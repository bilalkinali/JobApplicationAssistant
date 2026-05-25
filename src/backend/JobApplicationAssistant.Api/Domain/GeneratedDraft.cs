namespace JobApplicationAssistant.Api.Domain;

public sealed class GeneratedDraft
{
    public Guid Id { get; set; }

    public Guid JobApplicationId { get; set; }

    public JobApplication JobApplication { get; set; } = null!;

    public string CoverLetterText { get; set; } = string.Empty;

    public string ShortMotivationText { get; set; } = string.Empty;

    public string ClaimAudit { get; set; } = "{}";

    public string DraftQualityCheck { get; set; } = "{}";

    public DateTimeOffset GeneratedAt { get; set; }

    public DateTimeOffset? LastEditedAt { get; set; }

    public DateTimeOffset? AuditUpdatedAt { get; set; }

    public bool IsClaimAuditStale { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
