using System.Net.Http.Json;
using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace JobApplicationAssistant.Api.Tests.Endpoints;

public sealed class AiStatusApiTests
{
    [Fact]
    public async Task GetAiStatus_reports_configured_fake_provider_as_available()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/ai/status");

        response.EnsureSuccessStatusCode();
        var status = await response.Content.ReadFromJsonAsync<AiProviderStatusResponse>();
        Assert.NotNull(status);
        Assert.Equal("Fake", status.Provider);
        Assert.Equal("fake-deterministic", status.Model);
        Assert.Null(status.Endpoint);
        Assert.True(status.IsAvailable);
        Assert.Equal("Fake provider is available.", status.Message);
    }

    [Fact]
    public async Task PostAiDiagnostics_reports_fake_provider_readiness()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/ai/diagnostics", null);

        response.EnsureSuccessStatusCode();
        var diagnostics = await response.Content.ReadFromJsonAsync<AiDiagnosticsResponse>();
        Assert.NotNull(diagnostics);
        Assert.Equal("Fake", diagnostics.Provider);
        Assert.Equal("fake-deterministic", diagnostics.Model);
        Assert.True(diagnostics.IsAvailable);
        Assert.Equal("Fake provider diagnostics passed.", diagnostics.Message);
        var check = Assert.Single(diagnostics.Checks);
        Assert.Equal("provider", check.Name);
        Assert.Equal("ok", check.Status);
    }

    [Fact]
    public async Task GetAiStatus_reports_configured_ollama_provider_as_unavailable_without_blocking_startup()
    {
        await using var factory = new TestApplicationFactory().WithAiConfiguration(
            provider: "Ollama",
            endpoint: "http://127.0.0.1:1",
            model: "llama3.1:8b",
            timeoutSeconds: "1");
        using var client = factory.CreateClient();

        var rootResponse = await client.GetAsync("/");
        var statusResponse = await client.GetAsync("/api/ai/status");

        rootResponse.EnsureSuccessStatusCode();
        statusResponse.EnsureSuccessStatusCode();
        var status = await statusResponse.Content.ReadFromJsonAsync<AiProviderStatusResponse>();
        Assert.NotNull(status);
        Assert.Equal("Ollama", status.Provider);
        Assert.Equal("llama3.1:8b", status.Model);
        Assert.Equal("http://127.0.0.1:1", status.Endpoint);
        Assert.False(status.IsAvailable);
        Assert.Equal("Ollama endpoint is unavailable.", status.Message);
    }

    [Fact]
    public async Task PostAiDiagnostics_reports_unavailable_ollama_connectivity()
    {
        await using var factory = new TestApplicationFactory().WithAiConfiguration(
            provider: "Ollama",
            endpoint: "http://127.0.0.1:1",
            model: "llama3.1:8b",
            timeoutSeconds: "1");
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/ai/diagnostics", null);

        response.EnsureSuccessStatusCode();
        var diagnostics = await response.Content.ReadFromJsonAsync<AiDiagnosticsResponse>();
        Assert.NotNull(diagnostics);
        Assert.Equal("Ollama", diagnostics.Provider);
        Assert.Equal("llama3.1:8b", diagnostics.Model);
        Assert.Equal("http://127.0.0.1:1", diagnostics.Endpoint);
        Assert.False(diagnostics.IsAvailable);
        Assert.Equal("Ollama endpoint is unavailable.", diagnostics.Message);
        var check = Assert.Single(diagnostics.Checks);
        Assert.Equal("connectivity", check.Name);
        Assert.Equal("unavailable", check.Status);
    }

    [Fact]
    public async Task GetAiStatus_reports_configured_openai_compatible_provider_without_fake_fallback()
    {
        await using var factory = new TestApplicationFactory().WithAiConfiguration(
            provider: "OpenAiCompatible",
            endpoint: "http://127.0.0.1:1/v1",
            model: "qwen2.5-coder-14b-instruct",
            timeoutSeconds: "1");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/ai/status");

        response.EnsureSuccessStatusCode();
        var status = await response.Content.ReadFromJsonAsync<AiProviderStatusResponse>();
        Assert.NotNull(status);
        Assert.Equal("OpenAiCompatible", status.Provider);
        Assert.Equal("qwen2.5-coder-14b-instruct", status.Model);
        Assert.Equal("http://127.0.0.1:1/v1", status.Endpoint);
        Assert.False(status.IsAvailable);
        Assert.Equal("OpenAI-compatible endpoint is unavailable.", status.Message);
    }

    [Fact]
    public async Task PostAiDiagnostics_reports_openai_compatible_available_model()
    {
        var handler = new QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            JsonResponse("""{"data":[{"id":"local-model"}]}""")
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            storeRawPayloads: true);
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/ai/diagnostics", null);

        response.EnsureSuccessStatusCode();
        var diagnostics = await response.Content.ReadFromJsonAsync<AiDiagnosticsResponse>();
        Assert.NotNull(diagnostics);
        Assert.Equal("OpenAiCompatible", diagnostics.Provider);
        Assert.Equal("local-model", diagnostics.Model);
        Assert.Equal("http://lm-studio.test/v1", diagnostics.Endpoint);
        Assert.True(diagnostics.IsAvailable);
        Assert.Equal("OpenAI-compatible provider is available.", diagnostics.Message);
        Assert.Contains(diagnostics.Checks, check => check is { Name: "connectivity", Status: "ok" });
        Assert.Contains(diagnostics.Checks, check => check is { Name: "modelConfigured", Status: "ok" });
        Assert.Contains(diagnostics.Checks, check => check is { Name: "modelAvailability", Status: "ok" });
        Assert.Contains(diagnostics.Checks, check => check is { Name: "rawPayloads", Status: "enabled" });
        var request = Assert.Single(handler.Requests);
        Assert.Equal("http://lm-studio.test/v1/models", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task PostAiDiagnostics_normalizes_openai_compatible_root_endpoint_to_v1()
    {
        var handler = new QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            JsonResponse("""{"data":[{"id":"local-model"}]}""")
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(
            handler,
            model: "local-model",
            endpoint: "http://lm-studio.test");
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/ai/diagnostics", null);

        response.EnsureSuccessStatusCode();
        var diagnostics = await response.Content.ReadFromJsonAsync<AiDiagnosticsResponse>();
        Assert.NotNull(diagnostics);
        Assert.Equal("http://lm-studio.test/v1", diagnostics.Endpoint);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("http://lm-studio.test/v1/models", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task PostAiDiagnostics_reports_openai_compatible_unavailable_model()
    {
        var handler = new QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            JsonResponse("""{"data":[{"id":"different-model"}]}""")
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/ai/diagnostics", null);

        response.EnsureSuccessStatusCode();
        var diagnostics = await response.Content.ReadFromJsonAsync<AiDiagnosticsResponse>();
        Assert.NotNull(diagnostics);
        Assert.False(diagnostics.IsAvailable);
        Assert.Equal("Model local-model was not found.", diagnostics.Message);
        Assert.Contains(diagnostics.Checks, check => check is { Name: "connectivity", Status: "ok" });
        Assert.Contains(diagnostics.Checks, check => check is { Name: "modelAvailability", Status: "unavailable" });
    }

    [Fact]
    public async Task PostAiDiagnostics_reports_openai_compatible_blank_model_gracefully()
    {
        var handler = new QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            JsonResponse("""{"data":[{"id":"local-model"}]}""")
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: " ");
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/ai/diagnostics", null);

        response.EnsureSuccessStatusCode();
        var diagnostics = await response.Content.ReadFromJsonAsync<AiDiagnosticsResponse>();
        Assert.NotNull(diagnostics);
        Assert.Equal("OpenAiCompatible", diagnostics.Provider);
        Assert.False(diagnostics.IsAvailable);
        Assert.Equal("OpenAI-compatible endpoint is reachable, but Ai:Model is blank.", diagnostics.Message);
        Assert.Contains(diagnostics.Checks, check => check is { Name: "modelConfigured", Status: "unavailable" });
        Assert.Contains(diagnostics.Checks, check => check is { Name: "connectivity", Status: "ok" });
    }

    [Fact]
    public async Task PostAiDiagnostics_reports_openai_compatible_unknown_model_availability_gracefully()
    {
        var handler = new QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(
        [
            new(System.Net.HttpStatusCode.NotFound) { ReasonPhrase = "Not Found" }
        ]));
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/ai/diagnostics", null);

        response.EnsureSuccessStatusCode();
        var diagnostics = await response.Content.ReadFromJsonAsync<AiDiagnosticsResponse>();
        Assert.NotNull(diagnostics);
        Assert.True(diagnostics.IsAvailable);
        Assert.Equal("OpenAI-compatible endpoint is reachable; model availability could not be determined.", diagnostics.Message);
        Assert.Contains(diagnostics.Checks, check => check is { Name: "connectivity", Status: "ok" });
        Assert.Contains(diagnostics.Checks, check => check is { Name: "modelAvailability", Status: "unknown" });
    }

    [Fact]
    public async Task PostAiDiagnostics_reports_openai_compatible_unreachable_endpoint_gracefully()
    {
        var handler = new QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(), new HttpRequestException());
        await using var factory = new TestApplicationFactory().WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/ai/diagnostics", null);

        response.EnsureSuccessStatusCode();
        var diagnostics = await response.Content.ReadFromJsonAsync<AiDiagnosticsResponse>();
        Assert.NotNull(diagnostics);
        Assert.False(diagnostics.IsAvailable);
        Assert.Equal("OpenAI-compatible endpoint is unavailable.", diagnostics.Message);
        Assert.Contains(diagnostics.Checks, check => check is { Name: "connectivity", Status: "unavailable" });
    }

    [Fact]
    public async Task GetAiStatus_does_not_fall_back_to_fake_for_unsupported_provider()
    {
        await using var factory = new TestApplicationFactory().WithAiConfiguration(
            provider: "OpenAI",
            endpoint: "https://api.openai.com/v1",
            model: "gpt-4.1-mini",
            timeoutSeconds: "1");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/ai/status");

        response.EnsureSuccessStatusCode();
        var status = await response.Content.ReadFromJsonAsync<AiProviderStatusResponse>();
        Assert.NotNull(status);
        Assert.Equal("OpenAI", status.Provider);
        Assert.Equal("gpt-4.1-mini", status.Model);
        Assert.Equal("https://api.openai.com/v1", status.Endpoint);
        Assert.False(status.IsAvailable);
        Assert.Contains("not supported", status.Message);
    }

    [Fact]
    public async Task PostAiDiagnostics_reports_unsupported_provider_without_fake_fallback()
    {
        await using var factory = new TestApplicationFactory().WithAiConfiguration(
            provider: "OpenAI",
            endpoint: "https://api.openai.com/v1",
            model: "gpt-4.1-mini",
            timeoutSeconds: "1");
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/ai/diagnostics", null);

        response.EnsureSuccessStatusCode();
        var diagnostics = await response.Content.ReadFromJsonAsync<AiDiagnosticsResponse>();
        Assert.NotNull(diagnostics);
        Assert.Equal("OpenAI", diagnostics.Provider);
        Assert.Equal("gpt-4.1-mini", diagnostics.Model);
        Assert.Equal("https://api.openai.com/v1", diagnostics.Endpoint);
        Assert.False(diagnostics.IsAvailable);
        Assert.Contains("not supported", diagnostics.Message);
        var check = Assert.Single(diagnostics.Checks);
        Assert.Equal("provider", check.Name);
        Assert.Equal("unsupported", check.Status);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new()
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    internal sealed class QueuedOpenAiCompatibleHandler(
        Queue<HttpResponseMessage> responses,
        Exception? exception = null) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public void Enqueue(HttpResponseMessage response) => responses.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(responses.Dequeue());
        }
    }
}

