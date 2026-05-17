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

    private readonly string model;

    public FakeAiProvider()
        : this(new FakeAiProviderOptions())
    {
    }

    public FakeAiProvider(FakeAiProviderOptions options)
    {
        model = string.IsNullOrWhiteSpace(options.Model) ? "fake-deterministic" : options.Model.Trim();
    }

    public Task<AiProviderStatus> GetStatusAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(new AiProviderStatus(
            "Fake",
            model,
            null,
            true,
            "Fake provider is available."));
    }

    public Task<AiDiagnosticsResult> RunDiagnosticsAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(new AiDiagnosticsResult(
            "Fake",
            model,
            null,
            true,
            "Fake provider diagnostics passed.",
            [new AiDiagnosticCheck("provider", "ok", "Fake provider is ready.")]));
    }

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
                        .Select(keyword => new
                        {
                            Keyword = keyword,
                            Quality = ClassifyFactMatch(fact, keyword)
                        })
                        .Where(match => match.Quality is not null)
                        .DistinctBy(match => match.Keyword, StringComparer.OrdinalIgnoreCase)
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

            matches.AddRange(signalMatches.Select(match =>
            {
                var quality = BestQuality(match.Terms.Select(term => term.Quality!).ToList());
                return new EvidenceMatch(
                    $"match-{signal.Id}-{match.Fact.Id:N}",
                    signal.Id,
                    signal.Label,
                    signal.Category,
                    match.Fact.Id,
                    match.Fact.Title,
                    match.Fact.Summary,
                    match.Terms.Select(term => term.Keyword).ToList(),
                    quality,
                    QualityReason(quality, signal.Label, match.Fact.Title));
            }));
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
        var requirementsById = input.UnmatchedRequirements.ToDictionary(
            requirement => requirement.Id,
            StringComparer.OrdinalIgnoreCase);
        var approvedCustomFactIds = input.ApprovedCustomFacts
            .Select(fact => fact.Id)
            .ToHashSet();
        var gapLines = input.GapDecisions
            .Select(decision => GapDecisionLine(decision, requirementsById, approvedCustomFactIds))
            .Where(line => line is not null)
            .Select(line => line!)
            .ToList();
        if (input.GapDecisions.Count == 0)
        {
            gapLines = input.UnmatchedRequirements
                .Select(requirement => $"- I would treat {requirement.Requirement} as an area to learn, not as existing experience.")
                .ToList();
        }
        var evidenceText = string.Join(Environment.NewLine, evidenceLines);
        var gapText = gapLines.Count == 0
            ? "- I will not address ignored or covered gaps unless reviewed evidence supports them."
            : string.Join(Environment.NewLine, gapLines);
        var toneText = string.IsNullOrWhiteSpace(input.TonePreference)
            ? "plain and evidence-led"
            : input.TonePreference.Trim();
        var motivationGapText = gapLines.Count == 0
            ? "I will keep unsupported gaps out of concrete claims."
            : "I will describe selected unmatched requirements honestly as learning areas.";

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
            I am interested in the {input.RoleTitle} role at {input.CompanyName} because my reviewed evidence includes {string.Join(", ", input.ApprovedEvidence.Select(evidence => evidence.ProfileFactTitle))}. {motivationGapText}
            """;

        return Task.FromResult(new DraftGenerationResult(coverLetter, shortMotivation));
    }

    private static string? GapDecisionLine(
        DraftGapDecision decision,
        IReadOnlyDictionary<string, UnmatchedRequirement> requirementsById,
        ISet<Guid> approvedCustomFactIds)
    {
        if (!requirementsById.TryGetValue(decision.UnmatchedRequirementId, out var requirement))
        {
            return null;
        }

        return decision.Decision switch
        {
            "MentionAsLearningInterest" => $"- I would treat {requirement.Requirement} as an area to learn, not as existing experience.",
            "CoveredByCustomFact" when decision.CustomFactId is not null && approvedCustomFactIds.Contains(decision.CustomFactId.Value) => null,
            _ => null
        };
    }

    public Task<ClaimAuditResult> AuditClaimsAsync(ClaimAuditInput input, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var evidence = input.ApprovedEvidence.ToList();
        var claims = SplitClaims(input.CoverLetterText, input.ShortMotivationText)
            .Select((claim, index) =>
            {
                var evidenceIds = evidence
                    .Where(match => ClaimSupportedByEvidence(claim, match))
                    .Select(match => match.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var status = evidenceIds.Count > 0
                    ? "Supported"
                    : ClaimNeedsReview(claim) ? "NeedsReview" : "Unsupported";

                return new ClaimAuditClaim($"claim-{index + 1}", claim, status, evidenceIds);
            })
            .ToList();

        return Task.FromResult(new ClaimAuditResult(claims));
    }

    public Task<CandidateFitBriefResult> GenerateCandidateFitBriefAsync(CandidateFitBriefInput input, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var facts = input.ApprovedProfileFacts
            .OrderBy(fact => fact.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(fact => fact.Id)
            .ToList();
        var primaryFact = facts.FirstOrDefault();
        IReadOnlyList<Guid> primaryIds = primaryFact is null ? [] : [primaryFact.Id];
        var technologyItems = facts
            .SelectMany(fact => ExtractJsonValues(fact.Technologies)
                .Select(technology => new CandidateFitBriefItem(
                    technology,
                    $"{technology} is supported by {fact.Title}.",
                    [fact.Id])))
            .DistinctBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (technologyItems.Count == 0 && primaryFact is not null)
        {
            technologyItems.Add(new CandidateFitBriefItem(
                primaryFact.Title,
                primaryFact.Summary,
                [primaryFact.Id]));
        }

        var projectItems = facts
            .Take(3)
            .Select(fact => new CandidateFitBriefItem(fact.Title, fact.Summary, [fact.Id]))
            .ToList();
        var language = string.IsNullOrWhiteSpace(input.SelectedLanguage) ? "English" : input.SelectedLanguage.Trim();
        var tone = string.IsNullOrWhiteSpace(input.TonePreference) ? "evidence-led" : input.TonePreference.Trim();
        var signalSummary = input.JobSignals?.Signals.Count > 0
            ? string.Join(", ", input.JobSignals.Signals.Select(signal => signal.Label).Take(5))
            : string.IsNullOrWhiteSpace(input.JobPostingText) ? "the supplied role context" : "the supplied job posting";

        IReadOnlyList<CandidateFitBriefItem> competencies = primaryFact is null
            ? []
            : new[]
            {
                new CandidateFitBriefItem(
                    "Evidence-led delivery",
                    $"The strongest concrete evidence is {primaryFact.Title}.",
                    primaryIds)
            };
        IReadOnlyList<CandidateFitBriefItem> transferableStrengths = facts.Count == 0
            ? []
            : new[]
            {
                new CandidateFitBriefItem(
                    "Traceable profile evidence",
                    "The brief only uses approved profile facts as concrete support.",
                    facts.Select(fact => fact.Id).ToList())
            };

        var result = new CandidateFitBriefResult(
            $"Language: {language}. {input.RoleTitle} at {input.CompanyName} fit brief in a {tone} tone, based on {facts.Count} approved profile facts and {signalSummary}.",
            [
                new CandidateFitSkillGroup("Supported skills", technologyItems)
            ],
            competencies,
            projectItems,
            transferableStrengths,
            [
                new CandidateFitBriefItem(
                    "Unsupported requirements need review",
                    "Do not turn job requirements into candidate claims unless an approved fact supports them.",
                    [])
            ]);

        return Task.FromResult(result);
    }

    public Task<AssistedProfileImportResult> ImportProfileFactsAsync(AssistedProfileImportInput input, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var detectedTechnologies = KeywordDefinitions
            .Where(definition => ContainsAny(input.ExtractedText, definition.Keywords))
            .Select(definition => definition.Label)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var technologies = detectedTechnologies.ToList();
        if (technologies.Count == 0)
        {
            technologies.Add("Imported CV review");
        }

        var sourceContext = FirstUsefulSentence(input.ExtractedText);
        var hasExpandedCvCoverage =
            ContainsAny(input.ExtractedText, ["collaboration", "communication", "stakeholder", "requirements", "analysis"]) ||
            ContainsAny(input.ExtractedText, ["transferable", "business", "domain", "customer", "operations", "process"]) ||
            ContainsAny(input.ExtractedText, ["education", "degree", "university", "bachelor", "master", "certification"]) ||
            ContainsAny(input.ExtractedText, ["english", "danish", "language", "fluent"]);

        if (!hasExpandedCvCoverage)
        {
            var fact = new AssistedProfileImportFact(
                "ImportedCv",
                $"Demo CV import: {technologies.First()}",
                $"Fake assisted import from {input.FileName}: review this draft before using it as evidence.",
                [sourceContext],
                technologies,
                technologies.Select(technology => $"Reviewed CV evidence may mention {technology}.").ToList(),
                ["Do not claim this imported CV evidence until the draft fact is approved."],
                sourceContext);

            return Task.FromResult(new AssistedProfileImportResult([fact]));
        }

        var facts = new List<AssistedProfileImportFact>();

        if (detectedTechnologies.Count > 0)
        {
            facts.Add(new AssistedProfileImportFact(
                "Skill",
                $"Imported technical skills: {technologies.First()}",
                $"Fake assisted import from {input.FileName}: review these draft technical skills before using them as evidence.",
                [sourceContext],
                technologies,
                technologies.Select(technology => $"Reviewed CV evidence may mention {technology}.").ToList(),
                ["Do not claim these imported CV skills until the draft fact is approved."],
                sourceContext));
        }

        if (detectedTechnologies.Count > 0)
        {
            facts.Add(new AssistedProfileImportFact(
                "Tool",
                $"Imported tools: {technologies.First()}",
                "The CV names tools or technologies that should be reviewed separately from broader competencies.",
                [sourceContext],
                technologies,
                technologies.Select(technology => $"Reviewed CV evidence may mention hands-on use of {technology} after approval.").ToList(),
                ["Do not claim depth, recency, or production ownership unless explicitly present in the CV."],
                sourceContext));
        }

        if (ContainsAny(input.ExtractedText, ["project", "built", "delivered", "implemented", "developed"]))
        {
            facts.Add(new AssistedProfileImportFact(
                "Project",
                "Imported project delivery",
                "The CV describes project work that should be reviewed as a separate draft fact.",
                [sourceContext],
                technologies,
                ["Reviewed CV evidence may mention project delivery after approval."],
                ["Do not claim project ownership, production impact, or seniority unless explicitly approved."],
                sourceContext));
        }

        if (ContainsAny(input.ExtractedText, ["work history", "role", "employer", "experience"]))
        {
            facts.Add(new AssistedProfileImportFact(
                "WorkHistory",
                "Imported work history",
                "The CV includes role, employer, or work experience background for review.",
                [sourceContext],
                [],
                ["Reviewed CV evidence may mention work history after approval."],
                ["Do not claim role scope or dates not explicitly present in the CV."],
                sourceContext));
        }

        if (ContainsAny(input.ExtractedText, ["collaboration", "communication", "stakeholder", "requirements", "analysis"]))
        {
            facts.Add(new AssistedProfileImportFact(
                "Competency",
                "Imported collaboration and analysis competency",
                "The CV describes collaboration, communication, requirements, or analysis competency for review.",
                [sourceContext],
                [],
                ["Reviewed CV evidence may mention collaboration or analysis competency after approval."],
                ["Do not claim leadership or ownership beyond the imported CV evidence."],
                sourceContext));
        }

        if (ContainsAny(input.ExtractedText, ["business", "domain", "customer", "operations", "process"]))
        {
            facts.Add(new AssistedProfileImportFact(
                "BusinessExperience",
                "Imported business experience",
                "The CV describes business, domain, customer, operations, or process experience for review.",
                [sourceContext],
                [],
                ["Reviewed CV evidence may mention business context after approval."],
                ["Do not claim direct industry expertise unless explicitly present in the CV."],
                sourceContext));
        }

        if (ContainsAny(input.ExtractedText, ["transferable", "business", "domain", "customer", "operations", "process"]))
        {
            facts.Add(new AssistedProfileImportFact(
                "TransferableStrength",
                "Imported transferable business strength",
                "The CV describes business, domain, customer, operations, or process experience for review.",
                [sourceContext],
                [],
                ["Reviewed CV evidence may mention transferable business strengths after approval."],
                ["Do not claim direct industry expertise unless explicitly present in the CV."],
                sourceContext));
        }

        if (ContainsAny(input.ExtractedText, ["education", "degree", "university", "bachelor", "master", "certification"]))
        {
            facts.Add(new AssistedProfileImportFact(
                "Education",
                "Imported education background",
                "The CV includes education or certification background for review.",
                [sourceContext],
                [],
                ["Reviewed CV evidence may mention education or certification after approval."],
                ["Do not claim credentials not explicitly present in the CV."],
                sourceContext));
        }

        if (ContainsAny(input.ExtractedText, ["english", "danish", "language", "fluent"]))
        {
            facts.Add(new AssistedProfileImportFact(
                "Language",
                "Imported language background",
                "The CV includes language background for review.",
                [sourceContext],
                [],
                ["Reviewed CV evidence may mention language background after approval."],
                ["Do not claim fluency unless explicitly present in the CV."],
                sourceContext));
        }

        facts.Add(new AssistedProfileImportFact(
            "AllowedClaim",
            "Imported claims to review",
            "The imported CV evidence contains possible claims that require user approval before use.",
            [sourceContext],
            detectedTechnologies,
            detectedTechnologies.Count > 0
                ? detectedTechnologies.Select(technology => $"Reviewed CV evidence may mention {technology} after approval.").ToList()
                : ["Reviewed CV evidence may be used only after approval."],
            ["Do not use these imported claims until the draft fact is approved."],
            sourceContext));

        facts.Add(new AssistedProfileImportFact(
            "ForbiddenClaim",
            "Imported risky claims to avoid",
            "The imported CV evidence remains unapproved and should not support claims until reviewed.",
            [sourceContext],
            [],
            [],
            ["Do not claim this imported CV evidence until the draft fact is approved."],
            sourceContext));

        return Task.FromResult(new AssistedProfileImportResult(facts));
    }

    private static bool ContainsAny(string text, IReadOnlyList<string> keywords) =>
        keywords.Any(keyword => ContainsTerm(text, keyword));

    private static string? ClassifyFactMatch(ProfileFact fact, string keyword)
    {
        if (ContainsTerm(fact.AllowedClaims, keyword) ||
            ContainsTerm(fact.Summary, keyword))
        {
            return EvidenceQuality.Strong;
        }

        if (ContainsTerm(fact.FactItems, keyword) ||
            ContainsTerm(fact.Technologies, keyword))
        {
            return EvidenceQuality.Partial;
        }

        if (ContainsTerm(fact.Title, keyword) ||
            ContainsTerm(fact.Type, keyword))
        {
            return EvidenceQuality.Weak;
        }

        return null;
    }

    private static string BestQuality(IReadOnlyList<string> qualities)
    {
        if (qualities.Contains(EvidenceQuality.Strong, StringComparer.Ordinal))
        {
            return EvidenceQuality.Strong;
        }

        return qualities.Contains(EvidenceQuality.Partial, StringComparer.Ordinal)
            ? EvidenceQuality.Partial
            : EvidenceQuality.Weak;
    }

    private static string QualityReason(string quality, string signal, string profileFactTitle) =>
        quality switch
        {
            EvidenceQuality.Strong => $"{signal} is directly supported by approved claims or summary in {profileFactTitle}.",
            EvidenceQuality.Partial => $"{signal} appears in structured approved fact details for {profileFactTitle}, so wording should stay careful.",
            _ => $"{signal} only appears in broad approved fact metadata for {profileFactTitle}; review before using it as proof."
        };

    private static IReadOnlyList<string> SplitClaims(params string[] texts) =>
        texts
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .SelectMany(text => text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(claim => claim.Trim())
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Select(claim => claim.EndsWith(".", StringComparison.Ordinal) ? claim : $"{claim}.")
            .ToList();

    private static bool ClaimSupportedByEvidence(string claim, EvidenceMatch evidence)
    {
        var evidenceText = string.Join(' ', [
            evidence.Signal,
            evidence.ProfileFactTitle,
            evidence.Summary,
            string.Join(' ', evidence.MatchedTerms)
        ]);

        return ContainsMeaningfulWords(claim, evidenceText) || ContainsMeaningfulWords(evidence.Summary, claim);
    }

    private static bool ContainsMeaningfulWords(string expectedWords, string text)
    {
        var words = Regex.Matches(expectedWords, @"\b[\w\-.#]+\b")
            .Select(match => match.Value)
            .Where(word => word.Length >= 4)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return words.Count > 0 && words.All(word => ContainsTerm(text, word));
    }

    private static bool ClaimNeedsReview(string claim) =>
        ContainsTerm(claim, "may") ||
        ContainsTerm(claim, "might") ||
        ContainsTerm(claim, "interested") ||
        ContainsTerm(claim, "learn") ||
        ContainsTerm(claim, "fit");

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

    private static IReadOnlyList<string> ExtractJsonValues(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var values = new List<string>();
            AddJsonText(document.RootElement, values);
            return values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList();
        }
        catch (JsonException)
        {
            return string.IsNullOrWhiteSpace(json) ? [] : [json.Trim()];
        }
    }

    private static string FirstUsefulSentence(string text)
    {
        var normalized = Regex.Replace(text, @"\s+", " ").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Imported PDF CV text was empty after extraction.";
        }

        var sentence = Regex.Split(normalized, @"(?<=[.!?])\s+")
            .FirstOrDefault(value => value.Length >= 12) ?? normalized;
        return sentence.Length <= 240 ? sentence : $"{sentence[..237]}...";
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
