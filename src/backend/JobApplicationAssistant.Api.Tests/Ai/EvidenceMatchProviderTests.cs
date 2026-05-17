using System.Net;
using System.Net.Http.Json;
using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Domain;
using Xunit;

namespace JobApplicationAssistant.Api.Tests.Ai;

public sealed class EvidenceMatchProviderTests
{
    [Fact]
    public async Task Ollama_evidence_matching_accepts_quality_reason_and_unmatched_requirements()
    {
        var fact = ApprovedFact();
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidEvidenceMatchingJson(fact.Id, EvidenceQuality.Partial)) }
        ]));
        var provider = new OllamaAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") }, Options());

        var result = await provider.MatchEvidenceAsync(EvidenceInput(fact), CancellationToken.None);

        var match = Assert.Single(result.EvidenceMatches);
        Assert.Equal(EvidenceQuality.Partial, match.Quality);
        Assert.Equal("Reviewer should keep wording careful.", match.Reason);
        Assert.Single(result.UnmatchedRequirements);
        var requestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("Candidate fit brief context", requestJson);
        Assert.Contains("Fit brief context for API work", requestJson);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("GoodEnough")]
    public async Task Ollama_evidence_matching_rejects_missing_or_unknown_quality_values(string? quality)
    {
        var fact = ApprovedFact();
        var invalidJson = ValidEvidenceMatchingJson(fact.Id, quality);
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) }
        ]));
        var provider = new OllamaAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") }, Options());

        await Assert.ThrowsAsync<AiInvalidOutputException>(() =>
            provider.MatchEvidenceAsync(EvidenceInput(fact), CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count);
        var repairRequestJson = await handler.Requests[1].Content!.ReadAsStringAsync();
        Assert.Contains("Strong, Partial, or Weak", repairRequestJson);
    }

    [Fact]
    public async Task OpenAi_compatible_evidence_matching_accepts_quality_reason_and_unmatched_requirements()
    {
        var fact = ApprovedFact();
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidEvidenceMatchingJson(fact.Id, EvidenceQuality.Weak)) }
        ]));
        var provider = new OpenAiCompatibleAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/v1/") }, Options());

        var result = await provider.MatchEvidenceAsync(EvidenceInput(fact), CancellationToken.None);

        var match = Assert.Single(result.EvidenceMatches);
        Assert.Equal(EvidenceQuality.Weak, match.Quality);
        Assert.Equal("Reviewer should keep wording careful.", match.Reason);
        Assert.Single(result.UnmatchedRequirements);
    }

    [Fact]
    public async Task OpenAi_compatible_evidence_matching_rejects_unknown_quality_values()
    {
        var fact = ApprovedFact();
        var invalidJson = ValidEvidenceMatchingJson(fact.Id, "GoodEnough");
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(invalidJson) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(invalidJson) }
        ]));
        var provider = new OpenAiCompatibleAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/v1/") }, Options());

        await Assert.ThrowsAsync<AiInvalidOutputException>(() =>
            provider.MatchEvidenceAsync(EvidenceInput(fact), CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count);
    }

    private static EvidenceMatchInput EvidenceInput(ProfileFact fact) =>
        new(
            [
                new JobSignal("dotnet", ".NET", "RequiredSkill", [".net"]),
                new JobSignal("kubernetes", "Kubernetes", "PreferredSkill", ["kubernetes"])
            ],
            [fact],
            new CandidateFitBriefResult(
                "Fit brief context for API work.",
                [],
                [],
                [],
                [],
                []));

    private static ProfileFact ApprovedFact() =>
        new()
        {
            Id = Guid.NewGuid(),
            Type = "Project",
            Title = "Approved API work",
            Summary = "Built ASP.NET Core APIs.",
            Status = ProfileFactStatus.Approved,
            Technologies = """[".NET"]""",
            AllowedClaims = """["Built API services"]"""
        };

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

    private static string ValidEvidenceMatchingJson(Guid profileFactId, string? quality)
    {
        var qualityProperty = quality is null ? string.Empty : $"""
              "quality": "{quality}",
        """;

        return $$"""
        {
          "evidenceMatches": [
            {
              "signalId": "dotnet",
              "profileFactId": "{{profileFactId}}",
              "summary": "Approved API work demonstrates .NET experience.",
        {{qualityProperty}}
              "reason": "Reviewer should keep wording careful.",
              "matchedTerms": [".NET"]
            }
          ],
          "unmatchedRequirements": [
            {
              "signalId": "kubernetes",
              "recommendation": "Treat Kubernetes as an honest learning area."
            }
          ]
        }
        """;
    }

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
