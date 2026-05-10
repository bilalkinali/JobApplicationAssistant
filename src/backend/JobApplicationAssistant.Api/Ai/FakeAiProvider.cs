using JobApplicationAssistant.Api.Domain;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JobApplicationAssistant.Api.Ai;

public sealed partial class FakeAiProvider : IAiProvider
{
    private static readonly KeywordDefinition[] KeywordDefinitions =
    [
        new("dotnet", ".NET", "RequiredSkill", [".net", "asp.net", "dotnet"]),
        new("csharp", "C#", "RequiredSkill", ["c#", "csharp"]),
        new("react", "React", "RequiredSkill", ["react"]),
        new("typescript", "TypeScript", "RequiredSkill", ["typescript", "ts"]),
        new("javascript", "JavaScript", "RequiredSkill", ["javascript", "js"]),
        new("sql", "SQL", "RequiredSkill", ["sql"]),
        new("postgresql", "PostgreSQL", "RequiredSkill", ["postgresql", "postgres"]),
        new("entity-framework", "Entity Framework", "PreferredSkill", ["entity framework", "ef core"]),
        new("rest-api", "REST APIs", "Responsibility", ["rest", "api", "apis"]),
        new("azure", "Azure", "PreferredSkill", ["azure"]),
        new("aws", "AWS", "PreferredSkill", ["aws"]),
        new("docker", "Docker", "PreferredSkill", ["docker"]),
        new("kubernetes", "Kubernetes", "PreferredSkill", ["kubernetes", "k8s"]),
        new("git", "Git", "PreferredSkill", ["git"]),
        new("html-css", "HTML/CSS", "RequiredSkill", ["html", "css"])
    ];

    public Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisInput input, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var detectedLanguage = DetectLanguage(input.JobPostingText);
        var selectedLanguage = string.IsNullOrWhiteSpace(input.SelectedLanguage)
            ? detectedLanguage
            : input.SelectedLanguage.Trim();
        var companyName = ExtractLabeledValue(input.JobPostingText, "company") ?? input.CompanyName.Trim();
        var roleTitle = ExtractLabeledValue(input.JobPostingText, "role") ??
            ExtractLabeledValue(input.JobPostingText, "title") ??
            input.RoleTitle.Trim();
        var signals = KeywordDefinitions
            .Where(definition => ContainsAny(input.JobPostingText, definition.Keywords))
            .Select(definition => new JobSignal(
                definition.Id,
                definition.Label,
                definition.Category,
                definition.Keywords))
            .ToList();

        var document = new JobSignalsDocument(
            "Fake",
            DateTimeOffset.UtcNow,
            signals.Where(signal => signal.Category == "RequiredSkill").Select(signal => signal.Label).ToList(),
            signals.Where(signal => signal.Category == "PreferredSkill").Select(signal => signal.Label).ToList(),
            signals.Where(signal => signal.Category == "Responsibility").Select(signal => signal.Label).ToList(),
            signals);

