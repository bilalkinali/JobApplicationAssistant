using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IO.Compression;
using System.Text.Json;
using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Domain;
using JobApplicationAssistant.Api.Imports;
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
    public async Task AnalyzeJob_with_openai_compatible_updates_application_from_chat_completion_json()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = OpenAiChatCompletionContent(ValidOllamaAnalysisJson("Contoso", "Platform Engineer"))
            }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET, PostgreSQL, Kubernetes, and REST APIs.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var analyzed = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(analyzed);
        Assert.Equal("Contoso", analyzed.CompanyName);
        Assert.Equal("Platform Engineer", analyzed.RoleTitle);

        var signals = JsonSerializer.Deserialize<JobSignalsDocument>(analyzed.JobSignals, JsonOptions);
        Assert.NotNull(signals);
        Assert.Equal("OpenAiCompatible", signals.Provider);
        Assert.Contains(".NET", signals.RequiredSkills);

        var requestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("chat/completions", handler.Requests[0].RequestUri!.ToString());
        Assert.Contains("\"model\":\"local-model\"", requestJson);
        Assert.Contains("\"response_format\":{\"type\":\"text\"}", requestJson);
        Assert.Contains("Return only strict JSON", requestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("OpenAiCompatible", run.Provider);
        Assert.Equal("local-model", run.Model);
        Assert.Equal("Succeeded", run.Status);
        Assert.Equal(1, run.AttemptCount);
    }

    [Fact]
    public async Task AnalyzeJob_with_openai_compatible_repairs_malformed_json_once()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidOllamaAnalysisJson("Northwind", "API Engineer")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET and REST APIs.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task AnalyzeJob_with_openai_compatible_repairs_structurally_invalid_json_once()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("""{"companyName":"","roleTitle":"Missing Signals"}""") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidOllamaAnalysisJson("Tailspin", "Backend Engineer")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need PostgreSQL and Kubernetes.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var analyzed = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(analyzed);
        Assert.Equal("Tailspin", analyzed.CompanyName);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task AnalyzeJob_with_openai_compatible_empty_assistant_content_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(" ") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(" ") }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(handler.Requests);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("InvalidOutput", run.ErrorCode);
        Assert.Equal(1, run.AttemptCount);
    }

    [Fact]
    public async Task AnalyzeJob_with_openai_compatible_invalid_output_after_repair_records_raw_payload_when_enabled()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("""{"companyName":"","roleTitle":""}""") }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            storeRawPayloads: true);
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

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("InvalidOutput", run.ErrorCode);
        Assert.Equal(2, run.AttemptCount);
        Assert.Contains("""{"companyName":"","roleTitle":""}""", run.ErrorMessage);
    }

    [Fact]
    public async Task AnalyzeJob_with_openai_compatible_api_error_records_raw_payload_when_enabled()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "Service Unavailable",
                Content = JsonContent.Create(new { error = new { message = "model unavailable" } })
            }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            storeRawPayloads: true);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Contains("Raw request:", run.ErrorMessage);
        Assert.Contains("chat/completions", handler.Requests[0].RequestUri!.ToString());
        Assert.Contains("We need .NET.", run.ErrorMessage);
        Assert.Contains("Raw response:", run.ErrorMessage);
        Assert.Contains("model unavailable", run.ErrorMessage);
    }

    [Fact]
    public async Task AnalyzeJob_with_openai_compatible_long_raw_payload_failure_fits_ai_run_error_limit()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "Service Unavailable",
                Content = JsonContent.Create(new { error = new { message = "model unavailable" } })
            }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            storeRawPayloads: true);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, string.Concat(Enumerable.Repeat("We need .NET. ", 500)));

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.NotNull(run.ErrorMessage);
        Assert.True(run.ErrorMessage.Length <= 4000);
        Assert.Contains("[truncated for AiRun limit]", run.ErrorMessage);
    }

    [Fact]
    public async Task AnalyzeJob_with_openai_compatible_timeout_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(
            new Queue<HttpResponseMessage>(),
            new TaskCanceledException("timed out"));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            storeRawPayloads: true);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Equal(1, run.AttemptCount);
        Assert.Contains("Raw request:", run.ErrorMessage);
        Assert.Contains("Error context: Request timed out.", run.ErrorMessage);
    }

    [Fact]
    public async Task AnalyzeJob_with_openai_compatible_unreachable_endpoint_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(
            new Queue<HttpResponseMessage>(),
            new HttpRequestException("unreachable"));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Equal(1, run.AttemptCount);
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
    public async Task PrepareApplication_rejects_application_without_job_posting_text()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, jobPostingText: "");

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationRequest.JobPostingText), error.Details.Keys);
    }

    [Fact]
    public async Task PrepareApplication_rejects_when_no_approved_profile_facts_exist()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("ProfileFacts", error.Details.Keys);
    }

    [Fact]
    public async Task PrepareApplication_runs_analysis_and_matching_and_returns_evidence_review_checkpoint()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await SetApprovedEvidenceAsync(factory, application.Id, """[{"id":"stale-evidence"}]""");

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var prepared = await response.Content.ReadFromJsonAsync<PrepareApplicationResponse>();
        Assert.NotNull(prepared);
        Assert.Equal("ReviewEvidence", prepared.NextCheckpoint);
        Assert.Equal("PreparedForEvidenceReview", prepared.Application.Status);
        Assert.Equal("PreparedForEvidenceReview", prepared.Application.PreparationStatus);
        Assert.NotNull(prepared.Application.LastPreparedAt);
        Assert.Equal("[]", prepared.Application.ApprovedEvidence);

        var signals = JsonSerializer.Deserialize<JobSignalsDocument>(prepared.Application.JobSignals, JsonOptions);
        var candidateFitBrief = JsonSerializer.Deserialize<CandidateFitBriefResult>(prepared.Application.CandidateFitBrief, JsonOptions);
        var evidenceMatches = JsonSerializer.Deserialize<List<EvidenceMatch>>(prepared.Application.EvidenceMatches, JsonOptions);
        var unmatchedRequirements = JsonSerializer.Deserialize<List<UnmatchedRequirement>>(prepared.Application.UnmatchedRequirements, JsonOptions);
        Assert.NotNull(signals);
        Assert.NotNull(candidateFitBrief);
        Assert.NotNull(evidenceMatches);
        Assert.NotNull(unmatchedRequirements);
        Assert.Contains(".NET", signals.RequiredSkills);
        Assert.Contains("Approved API work", prepared.Application.CandidateFitBrief);
        Assert.Single(evidenceMatches);
        Assert.All(evidenceMatches, match =>
        {
            Assert.False(string.IsNullOrWhiteSpace(match.Quality));
            Assert.False(string.IsNullOrWhiteSpace(match.Reason));
        });
        Assert.Contains(unmatchedRequirements, requirement => requirement.Requirement == "Kubernetes");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var runSteps = db.AiRuns.Select(run => run.Step).ToList();
        Assert.Contains("JobAnalysis", runSteps);
        Assert.Contains("CandidateFitBrief", runSteps);
        Assert.Contains("EvidenceMatching", runSteps);
    }

    [Fact]
    public async Task PrepareApplication_persists_evidence_quality_and_keeps_weak_evidence_unapproved()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved .NET API work", "Approved", """[]""");
        await CreateProfileFactAsync(client, "Frontend delivery", "Approved", """["React"]""", "Structured frontend delivery evidence.");
        await CreateProfileFactAsync(client, "SQL metadata only", "Approved", """[]""", "Broad metadata-only evidence.");
        var application = await CreateApplicationAsync(client, "We need .NET, React, SQL, and Kubernetes.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var prepared = await response.Content.ReadFromJsonAsync<PrepareApplicationResponse>();
        Assert.NotNull(prepared);
        Assert.Equal("[]", prepared.Application.ApprovedEvidence);

        var evidenceMatches = JsonSerializer.Deserialize<List<EvidenceMatch>>(prepared.Application.EvidenceMatches, JsonOptions);
        var unmatchedRequirements = JsonSerializer.Deserialize<List<UnmatchedRequirement>>(prepared.Application.UnmatchedRequirements, JsonOptions);
        Assert.NotNull(evidenceMatches);
        Assert.NotNull(unmatchedRequirements);
        var strong = Assert.Single(evidenceMatches, match => match.Signal == ".NET");
        var partial = Assert.Single(evidenceMatches, match => match.Signal == "React");
        var weak = Assert.Single(evidenceMatches, match => match.Signal == "SQL");
        Assert.Equal(EvidenceQuality.Strong, strong.Quality);
        Assert.Equal(EvidenceQuality.Partial, partial.Quality);
        Assert.Equal(EvidenceQuality.Weak, weak.Quality);
        Assert.False(string.IsNullOrWhiteSpace(strong.Reason));
        Assert.False(string.IsNullOrWhiteSpace(partial.Reason));
        Assert.False(string.IsNullOrWhiteSpace(weak.Reason));
        Assert.Contains(unmatchedRequirements, requirement => requirement.Requirement == "Kubernetes");

        var reopenedResponse = await client.GetAsync($"/api/applications/{application.Id}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened);
        var reopenedMatches = JsonSerializer.Deserialize<List<EvidenceMatch>>(reopened.EvidenceMatches, JsonOptions);
        Assert.NotNull(reopenedMatches);
        Assert.Contains(reopenedMatches, match =>
            match.Id == weak.Id &&
            match.Quality == EvidenceQuality.Weak &&
            !string.IsNullOrWhiteSpace(match.Reason));

        var weakApprovalResponse = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest(JsonSerializer.Serialize(new[] { new { weak.Id } }, JsonOptions)));
        Assert.Equal(HttpStatusCode.BadRequest, weakApprovalResponse.StatusCode);

        var reviewedResponse = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest(JsonSerializer.Serialize(new[] { new { strong.Id }, new { partial.Id } }, JsonOptions)));
        reviewedResponse.EnsureSuccessStatusCode();
        var reviewed = await reviewedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reviewed);
        var approvedEvidence = JsonSerializer.Deserialize<List<EvidenceMatch>>(reviewed.ApprovedEvidence, JsonOptions);
        Assert.NotNull(approvedEvidence);
        Assert.Equal(2, approvedEvidence.Count);
        Assert.DoesNotContain(approvedEvidence, match => match.Quality == EvidenceQuality.Weak);
        Assert.Contains(approvedEvidence, match => match.Id == strong.Id && match.Quality == EvidenceQuality.Strong);
        Assert.Contains(approvedEvidence, match => match.Id == partial.Id && match.Quality == EvidenceQuality.Partial);
    }

    [Fact]
    public async Task PrepareApplication_uses_approved_imported_facts_in_candidate_fit_brief()
    {
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs for internal workflow automation."));
        });
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent([1, 2, 3]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "cv.pdf");
        var importResponse = await client.PostAsync("/api/profile/imports/pdf-cv", form);
        importResponse.EnsureSuccessStatusCode();
        var import = await importResponse.Content.ReadFromJsonAsync<AssistedProfileImportResponse>();
        Assert.NotNull(import);
        var importedFact = import.ProfileFacts.First();
        var approveResponse = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/{importedFact.Id:N}/decision",
            new ImportedDraftFactDecisionRequest("approve"));
        approveResponse.EnsureSuccessStatusCode();
        var application = await CreateApplicationAsync(client, "We need .NET delivery.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var prepared = await response.Content.ReadFromJsonAsync<PrepareApplicationResponse>();
        Assert.NotNull(prepared);
        var candidateFitBrief = JsonSerializer.Deserialize<CandidateFitBriefResult>(prepared.Application.CandidateFitBrief, JsonOptions);
        Assert.NotNull(candidateFitBrief);
        Assert.Contains(importedFact.Title, prepared.Application.CandidateFitBrief);
        Assert.Contains(importedFact.Id.ToString(), prepared.Application.CandidateFitBrief);
    }

    [Fact]
    public async Task PrepareApplication_candidate_fit_brief_receives_all_approved_profile_facts()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var nonMatchingApprovedFact = await CreateProfileFactAsync(client, "Approved COBOL migration", "Approved", """["COBOL"]""");
        await CreateProfileFactAsync(client, "Draft Kubernetes work", "Draft", """["Kubernetes"]""");
        var application = await CreateApplicationAsync(client, "We need .NET delivery.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var prepared = await response.Content.ReadFromJsonAsync<PrepareApplicationResponse>();
        Assert.NotNull(prepared);
        Assert.Contains(nonMatchingApprovedFact.Title, prepared.Application.CandidateFitBrief);
        Assert.Contains(nonMatchingApprovedFact.Id.ToString(), prepared.Application.CandidateFitBrief);
        Assert.DoesNotContain("Draft Kubernetes work", prepared.Application.CandidateFitBrief);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns.Where(run => run.Step == "CandidateFitBrief"));
        Assert.Equal("Succeeded", run.Status);
        Assert.Contains("\"approvedFactCount\":2", run.InputSummary);
    }

    [Fact]
    public async Task PrepareApplication_with_unavailable_provider_marks_failure_without_mutating_workflow_state()
    {
        await using var factory = new TestApplicationFactory()
            .WithOllamaResponses([new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "Service Unavailable"
            }]);
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("AiProvider", error.Details!.Keys);
        Assert.Contains("unavailable", error.Details["AiProvider"][0], StringComparison.OrdinalIgnoreCase);

        var reopenedResponse = await client.GetAsync($"/api/applications/{application.Id}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened);
        Assert.Equal("Fallback Company", reopened.CompanyName);
        Assert.Equal("Fallback Role", reopened.RoleTitle);
        Assert.Equal("Draft", reopened.Status);
        Assert.Equal("{}", reopened.JobSignals);
        Assert.Equal("FailedProviderUnavailable", reopened.PreparationStatus);
        Assert.Null(reopened.LastPreparedAt);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("JobAnalysis", run.Step);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Equal("Ollama", run.Provider);
        Assert.Equal("llama3.1:8b", run.Model);
        Assert.Equal(1, run.AttemptCount);
        Assert.NotNull(run.CompletedAt);
    }

    [Fact]
    public async Task PrepareApplication_with_invalid_analysis_output_marks_failure_without_mutating_workflow_state()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("""{"companyName":"","roleTitle":""}""") }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("AiProvider", error.Details!.Keys);
        Assert.Contains("invalid", error.Details["AiProvider"][0], StringComparison.OrdinalIgnoreCase);

        var reopenedResponse = await client.GetAsync($"/api/applications/{application.Id}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened);
        Assert.Equal("Fallback Company", reopened.CompanyName);
        Assert.Equal("Fallback Role", reopened.RoleTitle);
        Assert.Equal("Draft", reopened.Status);
        Assert.Equal("{}", reopened.JobSignals);
        Assert.Equal("FailedInvalidProviderOutput", reopened.PreparationStatus);
        Assert.Null(reopened.LastPreparedAt);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("JobAnalysis", run.Step);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("InvalidOutput", run.ErrorCode);
        Assert.Equal("Ollama", run.Provider);
        Assert.Equal("llama3.1:8b", run.Model);
        Assert.Equal(2, run.AttemptCount);
        Assert.NotNull(run.CompletedAt);
    }

    [Fact]
    public async Task PrepareApplication_with_unavailable_candidate_fit_brief_provider_returns_error_and_preserves_stable_state()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaAnalysisJson("Contoso", "Platform Engineer")) },
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { ReasonPhrase = "Service Unavailable" }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        var stablePreparedAt = await SetPreparedApplicationStateAsync(factory, application.Id);
        var existingDraft = await AddGeneratedDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("AiProvider", error.Details!.Keys);
        Assert.Contains("unavailable", error.Details["AiProvider"][0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Preparation", error.Details.Keys);
        Assert.Contains("candidate fit brief generation failed", error.Details["Preparation"][0]);

        var reopened = await GetApplicationAsync(client, application.Id);
        AssertStablePreparedState(reopened, stablePreparedAt, existingDraft.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var runs = db.AiRuns.OrderBy(run => run.StartedAt).ToList();
        Assert.Equal(2, runs.Count);
        Assert.Equal("JobAnalysis", runs[0].Step);
        Assert.Equal("Succeeded", runs[0].Status);
        Assert.Equal("CandidateFitBrief", runs[1].Step);
        Assert.Equal("Failed", runs[1].Status);
        Assert.Equal("ProviderUnavailable", runs[1].ErrorCode);
        Assert.NotNull(runs[1].CompletedAt);
    }

    [Fact]
    public async Task PrepareApplication_with_invalid_candidate_fit_brief_output_returns_validation_error_and_preserves_stable_state()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaAnalysisJson("Contoso", "Platform Engineer")) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("""{"candidateSummary":""}""") }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        var stablePreparedAt = await SetPreparedApplicationStateAsync(factory, application.Id);
        var existingDraft = await AddGeneratedDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("AiProvider", error.Details!.Keys);
        Assert.Contains("invalid", error.Details["AiProvider"][0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Preparation", error.Details.Keys);
        Assert.Contains("candidate fit brief generation failed", error.Details["Preparation"][0]);

        var reopened = await GetApplicationAsync(client, application.Id);
        AssertStablePreparedState(reopened, stablePreparedAt, existingDraft.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var runs = db.AiRuns.OrderBy(run => run.StartedAt).ToList();
        Assert.Equal(2, runs.Count);
        Assert.Equal("JobAnalysis", runs[0].Step);
        Assert.Equal("Succeeded", runs[0].Status);
        Assert.Equal("CandidateFitBrief", runs[1].Step);
        Assert.Equal("Failed", runs[1].Status);
        Assert.Equal("InvalidOutput", runs[1].ErrorCode);
        Assert.Equal(2, runs[1].AttemptCount);
        Assert.NotNull(runs[1].CompletedAt);
    }

    [Fact]
    public async Task PrepareApplication_with_matching_failure_preserves_analysis_and_marks_partial_state()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaAnalysisJson("Contoso", "Platform Engineer")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var approvedFact = await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidCandidateFitBriefJson(approvedFact.Id)) });
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { ReasonPhrase = "Service Unavailable" });
        await SetEvidenceReviewStateAsync(
            factory,
            application.Id,
            """[{"id":"existing-match","signalId":"legacy","profileFactId":"00000000-0000-0000-0000-000000000001","summary":"Existing match","matchedTerms":["Legacy"]}]""",
            """[{"id":"existing-unmatched","signalId":"legacy-gap","requirement":"Legacy gap","recommendation":"Existing recommendation"}]""",
            """[{"id":"existing-approved"}]""");
        var existingDraft = await AddGeneratedDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("AiProvider", error.Details!.Keys);
        Assert.Contains("Preparation", error.Details.Keys);
        Assert.Contains("Retry preparation before reviewing evidence.", error.Details["Preparation"][0]);

        var reopenedResponse = await client.GetAsync($"/api/applications/{application.Id}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened);
        Assert.Equal("Contoso", reopened.CompanyName);
        Assert.Equal("Platform Engineer", reopened.RoleTitle);
        Assert.Equal("PostingCaptured", reopened.Status);
        Assert.Equal("PartiallyPreparedAnalysisOnly", reopened.PreparationStatus);
        Assert.Null(reopened.LastPreparedAt);

        var signals = JsonSerializer.Deserialize<JobSignalsDocument>(reopened.JobSignals, JsonOptions);
        Assert.NotNull(signals);
        Assert.Contains(".NET", signals.RequiredSkills);
        Assert.Contains("existing-match", reopened.EvidenceMatches);
        Assert.Contains("existing-unmatched", reopened.UnmatchedRequirements);
        Assert.Contains("existing-approved", reopened.ApprovedEvidence);
        Assert.NotNull(reopened.GeneratedDraft);
        Assert.Equal(existingDraft.Id, reopened.GeneratedDraft.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var runs = db.AiRuns.OrderBy(run => run.StartedAt).ToList();
        Assert.Equal(3, runs.Count);
        Assert.Equal("JobAnalysis", runs[0].Step);
        Assert.Equal("Succeeded", runs[0].Status);
        Assert.Null(runs[0].ErrorCode);
        Assert.NotNull(runs[0].CompletedAt);
        Assert.Equal("CandidateFitBrief", runs[1].Step);
        Assert.Equal("Succeeded", runs[1].Status);
        Assert.NotNull(runs[1].CompletedAt);
        Assert.Equal("EvidenceMatching", runs[2].Step);
        Assert.Equal("Failed", runs[2].Status);
        Assert.Equal("ProviderUnavailable", runs[2].ErrorCode);
        Assert.NotNull(runs[2].CompletedAt);
    }

    [Fact]
    public async Task MatchEvidence_uses_only_approved_profile_facts_and_persists_unmatched_requirements()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        await CreateProfileFactAsync(client, "Draft Kubernetes work", "Draft", """["Kubernetes"]""");
        await CreateProfileFactAsync(client, "Archived React work", "Archived", """["React"]""");
        await CreateProfileFactAsync(client, "Rejected Azure work", "Rejected", """["Azure"]""");
        var application = await CreateApplicationAsync(client, "We need .NET, React, Kubernetes, and Azure.");
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
        Assert.Contains(unmatchedRequirements, requirement => requirement.Requirement == "Azure");
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
                      "quality": "Strong",
                      "reason": ".NET is directly supported by approved API work.",
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
                      "quality": "Strong",
                      "reason": "Invalid signal should still be rejected.",
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
    public async Task MatchEvidence_with_openai_compatible_stores_matches_unmatched_requirements_and_records_successful_ai_run()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>());
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var approvedFact = await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        await CreateProfileFactAsync(client, "Draft Kubernetes work", "Draft", """["Kubernetes"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OpenAiChatCompletionContent(ValidOllamaEvidenceMatchingJson(approvedFact.Id))
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

        var requestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("chat/completions", handler.Requests[0].RequestUri!.ToString());
        Assert.Contains("\"model\":\"local-model\"", requestJson);
        Assert.Contains("\"response_format\":{\"type\":\"text\"}", requestJson);
        Assert.Contains("Return only strict JSON", requestJson);
        Assert.Contains(approvedFact.Id.ToString(), requestJson);
        Assert.DoesNotContain("Draft Kubernetes work", requestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal(application.Id, run.JobApplicationId);
        Assert.Equal("EvidenceMatching", run.Step);
        Assert.Equal("OpenAiCompatible", run.Provider);
        Assert.Equal("local-model", run.Model);
        Assert.Equal("Succeeded", run.Status);
        Assert.Equal(1, run.AttemptCount);
        Assert.Null(run.ErrorCode);
        Assert.Contains("\"matchCount\":1", run.OutputSummary);
    }

    [Fact]
    public async Task MatchEvidence_with_openai_compatible_repairs_incomplete_signal_coverage_once()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>());
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var approvedFact = await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OpenAiChatCompletionContent($$"""
                {
                  "evidenceMatches": [
                    {
                      "signalId": "dotnet",
                      "profileFactId": "{{approvedFact.Id}}",
                      "summary": "Approved API work demonstrates .NET experience.",
                      "quality": "Strong",
                      "reason": ".NET is directly supported by approved API work.",
                      "matchedTerms": [".NET"]
                    }
                  ],
                  "unmatchedRequirements": []
                }
                """)
        });
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OpenAiChatCompletionContent(ValidOllamaEvidenceMatchingJson(approvedFact.Id))
        });

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);

        var repairRequestJson = await handler.Requests[1].Content!.ReadAsStringAsync();
        Assert.Contains("Repair this evidence matching JSON", repairRequestJson);
        Assert.Contains("Every job signal id listed below must appear exactly once", repairRequestJson);
        Assert.Contains("kubernetes", repairRequestJson);
        Assert.Contains(approvedFact.Id.ToString(), repairRequestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task MatchEvidence_with_openai_compatible_invalid_output_after_repair_records_raw_payload_and_does_not_mutate_application()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>());
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            storeRawPayloads: true);
        using var client = factory.CreateClient();
        var approvedFact = await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("{ malformed") });
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OpenAiChatCompletionContent($$"""
                {
                  "evidenceMatches": [
                    {
                      "signalId": "dotnet",
                      "profileFactId": "{{approvedFact.Id}}",
                      "summary": "First match.",
                      "quality": "Strong",
                      "reason": ".NET is directly supported by approved API work.",
                      "matchedTerms": [".NET"]
                    },
                    {
                      "signalId": "dotnet",
                      "profileFactId": "{{approvedFact.Id}}",
                      "summary": "Duplicate match.",
                      "quality": "Strong",
                      "reason": ".NET is directly supported by approved API work.",
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
        Assert.Contains("Duplicate match", run.ErrorMessage);
    }

    [Fact]
    public async Task MatchEvidence_with_openai_compatible_conflicting_output_after_repair_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>());
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            storeRawPayloads: true);
        using var client = factory.CreateClient();
        var approvedFact = await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("{ malformed") });
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OpenAiChatCompletionContent($$"""
                {
                  "evidenceMatches": [
                    {
                      "signalId": "dotnet",
                      "profileFactId": "{{approvedFact.Id}}",
                      "summary": "Approved API work demonstrates .NET experience.",
                      "quality": "Strong",
                      "reason": ".NET is directly supported by approved API work.",
                      "matchedTerms": [".NET"]
                    }
                  ],
                  "unmatchedRequirements": [
                    {
                      "signalId": "dotnet",
                      "recommendation": ".NET should not also be unmatched."
                    },
                    {
                      "signalId": "kubernetes",
                      "recommendation": "Treat Kubernetes as an honest learning area."
                    }
                  ]
                }
                """)
        });

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("InvalidOutput", run.ErrorCode);
        Assert.Equal(2, run.AttemptCount);
        Assert.Contains("conflicting evidence matching JSON", run.ErrorMessage);
        Assert.Contains(".NET should not also be unmatched", run.ErrorMessage);
    }

    [Fact]
    public async Task MatchEvidence_with_openai_compatible_api_error_records_raw_payload_when_enabled()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "Service Unavailable",
                Content = JsonContent.Create(new { error = new { message = "model unavailable" } })
            }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            storeRawPayloads: true);
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Contains("model unavailable", run.ErrorMessage);
    }

    [Fact]
    public async Task MatchEvidence_with_openai_compatible_timeout_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(
            new Queue<HttpResponseMessage>(),
            new TaskCanceledException("timed out"));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Equal(1, run.AttemptCount);
    }

    [Fact]
    public async Task MatchEvidence_with_openai_compatible_unreachable_endpoint_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(
            new Queue<HttpResponseMessage>(),
            new HttpRequestException("unreachable"));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await MarkApplicationAnalyzedAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Equal(1, run.AttemptCount);
    }

    [Fact]
    public async Task PrepareApplication_with_openai_compatible_runs_analysis_and_matching_and_returns_evidence_review_checkpoint()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = OpenAiChatCompletionContent(ValidOllamaAnalysisJson("Contoso", "Platform Engineer"))
            }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var approvedFact = await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OpenAiChatCompletionContent(ValidCandidateFitBriefJson(approvedFact.Id))
        });
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OpenAiChatCompletionContent(ValidOpenAiPrepareEvidenceMatchingJson(approvedFact.Id))
        });

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var prepared = await response.Content.ReadFromJsonAsync<PrepareApplicationResponse>();
        Assert.NotNull(prepared);
        Assert.Equal("ReviewEvidence", prepared.NextCheckpoint);
        Assert.Equal("PreparedForEvidenceReview", prepared.Application.Status);
        Assert.Equal("PreparedForEvidenceReview", prepared.Application.PreparationStatus);
        Assert.NotNull(prepared.Application.LastPreparedAt);

        var signals = JsonSerializer.Deserialize<JobSignalsDocument>(prepared.Application.JobSignals, JsonOptions);
        var evidenceMatches = JsonSerializer.Deserialize<List<EvidenceMatch>>(prepared.Application.EvidenceMatches, JsonOptions);
        var unmatchedRequirements = JsonSerializer.Deserialize<List<UnmatchedRequirement>>(prepared.Application.UnmatchedRequirements, JsonOptions);
        Assert.NotNull(signals);
        Assert.NotNull(evidenceMatches);
        Assert.NotNull(unmatchedRequirements);
        Assert.Equal("OpenAiCompatible", signals.Provider);
        Assert.Single(evidenceMatches);
        Assert.Equal(3, unmatchedRequirements.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var runs = db.AiRuns.OrderBy(run => run.StartedAt).ToList();
        Assert.Equal(3, runs.Count);
        Assert.All(runs, run => Assert.Equal("OpenAiCompatible", run.Provider));
        Assert.Equal("JobAnalysis", runs[0].Step);
        Assert.Equal("CandidateFitBrief", runs[1].Step);
        Assert.Equal("EvidenceMatching", runs[2].Step);
        Assert.All(runs, run => Assert.Equal("Succeeded", run.Status));
    }

    [Fact]
    public async Task PrepareApplication_with_openai_compatible_matching_failure_preserves_analysis_and_records_raw_payload()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = OpenAiChatCompletionContent(ValidOllamaAnalysisJson("Contoso", "Platform Engineer"))
            }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            storeRawPayloads: true);
        using var client = factory.CreateClient();
        var approvedFact = await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = OpenAiChatCompletionContent(ValidCandidateFitBriefJson(approvedFact.Id))
        });
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            ReasonPhrase = "Service Unavailable",
            Content = JsonContent.Create(new { error = new { message = "matching unavailable" } })
        });
        await SetEvidenceReviewStateAsync(
            factory,
            application.Id,
            """[{"id":"existing-match","signalId":"legacy","profileFactId":"00000000-0000-0000-0000-000000000001","summary":"Existing match","matchedTerms":["Legacy"]}]""",
            """[{"id":"existing-unmatched","signalId":"legacy-gap","requirement":"Legacy gap","recommendation":"Existing recommendation"}]""",
            """[{"id":"existing-approved"}]""");

        var response = await client.PostAsync($"/api/applications/{application.Id}/prepare", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var reopenedResponse = await client.GetAsync($"/api/applications/{application.Id}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened);
        Assert.Equal("Contoso", reopened.CompanyName);
        Assert.Equal("Platform Engineer", reopened.RoleTitle);
        Assert.Equal("PostingCaptured", reopened.Status);
        Assert.Equal("PartiallyPreparedAnalysisOnly", reopened.PreparationStatus);
        Assert.Null(reopened.LastPreparedAt);
        Assert.Contains("existing-match", reopened.EvidenceMatches);
        Assert.Contains("existing-unmatched", reopened.UnmatchedRequirements);
        Assert.Contains("existing-approved", reopened.ApprovedEvidence);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var runs = db.AiRuns.OrderBy(run => run.StartedAt).ToList();
        Assert.Equal(3, runs.Count);
        Assert.Equal("Succeeded", runs[0].Status);
        Assert.Equal("Succeeded", runs[1].Status);
        Assert.Equal("CandidateFitBrief", runs[1].Step);
        Assert.Equal("Failed", runs[2].Status);
        Assert.Equal("EvidenceMatching", runs[2].Step);
        Assert.Equal("ProviderUnavailable", runs[2].ErrorCode);
        Assert.Contains("matching unavailable", runs[2].ErrorMessage);
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
    public async Task PutApprovedEvidence_rejects_candidate_fit_brief_profile_fact_ids()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var traceOnlyProfileFactId = Guid.NewGuid();
        var application = await CreateApplicationAsync(client, "We need .NET.");
        await SetCandidateFitBriefAsync(factory, application.Id, traceOnlyProfileFactId);

        var response = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest(JsonSerializer.Serialize(
                new[] { new { id = traceOnlyProfileFactId.ToString() } },
                JsonOptions)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApprovedEvidenceRequest.ApprovedEvidence), error.Details!.Keys);
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
    public async Task PutGapDecisions_persists_current_unmatched_requirement_decisions()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await SetEvidenceReviewStateAsync(
            factory,
            application.Id,
            "[]",
            JsonSerializer.Serialize(
                new[]
                {
                    new UnmatchedRequirement(
                        "unmatched-dotnet",
                        "dotnet",
                        ".NET",
                        "RequiredSkill",
                        "Ignore if not relevant."),
                    new UnmatchedRequirement(
                        "unmatched-kubernetes",
                        "kubernetes",
                        "Kubernetes",
                        "PreferredSkill",
                        "Mention as learning interest.")
                },
                JsonOptions),
            "[]");

        var response = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/gap-decisions",
            new GapDecisionsRequest(
                """
                [
                  {"unmatchedRequirementId":"unmatched-dotnet","decision":"Ignore"},
                  {"unmatchedRequirementId":"unmatched-kubernetes","decision":"Ignore"},
                  {"unmatchedRequirementId":"unmatched-kubernetes","decision":"MentionAsLearningInterest"}
                ]
                """));

        response.EnsureSuccessStatusCode();
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/gap-decisions",
            new GapDecisionsRequest(
                """
                [
                  {"unmatchedRequirementId":"unmatched-dotnet","decision":"MentionAsLearningInterest"},
                  {"unmatchedRequirementId":"unmatched-kubernetes","decision":"Ignore"}
                ]
                """));
        updateResponse.EnsureSuccessStatusCode();
        var reopened = await client.GetFromJsonAsync<ApplicationResponse>($"/api/applications/{application.Id}");
        Assert.NotNull(reopened);
        var reviewed = reopened;
        Assert.NotNull(reviewed);
        var decisions = JsonSerializer.Deserialize<List<GapDecisionTestItem>>(reviewed.GapDecisions, JsonOptions);
        Assert.NotNull(decisions);
        Assert.Equal(2, decisions.Count);
        Assert.Contains(decisions, decision =>
            decision.UnmatchedRequirementId == "unmatched-dotnet" &&
            decision.Decision == "MentionAsLearningInterest");
        Assert.Contains(decisions, decision =>
            decision.UnmatchedRequirementId == "unmatched-kubernetes" &&
            decision.Decision == "Ignore");
    }

    [Fact]
    public async Task PutGapDecisions_rejects_unknown_or_invalid_decisions()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var unknownIdResponse = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/gap-decisions",
            new GapDecisionsRequest("""[{"unmatchedRequirementId":"missing","decision":"Ignore"}]"""));
        var invalidDecisionResponse = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/gap-decisions",
            new GapDecisionsRequest("""[{"unmatchedRequirementId":"missing","decision":"CoveredByCustomFact"}]"""));
        var malformedResponse = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/gap-decisions",
            new GapDecisionsRequest("""{"unmatchedRequirementId":"missing","decision":"Ignore"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, unknownIdResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidDecisionResponse.StatusCode);
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
        await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/gap-decisions",
            new GapDecisionsRequest("""[{"unmatchedRequirementId":"unmatched-kubernetes","decision":"MentionAsLearningInterest"}]"""));

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
        Assert.NotNull(draft.AuditUpdatedAt);
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
    public async Task GenerateDraft_rejects_candidate_fit_brief_profile_fact_ids_without_approved_evidence()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var traceOnlyProfileFactId = Guid.NewGuid();
        var application = await CreateApplicationAsync(client, "We need .NET.");
        await SetCandidateFitBriefAsync(factory, application.Id, traceOnlyProfileFactId);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationResponse.ApprovedEvidence), error.Details!.Keys);
    }

    [Fact]
    public async Task GenerateDraft_rejects_application_with_unhandled_unmatched_requirements()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await db.JobApplications.FindAsync(application.Id);
            Assert.NotNull(stored);
            stored.ApprovedEvidence = JsonSerializer.Serialize(
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
            stored.UnmatchedRequirements = JsonSerializer.Serialize(
                new[]
                {
                    new UnmatchedRequirement(
                        "unmatched-kubernetes",
                        "kubernetes",
                        "Kubernetes",
                        "PreferredSkill",
                        "Decide how to handle Kubernetes.")
                },
                JsonOptions);
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationResponse.GapDecisions), error.Details.Keys);
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
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaDraftGenerationJson()) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
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
        Assert.NotNull(draft.AuditUpdatedAt);

        Assert.Equal(2, handler.Requests.Count);
        var ollamaRequestJson = await handler.Requests[0].Content!.ReadAsStringAsync();
        Assert.Contains("\"format\":\"json\"", ollamaRequestJson);
        Assert.Contains("Return only strict JSON", ollamaRequestJson);
        Assert.Contains("Approved API work", ollamaRequestJson);
        Assert.Contains("honest learning area", ollamaRequestJson);
        var auditRequestJson = await handler.Requests[1].Content!.ReadAsStringAsync();
        Assert.Contains("Ollama cover letter from approved API evidence.", auditRequestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var runs = db.AiRuns.ToList();
        var draftRun = Assert.Single(runs, run => run.Step == "DraftGeneration");
        Assert.Equal("Ollama", draftRun.Provider);
        Assert.Equal("Succeeded", draftRun.Status);
        Assert.Equal(1, draftRun.AttemptCount);
        Assert.Null(draftRun.ErrorCode);
        var auditRun = Assert.Single(runs, run => run.Step == "ClaimAudit");
        Assert.Equal("Succeeded", auditRun.Status);
        Assert.Equal(1, auditRun.AttemptCount);
    }

    [Fact]
    public async Task GenerateDraft_with_ollama_does_not_send_candidate_fit_brief_profile_fact_ids_to_draft_or_audit()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaDraftGenerationJson()) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.", "English");
        var traceOnlyProfileFactId = Guid.NewGuid();
        await SetCandidateFitBriefAsync(factory, application.Id, traceOnlyProfileFactId);
        await MarkApplicationReadyForDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);
        var draftRequestJson = await handler.Requests[0].Content!.ReadAsStringAsync();
        var auditRequestJson = await handler.Requests[1].Content!.ReadAsStringAsync();
        Assert.Contains("match-dotnet-test", draftRequestJson);
        Assert.Contains("match-dotnet-test", auditRequestJson);
        Assert.DoesNotContain(traceOnlyProfileFactId.ToString(), draftRequestJson);
        Assert.DoesNotContain(traceOnlyProfileFactId.ToString(), auditRequestJson);
    }

    [Fact]
    public async Task GenerateDraft_with_ollama_sends_gap_decisions_and_only_approved_custom_facts()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaDraftGenerationJson()) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET, Azure, Docker, and Kubernetes.", "English");
        var approvedCustomFactId = Guid.NewGuid();
        var pendingCustomFactId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await db.JobApplications.FindAsync(application.Id);
            Assert.NotNull(stored);
            stored.ApprovedEvidence = JsonSerializer.Serialize(
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
            stored.UnmatchedRequirements = JsonSerializer.Serialize(
                new[]
                {
                    new UnmatchedRequirement("unmatched-kubernetes", "kubernetes", "Kubernetes", "PreferredSkill", "Ignore unsupported Kubernetes."),
                    new UnmatchedRequirement("unmatched-azure", "azure", "Azure", "PreferredSkill", "Mention Azure as learning interest."),
                    new UnmatchedRequirement("unmatched-docker", "docker", "Docker", "PreferredSkill", "Covered by approved custom fact."),
                    new UnmatchedRequirement("unmatched-react", "react", "React", "RequiredSkill", "Pending custom fact is not approved.")
                },
                JsonOptions);
            stored.GapDecisions = JsonSerializer.Serialize(
                new[]
                {
                    new GapDecisionTestItem("unmatched-kubernetes", "Ignore", null),
                    new GapDecisionTestItem("unmatched-azure", "MentionAsLearningInterest", null),
                    new GapDecisionTestItem("unmatched-docker", "CoveredByCustomFact", approvedCustomFactId),
                    new GapDecisionTestItem("unmatched-react", "CoveredByCustomFact", pendingCustomFactId)
                },
                JsonOptions);
            stored.CustomFacts = JsonSerializer.Serialize(
                new[]
                {
                    new CustomFactTestItem(
                        approvedCustomFactId,
                        "unmatched-docker",
                        "Approved Docker deployment",
                        "Shipped a Docker-based deployment for a client.",
                        ["Docker"],
                        ["Shipped Docker deployment"],
                        "Approved",
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow),
                    new CustomFactTestItem(
                        pendingCustomFactId,
                        "unmatched-react",
                        "Pending React claim",
                        "Pending React work should not be supplied.",
                        ["React"],
                        ["Built React UI"],
                        "PendingConfirmation",
                        DateTimeOffset.UtcNow,
                        null)
                },
                JsonOptions);
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);
        var ollamaRequestJson = await handler.Requests[0].Content!.ReadAsStringAsync();
        Assert.Contains("Ignore", ollamaRequestJson);
        Assert.Contains("MentionAsLearningInterest", ollamaRequestJson);
        Assert.Contains("CoveredByCustomFact", ollamaRequestJson);
        Assert.DoesNotContain("Ignore unsupported Kubernetes.", ollamaRequestJson);
        Assert.DoesNotContain(pendingCustomFactId.ToString(), ollamaRequestJson);
        Assert.Contains("Approved Docker deployment", ollamaRequestJson);
        Assert.Contains("Shipped a Docker-based deployment for a client.", ollamaRequestJson);
        Assert.DoesNotContain("Covered by approved custom fact.", ollamaRequestJson);
        Assert.DoesNotContain("Pending React claim", ollamaRequestJson);
        Assert.DoesNotContain("Pending React work should not be supplied.", ollamaRequestJson);

        using var runScope = factory.Services.CreateScope();
        var runDb = runScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(runDb.AiRuns.Where(run => run.Step == "DraftGeneration"));
        Assert.Contains("\"unmatchedRequirementCount\":1", run.InputSummary);
        Assert.Contains("\"gapDecisionCount\":3", run.InputSummary);
        Assert.Contains("\"approvedCustomFactCount\":1", run.InputSummary);
    }

    [Fact]
    public async Task GenerateDraft_with_ollama_repairs_structurally_invalid_json_once()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("""{"coverLetterText":"","shortMotivationText":""}""") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaDraftGenerationJson()) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.", "English");
        await MarkApplicationReadyForDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(3, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns.Where(run => run.Step == "DraftGeneration"));
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task GenerateDraft_with_ollama_repairs_malformed_json_once()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaDraftGenerationJson()) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.", "English");
        await MarkApplicationReadyForDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(3, handler.Requests.Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns.Where(run => run.Step == "DraftGeneration"));
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
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaDraftGenerationJson()) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
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
        Assert.NotEqual("{}", draft.ClaimAudit);
        Assert.NotNull(draft.AuditUpdatedAt);
    }

    [Fact]
    public async Task GenerateDraft_with_ollama_audit_failure_preserves_generated_draft_and_records_failed_audit()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaDraftGenerationJson()) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent("""{"claims":[{"id":"claim-1","text":"Broken","status":"Maybe","evidenceIds":[]}]}""") }
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
        Assert.Equal("Ollama cover letter from approved API evidence.", draft.CoverLetterText);
        Assert.Equal("Ollama short motivation.", draft.ShortMotivationText);
        Assert.Equal("{}", draft.ClaimAudit);
        Assert.Null(draft.AuditUpdatedAt);
        Assert.False(draft.IsClaimAuditStale);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var draftRun = Assert.Single(db.AiRuns.Where(run => run.Step == "DraftGeneration"));
        Assert.Equal("Succeeded", draftRun.Status);
        var auditRun = Assert.Single(db.AiRuns.Where(run => run.Step == "ClaimAudit"));
        Assert.Equal("Failed", auditRun.Status);
        Assert.Equal("InvalidOutput", auditRun.ErrorCode);
        Assert.Equal(2, auditRun.AttemptCount);
    }

    [Fact]
    public async Task GenerateDraft_with_openai_compatible_persists_current_draft_audit_and_records_successful_ai_runs()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidOllamaDraftGenerationJson()) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.", "English");
        await MarkApplicationReadyForDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var draft = await response.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(draft);
        Assert.Equal("Ollama cover letter from approved API evidence.", draft.CoverLetterText);
        Assert.Equal("Ollama short motivation.", draft.ShortMotivationText);
        Assert.NotNull(draft.AuditUpdatedAt);
        Assert.False(draft.IsClaimAuditStale);

        Assert.Equal(2, handler.Requests.Count);
        var draftRequestJson = await handler.Requests[0].Content!.ReadAsStringAsync();
        Assert.Contains("\"model\":\"local-model\"", draftRequestJson);
        Assert.Contains("\"response_format\":{\"type\":\"text\"}", draftRequestJson);
        Assert.Contains("Return only strict JSON", draftRequestJson);
        Assert.Contains("Approved API work", draftRequestJson);
        var auditRequestJson = await handler.Requests[1].Content!.ReadAsStringAsync();
        Assert.Contains("Ollama cover letter from approved API evidence.", auditRequestJson);
        Assert.Contains("match-dotnet-test", auditRequestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var runs = db.AiRuns.ToList();
        var draftRun = Assert.Single(runs, run => run.Step == "DraftGeneration");
        Assert.Equal("OpenAiCompatible", draftRun.Provider);
        Assert.Equal("local-model", draftRun.Model);
        Assert.Equal("Succeeded", draftRun.Status);
        Assert.Equal(1, draftRun.AttemptCount);
        var auditRun = Assert.Single(runs, run => run.Step == "ClaimAudit");
        Assert.Equal("Succeeded", auditRun.Status);
        Assert.Equal(1, auditRun.AttemptCount);
    }

    [Fact]
    public async Task GenerateDraft_with_openai_compatible_repairs_structurally_invalid_json_once()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("""{"coverLetterText":"","shortMotivationText":""}""") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidOllamaDraftGenerationJson()) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.", "English");
        await MarkApplicationReadyForDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(3, handler.Requests.Count);
        var repairRequestJson = await handler.Requests[1].Content!.ReadAsStringAsync();
        Assert.Contains("Repair this draft generation JSON", repairRequestJson);
        Assert.Contains("structurally invalid draft generation JSON", repairRequestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns.Where(run => run.Step == "DraftGeneration"));
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task GenerateDraft_with_openai_compatible_invalid_output_after_repair_records_failure_raw_payload_and_preserves_draft()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("""{"coverLetterText":"","shortMotivationText":""}""") }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            storeRawPayloads: true);
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.", "English");
        await MarkApplicationReadyForDraftAsync(factory, application.Id);
        var existingDraft = await AddGeneratedDraftAsync(factory, application.Id);

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var reopenedResponse = await client.GetAsync($"/api/applications/{application.Id}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened?.GeneratedDraft);
        Assert.Equal(existingDraft.Id, reopened.GeneratedDraft.Id);
        Assert.Equal(existingDraft.CoverLetterText, reopened.GeneratedDraft.CoverLetterText);
        Assert.Equal(existingDraft.ClaimAudit, reopened.GeneratedDraft.ClaimAudit);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("InvalidOutput", run.ErrorCode);
        Assert.Equal(2, run.AttemptCount);
        Assert.Contains("Raw payload:", run.ErrorMessage);
        Assert.Contains("coverLetterText", run.ErrorMessage);
    }

    [Fact]
    public async Task GenerateDraft_with_openai_compatible_api_error_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { ReasonPhrase = "Service Unavailable" }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
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
        Assert.Equal("OpenAiCompatible", run.Provider);
        Assert.Equal("local-model", run.Model);
        Assert.Equal(1, run.AttemptCount);
        Assert.DoesNotContain("Raw request:", run.ErrorMessage);
        Assert.DoesNotContain("Raw response:", run.ErrorMessage);
    }

    [Fact]
    public async Task GenerateDraft_with_unreachable_openai_compatible_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(
            new Queue<HttpResponseMessage>(),
            new HttpRequestException());
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
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
    }

    [Fact]
    public async Task GenerateDraft_with_timed_out_openai_compatible_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(
            new Queue<HttpResponseMessage>(),
            new TaskCanceledException());
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
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
    public async Task GetApplications_includes_job_local_custom_facts_for_status_scanability()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var created = await CreateApplicationAsync(client, "We need .NET.");
        const string customFacts = """
            [
              {
                "id": "custom-1",
                "title": "Contract-specific API work",
                "summary": "Built a similar integration for a client.",
                "technologies": [".NET"],
                "allowedClaims": ["Built integrations"],
                "status": "PendingConfirmation"
              }
            ]
            """;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var application = await db.JobApplications.FindAsync(created.Id);
            Assert.NotNull(application);
            application.CustomFacts = customFacts;
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/applications");

        response.EnsureSuccessStatusCode();
        var applications = await response.Content.ReadFromJsonAsync<List<ApplicationResponse>>();
        Assert.NotNull(applications);
        var listed = Assert.Single(applications);
        Assert.Equal(customFacts, listed.CustomFacts);
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
    public async Task PutApplicationStatus_marks_saved_application_applied_and_updates_history_metadata()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/status",
            new ApplicationStatusRequest("Applied"));

        response.EnsureSuccessStatusCode();
        var applied = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(applied);
        Assert.Equal("Applied", applied.Status);
        Assert.True(applied.UpdatedAt > application.UpdatedAt);

        var historyResponse = await client.GetAsync("/api/applications?status=Applied");
        historyResponse.EnsureSuccessStatusCode();
        var history = await historyResponse.Content.ReadFromJsonAsync<List<ApplicationResponse>>();
        Assert.NotNull(history);
        var listed = Assert.Single(history);
        Assert.Equal(application.Id, listed.Id);
        Assert.Equal("Applied", listed.Status);
        Assert.Equal(applied.UpdatedAt, listed.UpdatedAt);
    }

    [Fact]
    public async Task PutApplicationStatus_marks_saved_application_archived_and_keeps_it_available_when_archived_sessions_are_included()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/status",
            new ApplicationStatusRequest("Archived"));

        response.EnsureSuccessStatusCode();
        var archived = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(archived);
        Assert.Equal("Archived", archived.Status);
        Assert.True(archived.UpdatedAt > application.UpdatedAt);

        var activeResponse = await client.GetAsync("/api/applications");
        var allResponse = await client.GetAsync("/api/applications?includeArchived=true");
        activeResponse.EnsureSuccessStatusCode();
        allResponse.EnsureSuccessStatusCode();
        var active = await activeResponse.Content.ReadFromJsonAsync<List<ApplicationResponse>>();
        var all = await allResponse.Content.ReadFromJsonAsync<List<ApplicationResponse>>();
        Assert.NotNull(active);
        Assert.NotNull(all);
        Assert.DoesNotContain(active, item => item.Id == application.Id);
        Assert.Contains(all, item => item.Id == application.Id && item.Status == "Archived");
    }

    [Fact]
    public async Task PutApplicationStatus_rejects_non_final_or_unknown_status()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/status",
            new ApplicationStatusRequest("ReadyForReview"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationStatusRequest.Status), error.Details.Keys);
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
    public async Task ExportCoverLetterTxt_returns_current_edited_cover_letter_with_text_headers_and_safe_filename()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(
            client,
            "We need .NET.",
            companyName: "Northwind & Sons",
            roleTitle: "Senior C# Engineer");
        await AddGeneratedDraftAsync(factory, application.Id, coverLetterText: "Edited cover letter text.\r\nSecond line.");

        var response = await client.GetAsync($"/api/applications/{application.Id}/exports/cover-letter.txt");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
        Assert.Equal("Edited cover letter text.\r\nSecond line.", await response.Content.ReadAsStringAsync());
        var contentDisposition = response.Content.Headers.ContentDisposition;
        Assert.NotNull(contentDisposition);
        Assert.Equal("attachment", contentDisposition.DispositionType);
        Assert.Equal("northwind-sons-senior-c-engineer-cover-letter.txt", GetFileName(contentDisposition));
    }

    [Fact]
    public async Task ExportCoverLetterTxt_rejects_missing_application_generated_draft_and_empty_cover_letter()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();

        var missingResponse = await client.GetAsync($"/api/applications/{Guid.NewGuid()}/exports/cover-letter.txt");

        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        var missingError = await missingResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(missingError);
        Assert.Equal("not_found", missingError.Code);

        var withoutDraft = await CreateApplicationAsync(client, "We need .NET.");
        var withoutDraftResponse = await client.GetAsync($"/api/applications/{withoutDraft.Id}/exports/cover-letter.txt");

        Assert.Equal(HttpStatusCode.BadRequest, withoutDraftResponse.StatusCode);
        var withoutDraftError = await withoutDraftResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(withoutDraftError);
        Assert.Contains(nameof(ApplicationResponse.GeneratedDraft), withoutDraftError.Details!.Keys);

        var emptyDraftApplication = await CreateApplicationAsync(client, "We need React.");
        await AddGeneratedDraftAsync(factory, emptyDraftApplication.Id, coverLetterText: "   ");
        var emptyDraftResponse = await client.GetAsync($"/api/applications/{emptyDraftApplication.Id}/exports/cover-letter.txt");

        Assert.Equal(HttpStatusCode.BadRequest, emptyDraftResponse.StatusCode);
        var emptyDraftError = await emptyDraftResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(emptyDraftError);
        Assert.Contains(nameof(GeneratedDraftResponse.CoverLetterText), emptyDraftError.Details!.Keys);
    }

    [Fact]
    public async Task ExportCoverLetterTxt_allows_stale_or_missing_claim_audit()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var stale = await CreateApplicationAsync(client, "We need .NET.", companyName: "Stale Co");
        var missing = await CreateApplicationAsync(client, "We need React.", companyName: "Missing Co");
        await AddGeneratedDraftAsync(factory, stale.Id, isClaimAuditStale: true, coverLetterText: "Stale audit cover letter.");
        await AddGeneratedDraftAsync(factory, missing.Id, claimAudit: "{}", auditUpdatedAt: null, coverLetterText: "Missing audit cover letter.");

        var staleResponse = await client.GetAsync($"/api/applications/{stale.Id}/exports/cover-letter.txt");
        var missingResponse = await client.GetAsync($"/api/applications/{missing.Id}/exports/cover-letter.txt");

        staleResponse.EnsureSuccessStatusCode();
        missingResponse.EnsureSuccessStatusCode();
        Assert.Equal("Stale audit cover letter.", await staleResponse.Content.ReadAsStringAsync());
        Assert.Equal("Missing audit cover letter.", await missingResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExportCoverLetterTxt_uses_application_id_filename_fallback_when_metadata_is_not_filename_safe()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(
            client,
            "We need .NET.",
            companyName: "!!!",
            roleTitle: "!!!");
        await AddGeneratedDraftAsync(factory, application.Id);

        var response = await client.GetAsync($"/api/applications/{application.Id}/exports/cover-letter.txt");

        response.EnsureSuccessStatusCode();
        var contentDisposition = response.Content.Headers.ContentDisposition;
        Assert.NotNull(contentDisposition);
        Assert.Equal($"application-{application.Id:N}-cover-letter.txt", GetFileName(contentDisposition));
    }

    [Fact]
    public async Task ExportCoverLetterDocx_returns_valid_document_with_current_cover_letter_context_profile_and_safe_filename()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync(
            "/api/profile",
            new ProfileRequest(
                "Alex Applicant",
                "alex@example.com",
                "+45 12 34 56 78",
                "Copenhagen",
                "https://www.linkedin.com/in/alex",
                null,
                null,
                "English",
                null,
                null));
        var application = await CreateApplicationAsync(
            client,
            "We need .NET.",
            companyName: "Northwind & Sons",
            roleTitle: "Senior C# Engineer");
        await AddGeneratedDraftAsync(factory, application.Id, coverLetterText: "Edited cover letter text.\r\nSecond line.");

        var response = await client.GetAsync($"/api/applications/{application.Id}/exports/cover-letter.docx");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", response.Content.Headers.ContentType?.MediaType);
        var contentDisposition = response.Content.Headers.ContentDisposition;
        Assert.NotNull(contentDisposition);
        Assert.Equal("attachment", contentDisposition.DispositionType);
        Assert.Equal("northwind-sons-senior-c-engineer-cover-letter.docx", GetFileName(contentDisposition));
        var documentText = await ReadDocxDocumentXmlAsync(response);
        Assert.Contains("Alex Applicant", documentText);
        Assert.Contains("alex@example.com", documentText);
        Assert.Contains("+45 12 34 56 78", documentText);
        Assert.Contains("Copenhagen", documentText);
        Assert.Contains("https://www.linkedin.com/in/alex", documentText);
        Assert.Contains("Northwind &amp; Sons", documentText);
        Assert.Contains("Senior C# Engineer", documentText);
        Assert.Contains("Edited cover letter text.", documentText);
        Assert.Contains("Second line.", documentText);
    }

    [Fact]
    public async Task ExportCoverLetterDocx_rejects_missing_application_generated_draft_and_empty_cover_letter()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();

        var missingResponse = await client.GetAsync($"/api/applications/{Guid.NewGuid()}/exports/cover-letter.docx");

        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        var missingError = await missingResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(missingError);
        Assert.Equal("not_found", missingError.Code);

        var withoutDraft = await CreateApplicationAsync(client, "We need .NET.");
        var withoutDraftResponse = await client.GetAsync($"/api/applications/{withoutDraft.Id}/exports/cover-letter.docx");

        Assert.Equal(HttpStatusCode.BadRequest, withoutDraftResponse.StatusCode);
        var withoutDraftError = await withoutDraftResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(withoutDraftError);
        Assert.Contains(nameof(ApplicationResponse.GeneratedDraft), withoutDraftError.Details!.Keys);

        var emptyDraftApplication = await CreateApplicationAsync(client, "We need React.");
        await AddGeneratedDraftAsync(factory, emptyDraftApplication.Id, coverLetterText: "   ");
        var emptyDraftResponse = await client.GetAsync($"/api/applications/{emptyDraftApplication.Id}/exports/cover-letter.docx");

        Assert.Equal(HttpStatusCode.BadRequest, emptyDraftResponse.StatusCode);
        var emptyDraftError = await emptyDraftResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(emptyDraftError);
        Assert.Contains(nameof(GeneratedDraftResponse.CoverLetterText), emptyDraftError.Details!.Keys);
    }

    [Fact]
    public async Task ExportCoverLetterDocx_allows_stale_or_missing_claim_audit_without_calling_ai()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>());
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var stale = await CreateApplicationAsync(client, "We need .NET.", companyName: "Stale Co");
        var missing = await CreateApplicationAsync(client, "We need React.", companyName: "Missing Co");
        await AddGeneratedDraftAsync(factory, stale.Id, isClaimAuditStale: true, coverLetterText: "Stale audit cover letter.");
        await AddGeneratedDraftAsync(factory, missing.Id, claimAudit: "{}", auditUpdatedAt: null, coverLetterText: "Missing audit cover letter.");

        var staleResponse = await client.GetAsync($"/api/applications/{stale.Id}/exports/cover-letter.docx");
        var missingResponse = await client.GetAsync($"/api/applications/{missing.Id}/exports/cover-letter.docx");

        staleResponse.EnsureSuccessStatusCode();
        missingResponse.EnsureSuccessStatusCode();
        Assert.Contains("Stale audit cover letter.", await ReadDocxDocumentXmlAsync(staleResponse));
        Assert.Contains("Missing audit cover letter.", await ReadDocxDocumentXmlAsync(missingResponse));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ExportCoverLetterDocx_tolerates_missing_optional_profile_contact_fields()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync(
            "/api/profile",
            new ProfileRequest(
                "Alex Applicant",
                "alex@example.com",
                null,
                null,
                null,
                null,
                null,
                "English",
                null,
                null));
        var application = await CreateApplicationAsync(client, "We need .NET.");
        await AddGeneratedDraftAsync(factory, application.Id, coverLetterText: "Cover letter with sparse contact info.");

        var response = await client.GetAsync($"/api/applications/{application.Id}/exports/cover-letter.docx");

        response.EnsureSuccessStatusCode();
        var documentText = await ReadDocxDocumentXmlAsync(response);
        Assert.Contains("Alex Applicant", documentText);
        Assert.Contains("alex@example.com", documentText);
        Assert.Contains("Cover letter with sparse contact info.", documentText);
    }

    [Fact]
    public async Task ExportCoverLetterDocx_uses_application_id_filename_fallback_when_metadata_is_not_filename_safe()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(
            client,
            "We need .NET.",
            companyName: "!!!",
            roleTitle: "!!!");
        await AddGeneratedDraftAsync(factory, application.Id);

        var response = await client.GetAsync($"/api/applications/{application.Id}/exports/cover-letter.docx");

        response.EnsureSuccessStatusCode();
        var contentDisposition = response.Content.Headers.ContentDisposition;
        Assert.NotNull(contentDisposition);
        Assert.Equal($"application-{application.Id:N}-cover-letter.docx", GetFileName(contentDisposition));
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
    public async Task AuditClaims_with_ollama_does_not_send_candidate_fit_brief_profile_fact_ids()
    {
        var handler = new QueuedOllamaHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OllamaGenerateContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOllamaHandler(handler);
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftForOllamaAuditAsync(factory, client, "match-dotnet-test");
        var traceOnlyProfileFactId = Guid.NewGuid();
        await SetCandidateFitBriefAsync(factory, draft.JobApplicationId, traceOnlyProfileFactId);

        var response = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var ollamaRequestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("match-dotnet-test", ollamaRequestJson);
        Assert.DoesNotContain(traceOnlyProfileFactId.ToString(), ollamaRequestJson);
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

    [Fact]
    public async Task AuditClaims_with_openai_compatible_persists_structured_results_and_records_successful_ai_run()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
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

        var requestJson = await Assert.Single(handler.Requests).Content!.ReadAsStringAsync();
        Assert.Contains("\"model\":\"local-model\"", requestJson);
        Assert.Contains("\"response_format\":{\"type\":\"text\"}", requestJson);
        Assert.Contains("Return only strict JSON", requestJson);
        Assert.Contains("match-dotnet-test", requestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("ClaimAudit", run.Step);
        Assert.Equal("OpenAiCompatible", run.Provider);
        Assert.Equal("Succeeded", run.Status);
        Assert.Equal(1, run.AttemptCount);
    }

    [Fact]
    public async Task AuditClaims_with_openai_compatible_repairs_unknown_evidence_reference_once()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidOllamaClaimAuditJson("unknown-match")) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidOllamaClaimAuditJson("match-dotnet-test")) }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftForOllamaAuditAsync(factory, client, "match-dotnet-test");

        var response = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, handler.Requests.Count);
        var repairRequestJson = await handler.Requests[1].Content!.ReadAsStringAsync();
        Assert.Contains("Repair this claim audit JSON", repairRequestJson);
        Assert.Contains("structurally invalid claim audit JSON", repairRequestJson);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("RepairedSucceeded", run.Status);
        Assert.Equal(2, run.AttemptCount);
    }

    [Fact]
    public async Task AuditClaims_with_openai_compatible_invalid_output_after_repair_records_failure_raw_payload_and_preserves_audit()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("""{"claims":[{"id":"claim-1","text":"Broken","status":"Maybe","evidenceIds":[]}]}""") }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            storeRawPayloads: true);
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
        Assert.Contains("Raw payload:", run.ErrorMessage);
        Assert.Contains("Maybe", run.ErrorMessage);
    }

    [Fact]
    public async Task AuditClaims_with_openai_compatible_api_error_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { ReasonPhrase = "Service Unavailable" }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
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
        Assert.Equal("OpenAiCompatible", run.Provider);
        Assert.Equal("local-model", run.Model);
        Assert.Equal(1, run.AttemptCount);
    }

    [Fact]
    public async Task AuditClaims_with_unreachable_openai_compatible_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(
            new Queue<HttpResponseMessage>(),
            new HttpRequestException());
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftForOllamaAuditAsync(factory, client, "match-dotnet-test");

        var response = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
    }

    [Fact]
    public async Task AuditClaims_with_timed_out_openai_compatible_records_failure()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(
            new Queue<HttpResponseMessage>(),
            new TaskCanceledException());
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftForOllamaAuditAsync(factory, client, "match-dotnet-test");

        var response = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
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

    private static async Task<ApplicationResponse> GetApplicationAsync(HttpClient client, Guid applicationId)
    {
        var response = await client.GetAsync($"/api/applications/{applicationId}");
        response.EnsureSuccessStatusCode();
        var application = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(application);
        return application;
    }

    private static void AssertStablePreparedState(
        ApplicationResponse application,
        DateTimeOffset stablePreparedAt,
        Guid existingDraftId)
    {
        Assert.Equal("Stable Company", application.CompanyName);
        Assert.Equal("Stable Role", application.RoleTitle);
        Assert.Equal("PreparedForEvidenceReview", application.Status);
        Assert.Equal("PreparedForEvidenceReview", application.PreparationStatus);
        Assert.Equal(stablePreparedAt, application.LastPreparedAt);
        Assert.Contains("stable-signal", application.JobSignals);
        Assert.Contains("existing-match", application.EvidenceMatches);
        Assert.Contains("existing-unmatched", application.UnmatchedRequirements);
        Assert.Contains("existing-fit", application.CandidateFitBrief);
        Assert.Contains("existing-approved", application.ApprovedEvidence);
        Assert.Contains("existing-gap", application.GapDecisions);
        Assert.Contains("existing-custom", application.CustomFacts);
        Assert.NotNull(application.GeneratedDraft);
        Assert.Equal(existingDraftId, application.GeneratedDraft.Id);
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
        application.GapDecisions = JsonSerializer.Serialize(
            new[]
            {
                new GapDecisionTestItem("unmatched-kubernetes", "MentionAsLearningInterest", null)
            },
            JsonOptions);
        await db.SaveChangesAsync();
    }

    private static async Task SetCandidateFitBriefAsync(
        WebApplicationFactory<Program> factory,
        Guid applicationId,
        Guid traceOnlyProfileFactId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var application = await db.JobApplications.FindAsync(applicationId);
        Assert.NotNull(application);
        application.CandidateFitBrief = CandidateFitBriefTraceabilityJson(traceOnlyProfileFactId);
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
        bool isClaimAuditStale = false,
        string coverLetterText = "Existing cover letter.")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        auditUpdatedAt ??= claimAudit == "{}" ? null : now;
        var draft = new GeneratedDraft
        {
            Id = Guid.NewGuid(),
            JobApplicationId = applicationId,
            CoverLetterText = coverLetterText,
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

    private static string? GetFileName(ContentDispositionHeaderValue contentDisposition) =>
        contentDisposition.FileNameStar ?? contentDisposition.FileName?.Trim('"');

    private static async Task<string> ReadDocxDocumentXmlAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var document = archive.GetEntry("word/document.xml");
        Assert.NotNull(document);
        using var reader = new StreamReader(document.Open());
        return await reader.ReadToEndAsync();
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

    private static async Task SetApprovedEvidenceAsync(WebApplicationFactory<Program> factory, Guid applicationId, string approvedEvidence)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var application = await db.JobApplications.FindAsync(applicationId);
        Assert.NotNull(application);
        application.ApprovedEvidence = approvedEvidence;
        await db.SaveChangesAsync();
    }

    private static async Task SetEvidenceReviewStateAsync(
        WebApplicationFactory<Program> factory,
        Guid applicationId,
        string evidenceMatches,
        string unmatchedRequirements,
        string approvedEvidence)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var application = await db.JobApplications.FindAsync(applicationId);
        Assert.NotNull(application);
        application.EvidenceMatches = evidenceMatches;
        application.UnmatchedRequirements = unmatchedRequirements;
        application.ApprovedEvidence = approvedEvidence;
        await db.SaveChangesAsync();
    }

    private static async Task<DateTimeOffset> SetPreparedApplicationStateAsync(
        WebApplicationFactory<Program> factory,
        Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var application = await db.JobApplications.FindAsync(applicationId);
        Assert.NotNull(application);
        var now = DateTimeOffset.UtcNow;
        application.CompanyName = "Stable Company";
        application.RoleTitle = "Stable Role";
        application.Status = "PreparedForEvidenceReview";
        application.JobSignals = """{"signals":[{"id":"stable-signal","label":".NET"}]}""";
        application.EvidenceMatches = """[{"id":"existing-match","signalId":"stable-signal","profileFactId":"00000000-0000-0000-0000-000000000001","summary":"Existing match","matchedTerms":[".NET"]}]""";
        application.UnmatchedRequirements = """[{"id":"existing-unmatched","signalId":"stable-gap","requirement":"Existing gap","recommendation":"Existing recommendation"}]""";
        application.CandidateFitBrief = """{"candidateSummary":"existing-fit"}""";
        application.ApprovedEvidence = """[{"id":"existing-approved"}]""";
        application.GapDecisions = """[{"unmatchedRequirementId":"existing-gap","decision":"AcceptedGap"}]""";
        application.CustomFacts = """[{"id":"existing-custom","title":"Existing custom fact"}]""";
        application.LastPreparedAt = now;
        application.PreparationStatus = "PreparedForEvidenceReview";
        application.UpdatedAt = now;
        await db.SaveChangesAsync();
        return now;
    }

    private sealed record GapDecisionTestItem(
        string UnmatchedRequirementId,
        string Decision,
        Guid? CustomFactId = null);

    private sealed record CustomFactTestItem(
        Guid Id,
        string UnmatchedRequirementId,
        string Title,
        string Summary,
        IReadOnlyList<string> Technologies,
        IReadOnlyList<string> AllowedClaims,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReviewedAt);

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
        string technologies,
        string? summary = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/profile/facts",
            new ProfileFactRequest(
                "Project",
                title,
                summary ?? $"Evidence for {title}.",
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

    private sealed class StubPdfTextExtractor(string text) : IPdfTextExtractor
    {
        public Task<PdfTextExtractionResult> ExtractAsync(Stream pdfStream, CancellationToken ct) =>
            Task.FromResult(PdfTextExtractionResult.Success(text));
    }

    private static JsonContent OpenAiChatCompletionContent(string content) =>
        JsonContent.Create(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        role = "assistant",
                        content
                    }
                }
            }
        });

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
              "quality": "Strong",
              "reason": ".NET is directly supported by approved API work.",
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

    private static string ValidOpenAiPrepareEvidenceMatchingJson(Guid profileFactId) =>
        $$"""
        {
          "evidenceMatches": [
            {
              "signalId": "dotnet",
              "profileFactId": "{{profileFactId}}",
              "summary": "Approved API work demonstrates .NET experience.",
              "quality": "Strong",
              "reason": ".NET is directly supported by approved API work.",
              "matchedTerms": [".NET"]
            }
          ],
          "unmatchedRequirements": [
            {
              "signalId": "postgresql",
              "recommendation": "Treat PostgreSQL as an honest learning area."
            },
            {
              "signalId": "kubernetes",
              "recommendation": "Treat Kubernetes as an honest learning area."
            },
            {
              "signalId": "rest-api",
              "recommendation": "Treat REST API ownership as an honest learning area."
            }
          ]
        }
        """;

    private static string ValidCandidateFitBriefJson(Guid profileFactId) =>
        $$"""
        {
          "candidateSummary": "Strong evidence-led fit for the role.",
          "skillGroups": [
            {
              "name": "Supported skills",
              "items": [
                {
                  "title": ".NET",
                  "summary": "Approved API work supports .NET delivery.",
                  "supportingProfileFactIds": ["{{profileFactId}}"]
                }
              ]
            }
          ],
          "competencies": [
            {
              "title": "Delivery ownership",
              "summary": "The candidate has concrete delivery evidence.",
              "supportingProfileFactIds": ["{{profileFactId}}"]
            }
          ],
          "relevantProjects": [
            {
              "title": "Approved API work",
              "summary": "Relevant project evidence for this application.",
              "supportingProfileFactIds": ["{{profileFactId}}"]
            }
          ],
          "transferableStrengths": [
            {
              "title": "Traceable evidence",
              "summary": "Claims map back to approved profile facts.",
              "supportingProfileFactIds": ["{{profileFactId}}"]
            }
          ],
          "riskNotes": [
            {
              "title": "Unsupported requirements",
              "summary": "Unsupported requirements should remain honest gaps.",
              "supportingProfileFactIds": []
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

    private static string CandidateFitBriefTraceabilityJson(Guid profileFactId) =>
        $$"""
        {
          "candidateSummary": "Traceability-only fit context.",
          "skillGroups": [
            {
              "name": "Traceability-only",
              "items": [
                {
                  "title": "Traceability-only profile fact",
                  "summary": "This profile fact id explains where the fit context came from.",
                  "supportingProfileFactIds": ["{{profileFactId}}"]
                }
              ]
            }
          ],
          "competencies": [],
          "relevantProjects": [],
          "transferableStrengths": [],
          "riskNotes": []
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