internal static class TestApplicationFactoryAiExtensions
{
    public static WebApplicationFactory<Program> WithAiConfiguration(
        this TestApplicationFactory factory,
        string provider,
        string endpoint,
        string model,
        string timeoutSeconds) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ai:Provider"] = provider,
                    ["Ai:Endpoint"] = endpoint,
                    ["Ai:Model"] = model,
                    ["Ai:TimeoutSeconds"] = timeoutSeconds
                });
            });
        });

    public static WebApplicationFactory<Program> WithOpenAiCompatibleHandler(
        this TestApplicationFactory factory,
        AiStatusApiTests.QueuedOpenAiCompatibleHandler handler,
        string model,
        bool storeRawPayloads = false,
        string endpoint = "http://lm-studio.test/v1") =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiProvider>();
                services.RemoveAll<AiOptions>();
                services.AddSingleton(new AiOptions
                {
                    Provider = "OpenAiCompatible",
                    Endpoint = OpenAiCompatibleAiProvider.NormalizeEndpoint(endpoint),
                    Model = model,
                    TimeoutSeconds = 1,
                    StoreRawPayloads = storeRawPayloads
                });
                services.AddSingleton<IAiProvider>(serviceProvider =>
                {
                    var options = serviceProvider.GetRequiredService<AiOptions>();
                    var client = new HttpClient(handler)
                    {
                        BaseAddress = new Uri($"{options.Endpoint.TrimEnd('/')}/"),
                        Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
                    };

                    return new OpenAiCompatibleAiProvider(client, options);
                });
            });
        });
}
