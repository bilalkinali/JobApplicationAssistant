using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JobApplicationAssistant.Api.Ai;

public static partial class DraftQualityChecker
{
    public const int CopiedSevenWordPhraseThreshold = 3;

    private static readonly string[] MojibakeMarkers = ["Ã", "Â", "â€"];

    private static readonly string[] GenericDanishPhrases =
    [
        "jeg skriver for at udtrykke min interesse",
        "muligheden for at blive en del af",
        "spændende mulighed",
        "jeg brænder for",
        "med min baggrund inden for",
        "jeg er overbevist om",
        "vil jeg kunne bidrage positivt",
        "passer perfekt til min profil"
    ];

    private static readonly string[] AwkwardDanishPhrases =
    [
        "en stærk forståelse for at arbejde med",
        "jeg har en stor motivation for at lære",
        "jeg ser frem til muligheden for",
        "jeg vil meget gerne bringe mine kompetencer i spil",
        "i rollen som den rette kandidat",
        "event-driven integration platform",
        "en grad af studier i computer science",
        "st\u00e6rk grundlagning",
        "jeg er fluent",
        "interesseret i at l\u00e6rer",
        "ikke-matched preferred skills"
    ];

    public static string RepairCommonMojibake(string value)
    {
        var trimmed = value.Trim();
        if (!LooksLikeMojibake(trimmed))
        {
            return trimmed;
        }

        try
        {
            var repaired = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(trimmed));
            return CountMojibakeMarkers(repaired) < CountMojibakeMarkers(trimmed)
                ? repaired.Trim()
                : trimmed;
        }
        catch (EncoderFallbackException)
        {
            return trimmed;
        }
    }

    public static DraftQualityCheckResult Check(
        string coverLetterText,
        string shortMotivationText,
        string jobPostingText,
        string? selectedLanguage,
        IReadOnlyList<EvidenceMatch> approvedEvidence)
    {
        var issues = new List<DraftQualityIssue>();
        var combinedDraft = $"{coverLetterText}\n\n{shortMotivationText}";
        var copiedPhraseCount = CountCopiedSevenWordPhrases(jobPostingText, combinedDraft);

        if (copiedPhraseCount > CopiedSevenWordPhraseThreshold)
        {
            issues.Add(new DraftQualityIssue(
                "CopiedJobPostingPhrases",
                "NeedsRevision",
                $"Draft repeats {copiedPhraseCount} distinct seven-word phrases from the job posting; threshold is {CopiedSevenWordPhraseThreshold}."));
        }

        if (LooksLikeMojibake(combinedDraft))
        {
            issues.Add(new DraftQualityIssue(
                "DanishEncodingMojibake",
                "NeedsRevision",
                "Draft still contains likely UTF-8/Latin-1 mojibake such as Ã, Â, or â€ sequences."));
        }

        if (IsDanish(selectedLanguage))
        {
            AddDanishLanguageIssues(issues, coverLetterText, shortMotivationText);
        }

        AddGenericParagraphIssues(issues, coverLetterText, approvedEvidence);
        AddShortMotivationIssues(issues, shortMotivationText, coverLetterText);

        var status = issues.Any(issue => issue.Severity == "NeedsRevision")
            ? "NeedsRevision"
            : "Passed";

        return new DraftQualityCheckResult(
            status,
            issues,
            copiedPhraseCount,
            CopiedSevenWordPhraseThreshold);
    }

    public static bool NeedsRevision(string? draftQualityCheck)
    {
        if (string.IsNullOrWhiteSpace(draftQualityCheck))
        {
            return false;
        }

        try
        {
            var result = JsonSerializer.Deserialize<DraftQualityCheckResult>(
                draftQualityCheck,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return string.Equals(result?.Status, "NeedsRevision", StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void AddDanishLanguageIssues(List<DraftQualityIssue> issues, string coverLetterText, string shortMotivationText)
    {
        var combined = NormalizeForSearch($"{coverLetterText}\n{shortMotivationText}");
        if (GenericDanishPhrases.Any(phrase => combined.Contains(phrase, StringComparison.Ordinal)) ||
            AwkwardDanishPhrases.Any(phrase => combined.Contains(phrase, StringComparison.Ordinal)))
        {
            issues.Add(new DraftQualityIssue(
                "UnnaturalDanishPhrasing",
                "NeedsRevision",
                "Draft contains Danish phrasing that is likely generic, translated, or awkward."));
        }

        if (MistakePattern().IsMatch(combined))
        {
            issues.Add(new DraftQualityIssue(
                "DanishLanguageMistake",
                "NeedsRevision",
                "Draft contains obvious Danish spelling or language mistakes."));
        }
    }

    private static void AddGenericParagraphIssues(List<DraftQualityIssue> issues, string coverLetterText, IReadOnlyList<EvidenceMatch> approvedEvidence)
    {
        var anchors = approvedEvidence
            .SelectMany(evidence => evidence.MatchedTerms)
            .Concat(approvedEvidence.Select(evidence => evidence.ProfileFactTitle))
            .Concat(approvedEvidence.Select(evidence => evidence.Signal))
            .Select(NormalizeForSearch)
            .Where(anchor => anchor.Length >= 4)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var genericParagraphCount = SplitParagraphs(coverLetterText)
            .Count(paragraph =>
            {
                var normalized = NormalizeForSearch(paragraph);
                return normalized.Length > 140 &&
                    GenericDanishPhrases.Any(phrase => normalized.Contains(phrase, StringComparison.Ordinal)) &&
                    !anchors.Any(anchor => normalized.Contains(anchor, StringComparison.Ordinal));
            });

        if (genericParagraphCount > 0)
        {
            issues.Add(new DraftQualityIssue(
                "OverlyGenericParagraph",
                "NeedsRevision",
                "Draft contains paragraphs that read as generic motivation instead of evidence-grounded fit."));
        }
    }

    private static void AddShortMotivationIssues(List<DraftQualityIssue> issues, string shortMotivationText, string coverLetterText)
    {
        var words = WordTokens(shortMotivationText).ToList();
        if (words.Count < 18)
        {
            issues.Add(new DraftQualityIssue(
                "WeakShortMotivation",
                "NeedsRevision",
                "Short motivation is too thin to work as a useful standalone pitch."));
            return;
        }

        var motivationSevenGrams = SevenGrams(words).ToHashSet(StringComparer.Ordinal);
        var coverLetterSevenGrams = SevenGrams(WordTokens(coverLetterText)).ToHashSet(StringComparer.Ordinal);
        if (motivationSevenGrams.Count > 0 && motivationSevenGrams.Count(coverLetterSevenGrams.Contains) > 0)
        {
            issues.Add(new DraftQualityIssue(
                "WeakShortMotivation",
                "NeedsRevision",
                "Short motivation repeats cover letter phrasing instead of acting as a distinct concise pitch."));
        }
    }

    private static int CountCopiedSevenWordPhrases(string sourceText, string draftText)
    {
        var sourceGrams = SevenGrams(WordTokens(sourceText)).ToHashSet(StringComparer.Ordinal);
        if (sourceGrams.Count == 0)
        {
            return 0;
        }

        return SevenGrams(WordTokens(draftText))
            .Where(sourceGrams.Contains)
            .Distinct(StringComparer.Ordinal)
            .Count();
    }

    private static IEnumerable<string> SevenGrams(IEnumerable<string> tokens)
    {
        var window = new Queue<string>(7);
        foreach (var token in tokens)
        {
            window.Enqueue(token);
            if (window.Count < 7)
            {
                continue;
            }

            if (window.Count > 7)
            {
                window.Dequeue();
            }

            yield return string.Join(' ', window);
        }
    }

    private static IEnumerable<string> WordTokens(string value)
    {
        foreach (Match match in WordPattern().Matches(NormalizeForSearch(value)))
        {
            yield return match.Value;
        }
    }

    private static IReadOnlyList<string> SplitParagraphs(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool IsDanish(string? selectedLanguage) =>
        string.Equals(selectedLanguage, "Danish", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(selectedLanguage, "Dansk", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeMojibake(string value) =>
        MojibakeMarkers.Any(marker => value.Contains(marker, StringComparison.Ordinal));

    private static int CountMojibakeMarkers(string value) =>
        MojibakeMarkers.Sum(marker => value.Split(marker, StringSplitOptions.None).Length - 1);

    private static string NormalizeForSearch(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormC).ToLower(CultureInfo.GetCultureInfo("da-DK"));
        return WhitespacePattern().Replace(normalized, " ").Trim();
    }

    [GeneratedRegex(@"\p{L}[\p{L}\p{Mn}\p{Pd}']*")]
    private static partial Regex WordPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"\b(jeg er erfaring|forstã|kã)\b", RegexOptions.IgnoreCase)]
    private static partial Regex MistakePattern();
}

public sealed record DraftQualityCheckResult(
    string Status,
    IReadOnlyList<DraftQualityIssue> Issues,
    int CopiedSevenWordPhraseCount,
    int CopiedPhraseThreshold);

public sealed record DraftQualityIssue(
    string Code,
    string Severity,
    string Message);
