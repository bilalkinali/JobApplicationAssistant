using System.Net.Http.Json;
using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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
}