        return Task.FromResult(new JobAnalysisResult(companyName, roleTitle, detectedLanguage, selectedLanguage, document));
    }

    public Task<EvidenceMatchResult> MatchEvidenceAsync(EvidenceMatchInput input, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var matches = new List<EvidenceMatch>();
        var unmatched = new List<UnmatchedRequirement>();

        foreach (var signal in input.Signals)
        {
            var signalMatches = input.ApprovedFacts
                .Select(fact => new
                {
                    Fact = fact,
                    Terms = signal.Keywords
                        .Where(keyword => FactContains(fact, keyword))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                })
                .Where(match => match.Terms.Count > 0)
                .ToList();

            if (signalMatches.Count == 0)
            {
                unmatched.Add(new UnmatchedRequirement(
                    $"unmatched-{signal.Id}",
                    signal.Id,
                    signal.Label,
                    signal.Category,
                    $"Mention {signal.Label} as an interest to learn only if the posting makes it relevant."));
                continue;
            }

            matches.AddRange(signalMatches.Select(match => new EvidenceMatch(
                $"match-{signal.Id}-{match.Fact.Id:N}",
                signal.Id,
                signal.Label,
                signal.Category,
                match.Fact.Id,
                match.Fact.Title,
                match.Fact.Summary,
                match.Terms)));
        }

        return Task.FromResult(new EvidenceMatchResult(matches, unmatched));
    }

    public Task<DraftGenerationResult> GenerateDraftAsync(DraftGenerationInput input, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var language = string.IsNullOrWhiteSpace(input.SelectedLanguage) ? "English" : input.SelectedLanguage.Trim();
        var applicant = string.IsNullOrWhiteSpace(input.ApplicantName) ? "I" : input.ApplicantName.Trim();
        var evidenceLines = input.ApprovedEvidence
            .Select(evidence => $"- {evidence.ProfileFactTitle}: {evidence.Summary}")
            .ToList();
        var gapLines = input.UnmatchedRequirements
            .Select(requirement => $"- I would treat {requirement.Requirement} as an area to learn, not as existing experience.")
            .ToList();
        var evidenceText = string.Join(Environment.NewLine, evidenceLines);
        var gapText = gapLines.Count == 0
            ? "- I will keep the application focused on the reviewed evidence."
            : string.Join(Environment.NewLine, gapLines);
        var toneText = string.IsNullOrWhiteSpace(input.TonePreference)
            ? "plain and evidence-led"
            : input.TonePreference.Trim();

        var coverLetter = $"""
            Language: {language}
            Dear {input.CompanyName} hiring team,

            I am applying for the {input.RoleTitle} role at {input.CompanyName}. My draft is written in a {toneText} tone and uses only reviewed evidence.

            Approved evidence:
            {evidenceText}

            Honest gap handling:
            {gapText}

            Kind regards,
            {applicant}
            """;

        var shortMotivation = $"""
            Language: {language}
            I am interested in the {input.RoleTitle} role at {input.CompanyName} because my reviewed evidence includes {string.Join(", ", input.ApprovedEvidence.Select(evidence => evidence.ProfileFactTitle))}. I will describe unmatched requirements honestly as learning areas.
            """;

        return Task.FromResult(new DraftGenerationResult(coverLetter, shortMotivation));
    }

    private static bool ContainsAny(string text, IReadOnlyList<string> keywords) =>
        keywords.Any(keyword => ContainsTerm(text, keyword));

    private static bool FactContains(ProfileFact fact, string keyword)
    {
        var haystack = string.Join(' ', [
            fact.Type,
            fact.Title,
            fact.Summary,
            ExtractJsonText(fact.FactItems),
            ExtractJsonText(fact.Technologies),
            ExtractJsonText(fact.AllowedClaims)
        ]);

        return ContainsTerm(haystack, keyword);
    }

    private static bool ContainsTerm(string text, string keyword)
    {
        if (keyword.Any(character => !char.IsLetterOrDigit(character)))
        {
            return text.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        return Regex.IsMatch(text, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase);
    }

    private static string DetectLanguage(string text)
    {
        var lower = text.ToLowerInvariant();
        var danishMarkers = new[] { " og ", " med ", " erfaring ", " udvikler", " ansøgning", " københavn", "æ", "ø", "å" };
        return danishMarkers.Any(marker => lower.Contains(marker, StringComparison.Ordinal)) ? "Danish" : "English";
    }

    private static string? ExtractLabeledValue(string text, string label)
    {
        var match = Regex.Match(
            text,
            $@"^\s*{Regex.Escape(label)}\s*:\s*(?<value>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static string ExtractJsonText(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var values = new List<string>();
            AddJsonText(document.RootElement, values);
            return string.Join(' ', values);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static void AddJsonText(JsonElement element, List<string> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                values.Add(element.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Array:
                foreach (var child in element.EnumerateArray())
                {
                    AddJsonText(child, values);
                }
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    AddJsonText(property.Value, values);
                }
                break;
        }
    }

    private sealed record KeywordDefinition(
        string Id,
        string Label,
        string Category,
        IReadOnlyList<string> Keywords);
}
