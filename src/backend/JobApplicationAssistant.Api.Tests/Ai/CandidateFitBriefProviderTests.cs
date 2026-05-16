using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Domain;
using Xunit;

namespace JobApplicationAssistant.Api.Tests.Ai;

public sealed class CandidateFitBriefProviderTests
{
    [Fact]
    public async Task FakeAiProvider_returns_deterministic_candidate_fit_brief_with_traceable_fact_ids()
    {
        var provider = new FakeAiProvider();
        var fact = ApprovedFact("Approved API work", "Built ASP.NET Core APIs backed by PostgreSQL.", """[".NET","PostgreSQL"]""");
        var input = CandidateFitInput([fact]);

        var first = await provider.GenerateCandidateFitBriefAsync(input, CancellationToken.None);
        var second = await provider.GenerateCandidateFitBriefAsync(input, CancellationToken.None);

        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Contains("Northwind", first.CandidateSummary);
        Assert.Contains("direct", first.CandidateSummary);
        Assert.Contains(first.SkillGroups.SelectMany(group => group.Items), item => item.SupportingProfileFactIds.Contains(fact.Id));
        Assert.Contains(first.RelevantProjects, item => item.SupportingProfileFactIds.Contains(fact.Id));
    }

    [Fact]
    public async Task Ollama_candidate_fit_brief_accepts_valid_output()
    {
        var fact = ApprovedFact("Approved API work", "Built ASP.NET Core APIs backed by PostgreSQL.", """[".NET"]""");
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidCandidateFitBriefJson(fact.Id)) }
        ]));
        var provider = new OllamaAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") }, Options());

        var result = await provider.GenerateCandidateFitBriefAsync(CandidateFitInput([fact]), CancellationToken.None);

        Assert.Equal(1, result.AttemptCount);
        Assert.Equal("Candidate has traceable API delivery evidence.", result.CandidateSummary);
        Assert.Contains(result.Competencies, item => item.SupportingProfileFactIds.Contains(fact.Id));
        var requestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("Northwind", requestJson);
        Assert.Contains("direct", requestJson);
        Assert.Contains(fact.Id.ToString(), requestJson);
        Assert.Contains("REST APIs", requestJson);
    }

    [Fact]
    public async Task OpenAi_compatible_candidate_fit_brief_accepts_valid_output()
    {
        var fact = ApprovedFact("Approved API work", "Built ASP.NET Core APIs backed by PostgreSQL.", """[".NET"]""");
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidCandidateFitBriefJson(fact.Id)) }
        ]));
        var provider = new OpenAiCompatibleAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/v1/") }, Options());

        var result = await provider.GenerateCandidateFitBriefAsync(CandidateFitInput([fact]), CancellationToken.None);

        Assert.Equal(1, result.AttemptCount);
        Assert.Contains(result.RelevantProjects, item => item.SupportingProfileFactIds.Contains(fact.Id));
        var requestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("chat/completions", handler.Requests[0].RequestUri!.ToString());
        Assert.Contains("candidate fit brief", requestJson);
        Assert.Contains(fact.Id.ToString(), requestJson);
    }

    [Theory]
    [InlineData("{ malformed")]
    [InlineData("""{"candidateSummary":"summary","skillGroups":[]}""")]
    public async Task Ollama_candidate_fit_brief_rejects_malformed_or_missing_required_arrays(string invalidJson)
    {
        var fact = ApprovedFact("Approved API work", "Built ASP.NET Core APIs backed by PostgreSQL.", """[".NET"]""");
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) }
        ]));
        var provider = new OllamaAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") }, Options());

        await Assert.ThrowsAsync<AiInvalidOutputException>(() =>
            provider.GenerateCandidateFitBriefAsync(CandidateFitInput([fact]), CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count);
        var repairRequestJson = await handler.Requests[1].Content!.ReadAsStringAsync();
        Assert.Contains("Repair this candidate fit brief JSON", repairRequestJson);
    }

    [Fact]
    public async Task Ollama_candidate_fit_brief_rejects_invalid_profile_fact_ids()
    {
        var fact = ApprovedFact("Approved API work", "Built ASP.NET Core APIs backed by PostgreSQL.", """[".NET"]""");
        var invalidJson = ValidCandidateFitBriefJson(Guid.NewGuid());
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) }
        ]));
        var provider = new OllamaAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") }, Options());

        await Assert.ThrowsAsync<AiInvalidOutputException>(() =>
            provider.GenerateCandidateFitBriefAsync(CandidateFitInput([fact]), CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Ollama_candidate_fit_brief_rejects_concrete_items_without_supporting_profile_fact_ids()
    {
        var fact = ApprovedFact("Approved API work", "Built ASP.NET Core APIs backed by PostgreSQL.", """[".NET"]""");
        var invalidJson = ValidCandidateFitBriefJson(fact.Id).Replace(
            $"\"supportingProfileFactIds\": [\"{fact.Id}\"]",
            "\"supportingProfileFactIds\": []",
            StringComparison.Ordinal);
        var handler = new QueuedHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(invalidJson) }
        ]));
        var provider = new OllamaAiProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") }, Options());

        await Assert.ThrowsAsync<AiInvalidOutputException>(() =>
            provider.GenerateCandidateFitBriefAsync(CandidateFitInput([fact]), CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count);
    }

    private static CandidateFitBriefInput CandidateFitInput(IReadOnlyList<ProfileFact> facts) =>
        new(
            "Northwind",
            "Platform Developer",
            "https://example.test/jobs/platform-developer",
            new DateOnly(2026, 6, 1),
            "English",
            "direct",
            "We need .NET, PostgreSQL, and REST APIs.",
            new JobSignalsDocument(
                "test",
                DateTimeOffset.Parse("2026-05-17T00:00:00+00:00"),
                [".NET"],
                ["PostgreSQL"],
                ["REST APIs"],
                [new JobSignal("rest-api", "REST APIs", "Responsibility", ["rest", "api"])]),
            facts);

    private static ProfileFact ApprovedFact(string title, string summary, string technologies) =>
        new()
        {
            Id = Guid.NewGuid(),
            Type = "Project",
            Title = title,
            Summary = summary,
            Status = ProfileFactStatus.Approved,
            FactItems = """["Delivered production APIs"]""",
            Technologies = technologies,
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

    private static string ValidCandidateFitBriefJson(Guid profileFactId) =>
        $$"""
        {
          "candidateSummary": "Candidate has traceable API delivery evidence.",
          "skillGroups": [
            {
              "name": "Backend delivery",
              "items": [
                {
                  "title": ".NET APIs",
                  "summary": "Supported by approved API work.",
                  "supportingProfileFactIds": ["{{profileFactId}}"]
                }
              ]
            }
          ],
          "competencies": [
            {
              "title": "API implementation",
              "summary": "Can connect job API requirements to approved project evidence.",
              "supportingProfileFactIds": ["{{profileFactId}}"]
            }
          ],
          "relevantProjects": [
            {
              "title": "Approved API work",
              "summary": "Relevant to platform delivery.",
              "supportingProfileFactIds": ["{{profileFactId}}"]
            }
          ],
          "transferableStrengths": [
            {
              "title": "Evidence-led delivery",
              "summary": "Uses reviewed facts for claims.",
              "supportingProfileFactIds": ["{{profileFactId}}"]
            }
          ],
          "riskNotes": [
            {
              "title": "Kubernetes evidence",
              "summary": "No approved fact supports Kubernetes depth.",
              "supportingProfileFactIds": []
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
