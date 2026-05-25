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
}
