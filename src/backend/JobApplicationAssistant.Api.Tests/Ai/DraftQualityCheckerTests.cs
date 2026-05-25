using JobApplicationAssistant.Api.Ai;
using Xunit;

namespace JobApplicationAssistant.Api.Tests.Ai;

public sealed class DraftQualityCheckerTests
{
    [Fact]
    public void RepairCommonMojibake_restores_danish_characters()
    {
        var repaired = DraftQualityChecker.RepairCommonMojibake("KÃ¦re Vejle Kommune, jeg har forstÃ¥else for muligheden.");

        Assert.Contains("Kære", repaired);
        Assert.Contains("forståelse", repaired);
        Assert.DoesNotContain("Ã", repaired);
    }

    [Fact]
    public void Check_marks_draft_needing_revision_when_copied_phrase_threshold_is_exceeded()
    {
        var jobPosting = """
        Vi søger en udvikler der kan skabe robuste digitale løsninger i tæt samarbejde.
        Du skal kunne arbejde med komplekse integrationer mellem systemer og borgervendte services.
        Rollen kræver evnen til at omsætte forretningens behov til stabile tekniske løsninger.
        Vi lægger vægt på nysgerrighed ansvarlighed og god kommunikation i teamet.
        """;
        var coverLetter = """
        Jeg kan skabe robuste digitale løsninger i tæt samarbejde.

        Jeg kan arbejde med komplekse integrationer mellem systemer og borgervendte services.

        Jeg har evnen til at omsætte forretningens behov til stabile tekniske løsninger.

        Jeg arbejder med nysgerrighed ansvarlighed og god kommunikation i teamet.
        """;

        var result = DraftQualityChecker.Check(
            coverLetter,
            "Jeg bidrager med .NET, integrationer og erfaring fra konkrete projekter.",
            jobPosting,
            "Danish",
            []);

        Assert.Equal("NeedsRevision", result.Status);
        Assert.True(result.CopiedSevenWordPhraseCount > result.CopiedPhraseThreshold);
        Assert.Contains(result.Issues, issue => issue.Code == "CopiedJobPostingPhrases");
    }

    [Fact]
    public void Check_marks_danish_draft_needing_revision_when_it_contains_mixed_language_phrasing()
    {
        var coverLetter =
            "K\u00e6re Vejle Kommune,\n\n" +
            "Jeg har bygget et event-driven integration platform mellem Salesforce og .NET services.\n" +
            "Jeg har en grad af studier i computer science, der giver mig en st\u00e6rk grundlagning.\n" +
            "Jeg er fluent i flere sprog og interesseret i at l\u00e6rer om ikke-matched preferred skills.";

        var result = DraftQualityChecker.Check(
            coverLetter,
            "Jeg kan bidrage med integrationserfaring og teknisk nysgerrighed i rollen.",
            "Vi s\u00f8ger en kandidat, der kan skrive naturligt dansk.",
            "Danish",
            []);

        Assert.Equal("NeedsRevision", result.Status);
        Assert.Contains(result.Issues, issue => issue.Code == "UnnaturalDanishPhrasing");
    }

    [Fact]
    public void NeedsRevision_returns_true_only_for_needs_revision_quality_status()
    {
        Assert.True(DraftQualityChecker.NeedsRevision("""{"status":"NeedsRevision","issues":[]}"""));
        Assert.False(DraftQualityChecker.NeedsRevision("""{"status":"Passed","issues":[]}"""));
        Assert.False(DraftQualityChecker.NeedsRevision("{}"));
    }
}
