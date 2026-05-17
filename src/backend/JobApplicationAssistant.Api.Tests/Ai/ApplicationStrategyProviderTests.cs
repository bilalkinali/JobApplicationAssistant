using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JobApplicationAssistant.Api.Ai;
using Xunit;

namespace JobApplicationAssistant.Api.Tests.Ai;

public sealed class ApplicationStrategyProviderTests
{
    [Fact]
    public async Task FakeAiProvider_returns_deterministic_application_strategy_with_approved_evidence()
    {
        var provider = new FakeAiProvider();
        var input = StrategyInput(Guid.NewGuid());

        var first = await provider.GenerateApplicationStrategyAsync(input, CancellationToken.None);
        var second = await provider.GenerateApplicationStrategyAsync(input, CancellationToken.None);

        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Contains(first.PrimaryAngles, angle => angle.EvidenceIds.Contains("match-dotnet"));
        Assert.Contains(first.GapHandlingGuidance, guidance => guidance.UnmatchedRequirementId == "unmatched-kubernetes");
        Assert.Contains("direct", first.ToneGuidance);
        Assert.All(first.DraftOutline, item => Assert.NotNull(item.EvidenceIds));
    }

    [Fact]
    public async Task Ollama_application_strategy_accepts_valid_output()
    {
        var profileFactId = Guid.NewGuid();
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidApplicationStrategyJson(profileFactId, "match-dotnet")) }
        ]));
        var provider = new OllamaAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") }, Options());

        var result = await provider.GenerateApplicationStrategyAsync(StrategyInput(profileFactId), CancellationToken.None);

        Assert.Equal(1, result.AttemptCount);
        Assert.Contains(result.PrimaryAngles, angle => angle.EvidenceIds.Contains("match-dotnet"));
        Assert.Contains(result.PrimaryAngles, angle => angle.ProfileFactIds.Contains(profileFactId));
        var requestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("application strategy", requestJson);
        Assert.Contains("quality", requestJson);
        Assert.Contains("Candidate fit brief", requestJson);
        Assert.Contains("unmatched-kubernetes", requestJson);
    }

    [Fact]
    public async Task OpenAi_compatible_application_strategy_accepts_valid_output()
    {
        var profileFactId = Guid.NewGuid();
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidApplicationStrategyJson(profileFactId, "match-dotnet")) }
        ]));
        var provider = new OpenAiCompatibleAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/v1/") }, Options());

        var result = await provider.GenerateApplicationStrategyAsync(StrategyInput(profileFactId), CancellationToken.None);

        Assert.Equal(1, result.AttemptCount);
        Assert.Contains(result.DraftOutline, item => item.EvidenceIds.Contains("match-dotnet"));
        Assert.Contains("chat/completions", handler.Requests[0].RequestUri!.ToString());
    }

    [Theory]
    [InlineData("""{"primaryAngles":[]}""")]
    [InlineData("{ malformed")]
    public async Task Ollama_application_strategy_rejects_malformed_or_missing_required_arrays(string invalidJson)
    {
        var profileFactId = Guid.NewGuid();
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) }
        ]));
        var provider = new OllamaAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") }, Options());

        await Assert.ThrowsAsync<AiInvalidOutputException>(() =>
            provider.GenerateApplicationStrategyAsync(StrategyInput(profileFactId), CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count);
        var repairRequestJson = await handler.Requests[1].Content!.ReadAsStringAsync();
        Assert.Contains("Repair this application strategy JSON", repairRequestJson);
    }

    [Fact]
    public async Task Ollama_application_strategy_rejects_invalid_evidence_ids()
    {
        var profileFactId = Guid.NewGuid();
        var invalidJson = ValidApplicationStrategyJson(profileFactId, "unknown-match");
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) }
        ]));
        var provider = new OllamaAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") }, Options());

        await Assert.ThrowsAsync<AiInvalidOutputException>(() =>
            provider.GenerateApplicationStrategyAsync(StrategyInput(profileFactId), CancellationToken.None));
    }

    [Fact]
    public async Task Ollama_application_strategy_rejects_invalid_profile_fact_ids()
    {
        var profileFactId = Guid.NewGuid();
        var invalidJson = ValidApplicationStrategyJson(Guid.NewGuid(), "match-dotnet");
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) }
        ]));
        var provider = new OllamaAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") }, Options());

        await Assert.ThrowsAsync<AiInvalidOutputException>(() =>
            provider.GenerateApplicationStrategyAsync(StrategyInput(profileFactId), CancellationToken.None));
    }

    private static ApplicationStrategyInput StrategyInput(Guid profileFactId) =>
        new(
            new JobAnalysisResult(
                "Northwind",
                "Platform Developer",
                "English",
                "English",
                new JobSignalsDocument(
                    "test",
                    DateTimeOffset.Parse("2026-05-17T00:00:00+00:00"),
                    [".NET"],
                    ["Kubernetes"],
                    ["REST APIs"],
                    [new JobSignal("dotnet", ".NET", "RequiredSkill", [".net"])])),
            new CandidateFitBriefResult(
                "Candidate fit brief for API delivery.",
                [],
                [new CandidateFitBriefItem("API delivery", "Trace-only context.", [profileFactId])],
                [],
                [],
                []),
            [
                new EvidenceMatch(
                    "match-dotnet",
                    "dotnet",
                    ".NET",
                    "RequiredSkill",
                    profileFactId,
                    "Approved API work",
                    "Built ASP.NET Core APIs.",
                    [".net"],
                    EvidenceQuality.Strong,
                    ".NET is directly supported.")
            ],
            [
                new UnmatchedRequirement(
                    "unmatched-kubernetes",
                    "kubernetes",
                    "Kubernetes",
                    "PreferredSkill",
                    "Mention only as a learning interest.")
            ],
            [new DraftGapDecision("unmatched-kubernetes", "MentionAsLearningInterest", null)],
            [],
            "English",
            "direct");

    private static AiOptions Options() =>
        new()
        {
            Provider = "Ollama",
            Endpoint = "http://localhost:11434",
            Model = "local-model"
        };

    private static JsonContent OllamaGenerateContent(string response) =>
        JsonContent.Create(new { response });

    private static JsonContent OpenAiChatCompletionContent(string content) =>
        JsonContent.Create(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content
                    }
                }
            }
        });

    private static string ValidApplicationStrategyJson(Guid profileFactId, string evidenceId) =>
        $$"""
        {
          "primaryAngles": [
            {
              "title": "API delivery for platform work",
              "rationale": "Lead with the strongest approved API evidence.",
              "evidenceIds": ["{{evidenceId}}"],
              "profileFactIds": ["{{profileFactId}}"]
            }
          ],
          "secondaryAngles": [
            {
              "title": "Evidence-led collaboration",
              "rationale": "Use only as supporting context.",
              "evidenceIds": [],
              "profileFactIds": ["{{profileFactId}}"]
            }
          ],
          "gapHandlingGuidance": [
            {
              "unmatchedRequirementId": "unmatched-kubernetes",
              "guidance": "Mention Kubernetes only as a learning interest."
            }
          ],
          "claimsToAvoid": [
            {
              "claim": "Hands-on Kubernetes production ownership",
              "reason": "No approved evidence supports Kubernetes."
            }
          ],
          "toneGuidance": "Use direct, evidence-led language.",
          "draftOutline": [
            {
              "section": "Opening",
              "guidance": "Introduce the role and strongest evidence.",
              "evidenceIds": ["{{evidenceId}}"],
              "profileFactIds": ["{{profileFactId}}"]
            }
          ]
        }
        """;

    private sealed class QueuedHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responses.Dequeue());
        }
    }
}
