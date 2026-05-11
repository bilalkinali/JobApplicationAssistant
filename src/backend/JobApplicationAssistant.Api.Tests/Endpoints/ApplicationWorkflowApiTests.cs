using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Domain;
using JobApplicationAssistant.Api.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace JobApplicationAssistant.Api.Tests.Endpoints;

public sealed class ApplicationWorkflowApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PostApplication_rejects_missing_required_fields_invalid_url_and_unknown_status()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var request = new ApplicationRequest(
            "",
            " ",
            "not-a-url",
            null,
            "Unknown",
            "",
            null,
            null);

        var response = await client.PostAsJsonAsync("/api/applications", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationRequest.CompanyName), error.Details.Keys);
        Assert.Contains(nameof(ApplicationRequest.RoleTitle), error.Details.Keys);
        Assert.Contains(nameof(ApplicationRequest.ApplicationUrl), error.Details.Keys);
        Assert.Contains(nameof(ApplicationRequest.Status), error.Details.Keys);
    }

    [Fact]
    public async Task AnalyzeJob_rejects_application_without_job_posting_text()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, jobPostingText: "");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationRequest.JobPostingText), error.Details.Keys);
    }

    [Fact]
    public async Task AnalyzeJob_updates_application_with_extracted_metadata_and_signals()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(
            client,
            """
            Company: Northwind
            Role: Full-stack Developer

            We need C#, .NET, React, TypeScript, PostgreSQL, REST APIs, Docker, and Kubernetes.
            """);

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var analyzed = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(analyzed);
        Assert.Equal("Northwind", analyzed.CompanyName);
        Assert.Equal("Full-stack Developer", analyzed.RoleTitle);
        Assert.Equal("English", analyzed.DetectedLanguage);
        Assert.Equal("English", analyzed.SelectedLanguage);
        Assert.Equal("PostingCaptured", analyzed.Status);

        var signals = JsonSerializer.Deserialize<JobSignalsDocument>(analyzed.JobSignals, JsonOptions);
        Assert.NotNull(signals);
        Assert.Contains(".NET", signals.RequiredSkills);
        Assert.Contains("React", signals.RequiredSkills);
        Assert.Contains("Kubernetes", signals.PreferredSkills);
        Assert.Contains("REST APIs", signals.Responsibilities);
    }

    [Fact]
    public async Task AnalyzeJob_with_ollama_updates_application_from_strict_json_and_records_successful_ai_run()
    {
        var ollamaResponse = OllamaGenerateContent(ValidOllamaAnalysisJson("Contoso", "Platform Engineer"));
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = ollamaResponse }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET, PostgreSQL, Kubernetes, and REST APIs.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var analyzed = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(analyzed);
        Assert.Equal("Contoso", analyzed.CompanyName);
        Assert.Equal("Platform Engineer", analyzed.RoleTitle);
        Assert.Equal("English", analyzed.DetectedLanguage);
        Assert.Equal("English", analyzed.SelectedLanguage);
        Assert.Equal("PostingCaptured", analyzed.Status);

        var signals = JsonSerializer.Deserialize<JobSignalsDocument>(analyzed.JobSignals, JsonOptions);
        Assert.NotNull(signals);
        Assert.Equal("Ollama", signals.Provider);
        Assert.Contains(".NET", signals.RequiredSkills);
        Assert.Contains("PostgreSQL", signals.RequiredSkills);
        Assert.Contains("Kubernetes", signals.PreferredSkills);
        Assert.Contains("Build REST APIs", signals.Responsibilities);
        var ollamaRequestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("\"format\":\"json\"", ollamaRequestJson);
        Assert.Contains("\"stream\":false", ollamaRequestJson);
        Assert.Contains("Return only strict JSON", ollamaRequestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal(application.Id, run.JobApplicationId);
        Assert.Equal("JobAnalysis", run.Step);
        Assert.Equal("Ollama", run.Provider);
        Assert.Equal("llama3.1:8b", run.Model);
        Assert.Equal("Succeeded", run.Status);
        Assert.Equal(1, run.AttemptCount);
        Assert.NotNull(run.CompletedAt);
        Assert.Null(run.ErrorCode);
        Assert.Contains("Platform Engineer", run.OutputSummary);
    }

    [Fact]
    public async Task AnalyzeJob_with_ollama_repairs_malformed_json_once_and_records_repaired_success()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaAnalysisJson("Northwind", "API Engineer")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET and REST APIs.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var analyzed = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(analyzed);
        Assert.Equal("Northwind", analyzed.CompanyName);
        Assert.Equal("API Engineer", analyzed.RoleTitle);
        Assert.Equal(2, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task AnalyzeJob_with_ollama_repairs_structurally_invalid_json_once()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("""{"companyName":"","roleTitle":"Missing Signals"}""") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaAnalysisJson("Tailspin", "Backend Engineer")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need PostgreSQL and Kubernetes.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var analyzed = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(analyzed);
        Assert.Equal("Tailspin", analyzed.CompanyName);
        Assert.Equal("Backend Engineer", analyzed.RoleTitle);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task AnalyzeJob_with_ollama_repairs_invalid_signal_structure_once()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = OllamaGenerateContent("""
                    {
                      "companyName":"Broken Signals",
                      "roleTitle":"Backend Engineer",
                      "detectedLanguage":"English",
                      "selectedLanguage":"English",
                      "jobSignals":{
                        "requiredSkills":[".NET"],
                        "preferredSkills":[],
                        "responsibilities":[],
                        "signals":[{"id":"dotnet","label":".NET","category":"Skill"}]
                      }
                    }
                    """)
            },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaAnalysisJson("Fabrikam", "Backend Engineer")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var analyzed = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(analyzed);
        Assert.Equal("Fabrikam", analyzed.CompanyName);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task AnalyzeJob_with_ollama_invalid_output_after_repair_records_failure_and_does_not_mutate_application()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("""{"companyName":"","roleTitle":""}""") }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var reopenedResponse = await client.GetAsync($"/api/applications/{application.Id}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened);
        Assert.Equal("Fallback Company", reopened.CompanyName);
        Assert.Equal("Fallback Role", reopened.RoleTitle);
        Assert.Equal("Draft", reopened.Status);
        Assert.Equal("{}", reopened.JobSignals);
        Assert.Equal(2, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("InvalidOutput", run.ErrorCode);
        Assert.Equal(2, run.AttemptCount);
        Assert.NotNull(run.CompletedAt);
    }

    [Fact]
    public async Task AnalyzeJob_with_unavailable_ollama_records_failure()
    {
        await using var factory = new TestApplicationFactory()
            .WithOllamaResponses([new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "Service Unavailable"
            }]);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("AiProvider", error.Details.Keys);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Equal("Ollama", run.Provider);
        Assert.Equal("llama3.1:8b", run.Model);
        Assert.Equal(1, run.AttemptCount);
    }

    [Fact]
    public async Task AnalyzeJob_with_ollama_records_second_attempt_when_repair_request_is_unavailable()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { ReasonPhrase = "Service Unavailable" }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task MatchEvidence_rejects_application_without_analyzed_job_signals()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("JobSignals", error.Details.Keys);
    }

    [Fact]
    public async Task MatchEvidence_rejects_when_no_approved_profile_facts_exist()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("ProfileFacts", error.Details.Keys);
    }

    [Fact]
    public async Task MatchEvidence_uses_only_approved_profile_facts_and_persists_unmatched_requirements()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        await CreateProfileFactAsync(client, "Draft Kubernetes work", "Draft", """["Kubernetes"]""");
        await CreateProfileFactAsync(client, "Archived React work", "Archived", """["React"]""");
        var application = await CreateApplicationAsync(client, "We need .NET, React, and Kubernetes.");
        await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        response.EnsureSuccessStatusCode();
        var matched = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        Assert.Equal("ReadyForReview", matched.Status);

        var evidenceMatches = JsonSerializer.Deserialize<List<EvidenceMatch>>(matched.EvidenceMatches, JsonOptions);
        var unmatchedRequirements = JsonSerializer.Deserialize<List<UnmatchedRequirement>>(matched.UnmatchedRequirements, JsonOptions);
        Assert.NotNull(evidenceMatches);
        Assert.NotNull(unmatchedRequirements);

        var match = Assert.Single(evidenceMatches);
        Assert.Equal(".NET", match.Signal);
        Assert.Equal("Approved API work", match.ProfileFactTitle);
        Assert.Contains(unmatchedRequirements, requirement => requirement.Requirement == "React");
        Assert.Contains(unmatchedRequirements, requirement => requirement.Requirement == "Kubernetes");
    }

    [Fact]
    public async Task MatchEvidence_with_ollama_stores_matches_unmatched_requirements_and_records_successful_ai_run()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>());
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var approvedFact = await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        await CreateProfileFactAsync(client, "Draft Kubernetes work", "Draft", """["Kubernetes"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OllamaGenerateContent(ValidOllamaEvidenceMatchingJson(approvedFact.Id))
        });

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var matched = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        Assert.Equal("ReadyForReview", matched.Status);

        var evidenceMatches = JsonSerializer.Deserialize<List<EvidenceMatch>>(matched.EvidenceMatches, JsonOptions);
        var unmatchedRequirements = JsonSerializer.Deserialize<List<UnmatchedRequirement>>(matched.UnmatchedRequirements, JsonOptions);
        Assert.NotNull(evidenceMatches);
        Assert.NotNull(unmatchedRequirements);
        var match = Assert.Single(evidenceMatches);
        Assert.Equal("dotnet", match.SignalId);
        Assert.Equal(".NET", match.Signal);
        Assert.Equal(approvedFact.Id, match.ProfileFactId);
        Assert.Equal("Approved API work", match.ProfileFactTitle);
        Assert.Contains(".NET", match.MatchedTerms);
        var unmatched = Assert.Single(unmatchedRequirements);
        Assert.Equal("kubernetes", unmatched.SignalId);
        Assert.Equal("Kubernetes", unmatched.Requirement);

        var ollamaRequestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("\"format\":\"json\"", ollamaRequestJson);
        Assert.Contains("\"stream\":false", ollamaRequestJson);
        Assert.Contains("Return only strict JSON", ollamaRequestJson);
        Assert.Contains(approvedFact.Id.ToString(), ollamaRequestJson);
        Assert.DoesNotContain("Draft Kubernetes work", ollamaRequestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal(application.Id, run.JobApplicationId);
        Assert.Equal("EvidenceMatching", run.Step);
        Assert.Equal("Ollama", run.Provider);
        Assert.Equal("llama3.1:8b", run.Model);
        Assert.Equal("Succeeded", run.Status);
        Assert.Equal(1, run.AttemptCount);
        Assert.Null(run.ErrorCode);
        Assert.Contains("\"matchCount\":1", run.OutputSummary);
    }

    [Fact]
    public async Task MatchEvidence_with_ollama_repairs_malformed_json_once_and_records_repaired_success()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>());
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var approvedFact = await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") });
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OllamaGenerateContent(ValidOllamaEvidenceMatchingJson(approvedFact.Id))
        });

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task MatchEvidence_with_ollama_repairs_draft_fact_reference_once()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>());
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var approvedFact = await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var draftFact = await CreateProfileFactAsync(client, "Draft Kubernetes work", "Draft", """["Kubernetes"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OllamaGenerateContent(ValidOllamaEvidenceMatchingJson(draftFact.Id))
        });
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OllamaGenerateContent(ValidOllamaEvidenceMatchingJson(approvedFact.Id))
        });

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task MatchEvidence_with_ollama_repairs_incomplete_signal_coverage_once()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>());
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var approvedFact = await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OllamaGenerateContent($$"""
                {
                  "evidenceMatches": [
                    {
                      "signalId": "dotnet",
                      "profileFactId": "{{approvedFact.Id}}",
                      "summary": "Approved API work demonstrates .NET experience.",
                      "matchedTerms": [".NET"]
                    }
                  ],
                  "unmatchedRequirements": []
                }
                """)
        });
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OllamaGenerateContent(ValidOllamaEvidenceMatchingJson(approvedFact.Id))
        });

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task MatchEvidence_with_ollama_invalid_output_after_repair_records_failure_and_does_not_mutate_application()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>());
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") });
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OllamaGenerateContent("""
                {
                  "evidenceMatches": [
                    {
                      "signalId": "unknown-signal",
                      "profileFactId": "00000000-0000-0000-0000-000000000000",
                      "summary": "Unsupported reference.",
                      "matchedTerms": [".NET"]
                    }
                  ],
                  "unmatchedRequirements": []
                }
                """)
        });

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2, handler.Requests.Count);
        var reopenedResponse = await client.GetAsync($"/api/applications/{application.Id}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened);
        Assert.Equal("PostingCaptured", reopened.Status);
        Assert.Equal("[]", reopened.EvidenceMatches);
        Assert.Equal("[]", reopened.UnmatchedRequirements);
        Assert.Equal("[]", reopened.ApprovedEvidence);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("InvalidOutput", run.ErrorCode);
        Assert.Equal(2, run.AttemptCount);
        Assert.NotNull(run.CompletedAt);
    }

    [Fact]
    public async Task MatchEvidence_with_unavailable_ollama_records_failure()
    {
        await using var factory = new TestApplicationFactory()
            .WithOllamaResponses([new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "Service Unavailable"
            }]);
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("AiProvider", error!.Details!.Keys);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Equal("EvidenceMatching", run.Step);
        Assert.Equal("Ollama", run.Provider);
        Assert.Equal("llama3.1:8b", run.Model);
        Assert.Equal(1, run.AttemptCount);
    }

    [Fact]
    public async Task PutApprovedEvidence_persists_only_submitted_current_matches()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET", "React"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and React.");
        await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);
        var matchResponse = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);
        var matched = await matchResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        var evidenceMatches = JsonSerializer.Deserialize<List<EvidenceMatch>>(matched.EvidenceMatches, JsonOptions);
        Assert.NotNull(evidenceMatches);
        var selected = Assert.Single(evidenceMatches, match => match.Signal == ".NET");

        var response = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest(JsonSerializer.Serialize(new[] { new { selected.Id } }, JsonOptions)));

        response.EnsureSuccessStatusCode();
        var reviewed = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reviewed);
        Assert.NotEqual("{}", reviewed.JobSignals);
        Assert.NotEqual("[]", reviewed.EvidenceMatches);
        var approvedEvidence = JsonSerializer.Deserialize<List<EvidenceMatch>>(reviewed.ApprovedEvidence, JsonOptions);
        Assert.NotNull(approvedEvidence);
        var approved = Assert.Single(approvedEvidence);
        Assert.Equal(selected.Id, approved.Id);
        Assert.Equal(selected.ProfileFactId, approved.ProfileFactId);
    }

    [Fact]
    public async Task PutApprovedEvidence_rejects_unknown_or_malformed_review_state()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var unknownIdResponse = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest("""[{"id":"missing"}]"""));
        var malformedResponse = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest("""{"id":"missing"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, unknownIdResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malformedResponse.StatusCode);
    }

    [Fact]
    public async Task GenerateDraft_creates_current_draft_from_approved_evidence()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET", "React"]""");
        var application = await CreateApplicationAsync(
            client,
            """
            Company: Northwind
            Role: Full-stack Developer

            We need .NET, React, and Kubernetes.
            """,
            "English");
        Assert.NotEqual(Guid.Empty, application.Id);
        var analysisResponse = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);
        Assert.True(analysisResponse.IsSuccessStatusCode, await analysisResponse.Content.ReadAsStringAsync());
        var matchResponse = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);
        Assert.True(matchResponse.IsSuccessStatusCode, await matchResponse.Content.ReadAsStringAsync());
        var matched = await matchResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        var evidenceMatches = JsonSerializer.Deserialize<List<EvidenceMatch>>(matched.EvidenceMatches, JsonOptions);
        Assert.NotNull(evidenceMatches);
        await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest(JsonSerializer.Serialize(evidenceMatches, JsonOptions)));

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        response.EnsureSuccessStatusCode();
        var draft = await response.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(draft);
        Assert.Equal(application.Id, draft.JobApplicationId);
        Assert.Contains("Northwind", draft.CoverLetterText);
        Assert.Contains("Full-stack Developer", draft.CoverLetterText);
        Assert.Contains("Approved API work", draft.CoverLetterText);
        Assert.Contains("Kubernetes", draft.CoverLetterText);
        Assert.Contains("Northwind", draft.ShortMotivationText);
        Assert.Contains("Approved API work", draft.ShortMotivationText);
        Assert.Null(draft.LastEditedAt);
        Assert.Null(draft.AuditUpdatedAt);
    }

    [Fact]
    public async Task GenerateDraft_rejects_application_without_job_posting_text()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, jobPostingText: "");

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationRequest.JobPostingText), error.Details.Keys);
    }

    [Fact]
    public async Task GenerateDraft_rejects_application_without_approved_evidence()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationResponse.ApprovedEvidence), error.Details.Keys);
    }

    [Fact]
    public async Task GenerateDraft_updates_the_current_generated_draft_as_latest_state()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET.");
        await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);
        var matchResponse = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);
        Assert.True(matchResponse.IsSuccessStatusCode, await matchResponse.Content.ReadAsStringAsync());
        var matched = await matchResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest(matched.EvidenceMatches));
        var firstResponse = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);
        firstResponse.EnsureSuccessStatusCode();
        var first = await firstResponse.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(first);

        var secondResponse = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        secondResponse.EnsureSuccessStatusCode();
        var second = await secondResponse.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(second);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.JobApplicationId, second.JobApplicationId);
        Assert.Equal(first.CreatedAt, second.CreatedAt);
        Assert.True(second.GeneratedAt >= first.GeneratedAt);
    }

    [Fact]
    public async Task GenerateDraft_with_ollama_persists_current_draft_and_records_successful_ai_run()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaDraftGenerationJson()) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.", "English");
        await MarkApplicationReadyForDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var draft = await response.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(draft);
        Assert.Equal("Ollama cover letter from approved API evidence.", draft.CoverLetterText);
        Assert.Equal("Ollama short motivation.", draft.ShortMotivationText);
        Assert.Null(draft.AuditUpdatedAt);

        var ollamaRequestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("\"format\":\"json\"", ollamaRequestJson);
        Assert.Contains("Return only strict JSON", ollamaRequestJson);
        Assert.Contains("Approved API work", ollamaRequestJson);
        Assert.Contains("honest learning area", ollamaRequestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("DraftGeneration", run.Step);
        Assert.Equal("Ollama", run.Provider);
        Assert.Equal("Succeeded", run.Status);
        Assert.Equal(1, run.AttemptCount);
        Assert.Null(run.ErrorCode);
    }

    [Fact]
    public async Task GenerateDraft_with_ollama_repairs_structurally_invalid_json_once()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("""{"coverLetterText":"","shortMotivationText":""}""") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaDraftGenerationJson()) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.", "English");
        await MarkApplicationReadyForDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task GenerateDraft_with_ollama_repairs_malformed_json_once()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaDraftGenerationJson()) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.", "English");
        await MarkApplicationReadyForDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task GenerateDraft_with_ollama_invalid_output_after_repair_records_failure_and_does_not_mutate_existing_draft()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("""{"coverLetterText":"","shortMotivationText":""}""") }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.", "English");
        await MarkApplicationReadyForDraftAsync(factory, application.Id);
        var existingDraft = await AddGeneratedDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var reopenedResponse = await client.GetAsync($"/api/applications/{application.Id}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened);
        Assert.NotNull(reopened.GeneratedDraft);
        Assert.Equal(existingDraft.Id, reopened.GeneratedDraft.Id);
        Assert.Equal(existingDraft.CoverLetterText, reopened.GeneratedDraft.CoverLetterText);
        Assert.Equal(existingDraft.ShortMotivationText, reopened.GeneratedDraft.ShortMotivationText);
        Assert.Equal(existingDraft.ClaimAudit, reopened.GeneratedDraft.ClaimAudit);
        Assert.Equal(existingDraft.AuditUpdatedAt, reopened.GeneratedDraft.AuditUpdatedAt);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("InvalidOutput", run.ErrorCode);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task GenerateDraft_with_unavailable_ollama_records_failure()
    {
        await using var factory = new TestApplicationFactory()
            .WithOllamaResponses([new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "Service Unavailable"
            }]);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.", "English");
        await MarkApplicationReadyForDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Equal("DraftGeneration", run.Step);
        Assert.Equal("Ollama", run.Provider);
        Assert.Equal("llama3.1:8b", run.Model);
        Assert.Equal(1, run.AttemptCount);
    }

    [Fact]
    public async Task GenerateDraft_with_ollama_updates_existing_draft_as_latest_state()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaDraftGenerationJson()) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.", "English");
        await MarkApplicationReadyForDraftAsync(factory, application.Id);
        var existingDraft = await AddGeneratedDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var draft = await response.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(draft);
        Assert.Equal(existingDraft.Id, draft.Id);
        Assert.Equal(existingDraft.CreatedAt, draft.CreatedAt);
        Assert.Equal("Ollama cover letter from approved API evidence.", draft.CoverLetterText);
        Assert.Equal("Ollama short motivation.", draft.ShortMotivationText);
        Assert.Equal("{}", draft.ClaimAudit);
        Assert.Null(draft.AuditUpdatedAt);
    }

    [Fact]
    public async Task GetApplication_includes_current_generated_draft_when_one_exists()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftAsync(client);

        var response = await client.GetAsync($"/api/applications/{draft.JobApplicationId}");

        response.EnsureSuccessStatusCode();
        var application = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(application);
        Assert.NotNull(application.GeneratedDraft);
        Assert.Equal(draft.Id, application.GeneratedDraft.Id);
        Assert.Equal(draft.CoverLetterText, application.GeneratedDraft.CoverLetterText);
        Assert.Equal(draft.ShortMotivationText, application.GeneratedDraft.ShortMotivationText);
    }

    [Fact]
    public async Task GetApplications_includes_current_generated_draft_when_one_exists()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftAsync(client);
        await MarkDraftAuditedAsync(factory, draft.Id);

        var response = await client.GetAsync("/api/applications");

        response.EnsureSuccessStatusCode();
        var applications = await response.Content.ReadFromJsonAsync<List<ApplicationResponse>>();
        Assert.NotNull(applications);
        var application = Assert.Single(applications);
        Assert.NotNull(application.GeneratedDraft);
        Assert.Equal(draft.Id, application.GeneratedDraft.Id);
        Assert.False(application.GeneratedDraft.IsClaimAuditStale);
        Assert.NotNull(application.GeneratedDraft.AuditUpdatedAt);
    }

    [Fact]
    public async Task GetApplications_hides_archived_sessions_by_default_and_includes_them_when_requested()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateApplicationAsync(client, "We need .NET.", status: "Archived", companyName: "Archived Co");
        var active = await CreateApplicationAsync(client, "We need React.", companyName: "Active Co");

        var activeResponse = await client.GetAsync("/api/applications");
        var allResponse = await client.GetAsync("/api/applications?includeArchived=true");

        activeResponse.EnsureSuccessStatusCode();
        allResponse.EnsureSuccessStatusCode();
        var activeApplications = await activeResponse.Content.ReadFromJsonAsync<List<ApplicationResponse>>();
        var allApplications = await allResponse.Content.ReadFromJsonAsync<List<ApplicationResponse>>();
        Assert.NotNull(activeApplications);
        Assert.NotNull(allApplications);
        var listed = Assert.Single(activeApplications);
        Assert.Equal(active.Id, listed.Id);
        Assert.Equal(2, allApplications.Count);
        Assert.Contains(allApplications, application => application.Status == "Archived");
    }

    [Fact]
    public async Task GetApplications_filters_history_by_search_status_and_audit_readiness()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftAsync(client);
        await MarkDraftAuditedAsync(factory, draft.Id);
        await CreateApplicationAsync(client, "We need React.", status: "Applied", companyName: "Tailspin", roleTitle: "Frontend Engineer");

        var response = await client.GetAsync("/api/applications?search=fallback&status=ReadyForReview&readiness=Current");

        response.EnsureSuccessStatusCode();
        var applications = await response.Content.ReadFromJsonAsync<List<ApplicationResponse>>();
        Assert.NotNull(applications);
        var application = Assert.Single(applications);
        Assert.Equal(draft.JobApplicationId, application.Id);
        Assert.True(application.HasGeneratedDraft);
        Assert.Equal("Current", application.AuditReadiness);
    }

    [Fact]
    public async Task GetApplications_exposes_current_stale_missing_and_not_applicable_audit_readiness()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var current = await CreateApplicationAsync(client, "We need .NET.", companyName: "Current Audit");
        var stale = await CreateApplicationAsync(client, "We need React.", companyName: "Stale Audit");
        var missing = await CreateApplicationAsync(client, "We need SQL.", companyName: "Missing Audit");
        var notApplicable = await CreateApplicationAsync(client, "We need Azure.", companyName: "No Draft");
        await AddGeneratedDraftAsync(factory, current.Id);
        await AddGeneratedDraftAsync(factory, stale.Id, isClaimAuditStale: true);
        await AddGeneratedDraftAsync(factory, missing.Id, claimAudit: "{}", auditUpdatedAt: null);

        var response = await client.GetAsync("/api/applications");

        response.EnsureSuccessStatusCode();
        var applications = await response.Content.ReadFromJsonAsync<List<ApplicationResponse>>();
        Assert.NotNull(applications);
        Assert.Equal("Current", applications.Single(application => application.Id == current.Id).AuditReadiness);
        Assert.Equal("Stale", applications.Single(application => application.Id == stale.Id).AuditReadiness);
        Assert.Equal("Missing", applications.Single(application => application.Id == missing.Id).AuditReadiness);
        Assert.Equal("NotApplicable", applications.Single(application => application.Id == notApplicable.Id).AuditReadiness);
    }

    [Fact]
    public async Task GetApplications_rejects_unknown_history_filters()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/applications?status=Unknown&readiness=AlmostReady");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("status", error.Details.Keys);
        Assert.Contains("readiness", error.Details.Keys);
    }

    [Fact]
    public async Task PutGeneratedDraft_saves_manual_edits_preserves_generation_metadata_and_marks_audit_stale()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftAsync(client);
        await MarkDraftAuditedAsync(factory, draft.Id);

        var response = await client.PutAsJsonAsync(
            $"/api/applications/{draft.JobApplicationId}/generated-draft",
            new GeneratedDraftEditRequest(
                "Edited cover letter text.",
                "Edited short motivation text."));

        response.EnsureSuccessStatusCode();
        var edited = await response.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(edited);
        Assert.Equal(draft.Id, edited.Id);
        Assert.Equal("Edited cover letter text.", edited.CoverLetterText);
        Assert.Equal("Edited short motivation text.", edited.ShortMotivationText);
        Assert.Equal(draft.GeneratedAt, edited.GeneratedAt);
        Assert.Equal(draft.CreatedAt, edited.CreatedAt);
        Assert.NotNull(edited.LastEditedAt);
        Assert.Equal(edited.LastEditedAt, edited.UpdatedAt);
        Assert.NotNull(edited.AuditUpdatedAt);
        Assert.True(edited.IsClaimAuditStale);

        var reopenedResponse = await client.GetAsync($"/api/applications/{draft.JobApplicationId}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened?.GeneratedDraft);
        Assert.Equal("Edited cover letter text.", reopened.GeneratedDraft.CoverLetterText);
        Assert.Equal("Edited short motivation text.", reopened.GeneratedDraft.ShortMotivationText);
        Assert.True(reopened.GeneratedDraft.IsClaimAuditStale);
    }

    [Fact]
    public async Task PutGeneratedDraft_rejects_application_without_generated_draft()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/generated-draft",
            new GeneratedDraftEditRequest("Cover letter.", "Short motivation."));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationResponse.GeneratedDraft), error.Details.Keys);
    }

    [Fact]
    public async Task AuditClaims_rejects_application_without_generated_draft()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/audit-claims", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationResponse.GeneratedDraft), error.Details.Keys);
    }

    [Fact]
    public async Task AuditClaims_persists_structured_results_updates_timestamp_and_clears_stale_state()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftAsync(client);
        var firstAuditResponse = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);
        firstAuditResponse.EnsureSuccessStatusCode();
        var firstAudit = await firstAuditResponse.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(firstAudit);
        Assert.NotNull(firstAudit.AuditUpdatedAt);
        await client.PutAsJsonAsync(
            $"/api/applications/{draft.JobApplicationId}/generated-draft",
            new GeneratedDraftEditRequest(
                """
                Evidence for Approved API work.
                Led Kubernetes platform operations.
                I may be a fit for the team.
                """,
                ""));

        var response = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);

        response.EnsureSuccessStatusCode();
        var audited = await response.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(audited);
        Assert.NotNull(audited.AuditUpdatedAt);
        Assert.True(audited.AuditUpdatedAt >= firstAudit.AuditUpdatedAt);
        Assert.False(audited.IsClaimAuditStale);

        var audit = JsonSerializer.Deserialize<ClaimAuditResult>(audited.ClaimAudit, JsonOptions);
        Assert.NotNull(audit);
        Assert.Contains(
            audit.Claims,
            claim => claim.Status == "Supported" && claim.EvidenceIds.Any(id => id.StartsWith("match-dotnet", StringComparison.Ordinal)));
        Assert.Contains(audit.Claims, claim => claim.Status == "Unsupported" && claim.Text == "Led Kubernetes platform operations.");
        Assert.Contains(audit.Claims, claim => claim.Status == "NeedsReview" && claim.Text == "I may be a fit for the team.");

        var reopenedResponse = await client.GetAsync($"/api/applications/{draft.JobApplicationId}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened?.GeneratedDraft);
        Assert.Equal(audited.ClaimAudit, reopened.GeneratedDraft.ClaimAudit);
        Assert.False(reopened.GeneratedDraft.IsClaimAuditStale);
    }

    [Fact]
    public async Task AuditClaims_with_ollama_persists_structured_results_and_records_successful_ai_run()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftForOllamaAuditAsync(factory, client, "match-dotnet-test");

        var response = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var audited = await response.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(audited);
        Assert.NotNull(audited.AuditUpdatedAt);
        Assert.False(audited.IsClaimAuditStale);
        var audit = JsonSerializer.Deserialize<ClaimAuditResult>(audited.ClaimAudit, JsonOptions);
        Assert.NotNull(audit);
        Assert.Contains(audit.Claims, claim => claim.Status == "Supported" && claim.EvidenceIds.Contains("match-dotnet-test"));
        Assert.Contains(audit.Claims, claim => claim.Status == "Unsupported");
        Assert.Contains(audit.Claims, claim => claim.Status == "NeedsReview");

        var ollamaRequestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("Return only strict JSON", ollamaRequestJson);
        Assert.Contains("match-dotnet-test", ollamaRequestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("ClaimAudit", run.Step);
        Assert.Equal("Succeeded", run.Status);
        Assert.Equal(1, run.AttemptCount);
    }

    [Fact]
    public async Task AuditClaims_with_ollama_repairs_unknown_evidence_reference_once()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaClaimAuditJson("unknown-match")) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftForOllamaAuditAsync(factory, client, "match-dotnet-test");

        var response = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task AuditClaims_with_ollama_repairs_malformed_json_once()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftForOllamaAuditAsync(factory, client, "match-dotnet-test");

        var response = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task AuditClaims_with_ollama_invalid_output_after_repair_records_failure_and_does_not_mutate_existing_audit()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("""{"claims":[{"id":"claim-1","text":"Broken","status":"Maybe","evidenceIds":[]}]}""") }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftForOllamaAuditAsync(factory, client, "match-dotnet-test");
        await MarkDraftAuditedAsync(factory, draft.Id);

        var response = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var reopenedResponse = await client.GetAsync($"/api/applications/{draft.JobApplicationId}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened?.GeneratedDraft);
        Assert.Equal("""{"status":"current"}""", reopened.GeneratedDraft.ClaimAudit);
        Assert.NotNull(reopened.GeneratedDraft.AuditUpdatedAt);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("InvalidOutput", run.ErrorCode);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task AuditClaims_with_unavailable_ollama_records_failure()
    {
        await using var factory = new TestApplicationFactory()
            .WithOllamaResponses([new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "Service Unavailable"
            }]);
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftForOllamaAuditAsync(factory, client, "match-dotnet-test");

        var response = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Equal("ClaimAudit", run.Step);
        Assert.Equal("Ollama", run.Provider);
        Assert.Equal("llama3.1:8b", run.Model);
        Assert.Equal(1, run.AttemptCount);
    }

    private static async Task<ApplicationResponse> CreateApplicationAsync(
        HttpClient client,
        string jobPostingText,
        string selectedLanguage = "",
        string status = "Draft",
        string companyName = "Fallback Company",
        string roleTitle = "Fallback Role")
    {
        var response = await client.PostAsJsonAsync(
            "/api/applications",
            new ApplicationRequest(
                companyName,
                roleTitle,
                null,
                null,
                status,
                jobPostingText,
                null,
                selectedLanguage));

        response.EnsureSuccessStatusCode();
        var application = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(application);
        Assert.NotEqual(Guid.Empty, application.Id);
        return application;
    }

    private static async Task<GeneratedDraftResponse> CreateGeneratedDraftAsync(HttpClient client)
    {
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET.");
        await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);
        var matchResponse = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);
        Assert.True(matchResponse.IsSuccessStatusCode, await matchResponse.Content.ReadAsStringAsync());
        var matched = await matchResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest(matched.EvidenceMatches));
        var draftResponse = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);
        draftResponse.EnsureSuccessStatusCode();
        var draft = await draftResponse.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(draft);
        return draft;
    }

    private static async Task MarkApplicationReadyForDraftAsync(WebApplicationFactory<Program> factory, Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var application = await db.JobApplications.FindAsync(applicationId);
        Assert.NotNull(application);
        application.ApprovedEvidence = JsonSerializer.Serialize(
            new[]
            {
                new EvidenceMatch(
                    "match-dotnet-test",
                    "dotnet",
                    ".NET",
                    "RequiredSkill",
                    Guid.NewGuid(),
                    "Approved API work",
                    "Approved API work demonstrates .NET delivery.",
                    [".NET"])
            },
            JsonOptions);
        application.UnmatchedRequirements = JsonSerializer.Serialize(
            new[]
            {
                new UnmatchedRequirement(
                    "unmatched-kubernetes",
                    "kubernetes",
                    "Kubernetes",
                    "PreferredSkill",
                    "Mention Kubernetes as an honest learning area.")
            },
            JsonOptions);
        await db.SaveChangesAsync();
    }

    private static async Task<GeneratedDraftResponse> CreateGeneratedDraftForOllamaAuditAsync(
        WebApplicationFactory<Program> factory,
        HttpClient client,
        string evidenceId)
    {
        var application = await CreateApplicationAsync(client, "We need .NET.", "English");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await db.JobApplications.FindAsync(application.Id);
        Assert.NotNull(stored);
        stored.ApprovedEvidence = JsonSerializer.Serialize(
            new[]
            {
                new EvidenceMatch(
                    evidenceId,
                    "dotnet",
                    ".NET",
                    "RequiredSkill",
                    Guid.NewGuid(),
                    "Approved API work",
                    "Approved API work demonstrates .NET delivery.",
                    [".NET"])
            },
            JsonOptions);
        var now = DateTimeOffset.UtcNow;
        var draft = new GeneratedDraft
        {
            Id = Guid.NewGuid(),
            JobApplicationId = stored.Id,
            CoverLetterText = "I delivered .NET APIs. I led Kubernetes operations. I may be a fit.",
            ShortMotivationText = "I am interested in this .NET role.",
            ClaimAudit = "{}",
            GeneratedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.GeneratedDrafts.Add(draft);
        await db.SaveChangesAsync();

        return new GeneratedDraftResponse(
            draft.Id,
            draft.JobApplicationId,
            draft.CoverLetterText,
            draft.ShortMotivationText,
            draft.ClaimAudit,
            draft.GeneratedAt,
            draft.LastEditedAt,
            draft.AuditUpdatedAt,
            draft.CreatedAt,
            draft.UpdatedAt,
            draft.IsClaimAuditStale);
    }

    private static async Task<GeneratedDraftResponse> AddGeneratedDraftAsync(
        WebApplicationFactory<Program> factory,
        Guid applicationId,
        string claimAudit = """{"status":"current"}""",
        DateTimeOffset? auditUpdatedAt = default,
        bool isClaimAuditStale = false)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        auditUpdatedAt ??= claimAudit == "{}" ? null : now;
        var draft = new GeneratedDraft
        {
            Id = Guid.NewGuid(),
            JobApplicationId = applicationId,
            CoverLetterText = "Existing cover letter.",
            ShortMotivationText = "Existing motivation.",
            ClaimAudit = claimAudit,
            GeneratedAt = now,
            AuditUpdatedAt = auditUpdatedAt,
            IsClaimAuditStale = isClaimAuditStale,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.GeneratedDrafts.Add(draft);
        await db.SaveChangesAsync();

        return new GeneratedDraftResponse(
            draft.Id,
            draft.JobApplicationId,
            draft.CoverLetterText,
            draft.ShortMotivationText,
            draft.ClaimAudit,
            draft.GeneratedAt,
            draft.LastEditedAt,
            draft.AuditUpdatedAt,
            draft.CreatedAt,
            draft.UpdatedAt,
            draft.IsClaimAuditStale);
    }

    private static async Task MarkApplicationAnalyzedAsync(WebApplicationFactory<Program> factory, Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var application = await db.JobApplications.FindAsync(applicationId);
        Assert.NotNull(application);
        application.Status = "PostingCaptured";
        application.JobSignals = JsonSerializer.Serialize(
            new JobSignalsDocument(
                "Test",
                DateTimeOffset.UtcNow,
                [".NET"],
                ["Kubernetes"],
                [],
                [
                    new JobSignal("dotnet", ".NET", "RequiredSkill", [".net"]),
                    new JobSignal("kubernetes", "Kubernetes", "PreferredSkill", ["kubernetes"])
                ]),
            JsonOptions);
        await db.SaveChangesAsync();
    }

    private static async Task MarkDraftAuditedAsync(WebApplicationFactory<Program> factory, Guid draftId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var draft = await db.GeneratedDrafts.FindAsync(draftId);
        Assert.NotNull(draft);
        draft.ClaimAudit = """{"status":"current"}""";
        draft.AuditUpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    private static async Task<ProfileFactResponse> CreateProfileFactAsync(
        HttpClient client,
        string title,
        string status,
        string technologies)
    {
        var response = await client.PostAsJsonAsync(
            "/api/profile/facts",
            new ProfileFactRequest(
                "Project",
                title,
                $"Evidence for {title}.",
                status,
                null,
                technologies,
                null,
                null));

        response.EnsureSuccessStatusCode();
        var fact = await response.Content.ReadFromJsonAsync<ProfileFactResponse>();
        Assert.NotNull(fact);
        return fact;
    }

    private static JsonContent OllamaGenerateContent(string response) =>
        JsonContent.Create(new { response });

    private static string ValidOllamaAnalysisJson(string companyName, string roleTitle) =>
        $$"""
        {
          "companyName": "{{companyName}}",
          "roleTitle": "{{roleTitle}}",
          "detectedLanguage": "English",
          "selectedLanguage": "English",
          "jobSignals": {
            "requiredSkills": [".NET", "PostgreSQL"],
            "preferredSkills": ["Kubernetes"],
            "responsibilities": ["Build REST APIs"],
            "signals": [
              { "id": "dotnet", "label": ".NET", "category": "RequiredSkill", "keywords": [".net"] },
              { "id": "postgresql", "label": "PostgreSQL", "category": "RequiredSkill", "keywords": ["postgresql"] },
              { "id": "kubernetes", "label": "Kubernetes", "category": "PreferredSkill", "keywords": ["kubernetes"] },
              { "id": "rest-api", "label": "Build REST APIs", "category": "Responsibility", "keywords": ["rest", "api"] }
            ]
          }
        }
        """;

    private static string ValidOllamaEvidenceMatchingJson(Guid profileFactId) =>
        $$"""
        {
          "evidenceMatches": [
            {
              "signalId": "dotnet",
              "profileFactId": "{{profileFactId}}",
              "summary": "Approved API work demonstrates .NET experience.",
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

    private static string ValidOllamaDraftGenerationJson() =>
        """
        {
          "coverLetterText": "Ollama cover letter from approved API evidence.",
          "shortMotivationText": "Ollama short motivation."
        }
        """;

    private static string ValidOllamaClaimAuditJson(string evidenceId) =>
        $$"""
        {
          "claims": [
            {
              "id": "claim-1",
              "text": "I delivered .NET APIs.",
              "status": "Supported",
              "evidenceIds": ["{{evidenceId}}"]
            },
            {
              "id": "claim-2",
              "text": "I led Kubernetes operations.",
              "status": "Unsupported",
              "evidenceIds": []
            },
            {
              "id": "claim-3",
              "text": "I may be a fit.",
              "status": "NeedsReview",
              "evidenceIds": []
            }
          ]
        }
        """;

    internal sealed class QueuedOllamaHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public void Enqueue(HttpResponseMessage response) => responses.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responses.Dequeue());
        }
    }
}

internal static class TestApplicationFactoryOllamaExtensions
{
    public static WebApplicationFactory<Program> WithOllamaResponses(
        this TestApplicationFactory factory,
        IReadOnlyList<HttpResponseMessage> responses) =>
        factory.WithOllamaHandler(new ApplicationWorkflowApiTests.QueuedOllamaHandler(new Queue<HttpResponseMessage>(responses)));

    public static WebApplicationFactory<Program> WithOllamaHandler(
        this TestApplicationFactory factory,
        ApplicationWorkflowApiTests.QueuedOllamaHandler handler) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiProvider>();
                services.RemoveAll<AiOptions>();
                services.AddSingleton(new AiOptions
                {
                    Provider = "Ollama",
                    Endpoint = "http://ollama.test",
                    Model = "llama3.1:8b",
                    TimeoutSeconds = 1
                });
                services.AddSingleton<IAiProvider>(serviceProvider =>
                {
                    var options = serviceProvider.GetRequiredService<AiOptions>();
                    var client = new HttpClient(handler)
                    {
                        BaseAddress = new Uri(options.Endpoint),
                        Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
                    };

                    return new OllamaAiProvider(client, options);
                });
            });
        });
}
