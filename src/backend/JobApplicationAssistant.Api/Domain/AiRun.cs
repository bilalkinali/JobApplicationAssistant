namespace JobApplicationAssistant.Api.Domain;

public sealed class AiRun
{
    public Guid Id { get; set; }

    public Guid? JobApplicationId { get; set; }

    public JobApplication? JobApplication { get; set; }

    public string Step { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? InputSummary { get; set; }

    public string? OutputSummary { get; set; }
}
