namespace JobApplicationAssistant.Api.Domain;

public sealed class ProfileFact
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public ProfileFactStatus Status { get; set; } = ProfileFactStatus.Draft;

    public string FactItems { get; set; } = "[]";

    public string Technologies { get; set; } = "[]";

    public string AllowedClaims { get; set; } = "[]";

    public string ForbiddenClaims { get; set; } = "[]";

    public string SourceDocumentIds { get; set; } = "[]";

    public string? OriginalImportedSnapshot { get; set; }

    public bool ManuallyEdited { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
