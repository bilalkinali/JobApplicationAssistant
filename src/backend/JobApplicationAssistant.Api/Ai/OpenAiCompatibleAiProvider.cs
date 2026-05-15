using System.Net.Http.Json;
using System.Text.Json;

namespace JobApplicationAssistant.Api.Ai;

public sealed class OpenAiCompatibleAiProvider : IAiProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly AiOptions options;

    public OpenAiCompatibleAiProvider(HttpClient httpClient, AiOptions options)
    {
        this.httpClient = httpClient;
        this.options = options;
    }

    public async Task<AiProviderStatus> GetStatusAsync(CancellationToken ct)
    {
        var diagnostics = await RunDiagnosticsAsync(ct);

        return new AiProviderStatus(
            diagnostics.Provider,
            diagnostics.Model,
            diagnostics.Endpoint,
            diagnostics.IsAvailable,
            diagnostics.Message);
    }

    public async Task<AiDiagnosticsResult> RunDiagnosticsAsync(CancellationToken ct)
    {
        var checks = new List<AiDiagnosticCheck>
        {
            new("provider", "ok", "OpenAI-compatible provider is selected."),
            new("rawPayloads", options.StoreRawPayloads ? "enabled" : "disabled", options.StoreRawPayloads
                ? "Raw payload storage is enabled."
                : "Raw payload storage is disabled.")
        };
        var configuredModel = options.Model.Trim();
        var isModelConfigured = !string.IsNullOrWhiteSpace(configuredModel);
        checks.Add(new AiDiagnosticCheck(
            "modelConfigured",
            isModelConfigured ? "ok" : "unavailable",
            isModelConfigured ? $"Model {configuredModel} is configured." : "Ai:Model is blank."));

        try
        {
            using var response = await httpClient.GetAsync("models", ct);
            checks.Add(new AiDiagnosticCheck("connectivity", "ok", "OpenAI-compatible endpoint is reachable."));

            if (!response.IsSuccessStatusCode)
            {
                checks.Add(new AiDiagnosticCheck(
                    "modelAvailability",
                    "unknown",
                    $"Model list could not be determined because /models returned {(int)response.StatusCode} {response.ReasonPhrase}."));

                return Result(
                    isModelConfigured,
                    isModelConfigured
                        ? "OpenAI-compatible endpoint is reachable; model availability could not be determined."
                        : "OpenAI-compatible endpoint is reachable, but Ai:Model is blank.",
                    checks);
            }

            OpenAiModelsResponse? models;
            try
            {
                models = await response.Content.ReadFromJsonAsync<OpenAiModelsResponse>(JsonOptions, ct);
            }
            catch (JsonException)
            {
                checks.Add(new AiDiagnosticCheck("modelAvailability", "unknown", "Model list response was malformed."));
                return Result(
                    isModelConfigured,
                    isModelConfigured
                        ? "OpenAI-compatible endpoint is reachable; model availability could not be determined."
                        : "OpenAI-compatible endpoint is reachable, but Ai:Model is blank.",
                    checks);
            }

            if (models?.Data is null)
            {
                checks.Add(new AiDiagnosticCheck("modelAvailability", "unknown", "Model list response did not include a data array."));
                return Result(
                    isModelConfigured,
                    isModelConfigured
                        ? "OpenAI-compatible endpoint is reachable; model availability could not be determined."
                        : "OpenAI-compatible endpoint is reachable, but Ai:Model is blank.",
                    checks);
            }

            if (!isModelConfigured)
            {
                checks.Add(new AiDiagnosticCheck("modelAvailability", "unknown", "Model availability was not checked because Ai:Model is blank."));
                return Result(false, "OpenAI-compatible endpoint is reachable, but Ai:Model is blank.", checks);
            }

            var isModelAvailable = models.Data.Any(model => string.Equals(model?.Id, configuredModel, StringComparison.OrdinalIgnoreCase));
            checks.Add(new AiDiagnosticCheck(
                "modelAvailability",
                isModelAvailable ? "ok" : "unavailable",
                isModelAvailable ? $"Model {configuredModel} is available." : $"Model {configuredModel} was not found."));

            return Result(
                isModelConfigured && isModelAvailable,
                isModelAvailable
                    ? "OpenAI-compatible provider is available."
                    : isModelConfigured
                        ? $"Model {configuredModel} was not found."
                        : "OpenAI-compatible endpoint is reachable, but Ai:Model is blank.",
                checks);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            const string message = "OpenAI-compatible endpoint is unavailable.";
            checks.Add(new AiDiagnosticCheck("connectivity", "unavailable", message));
            return Result(false, message, checks);
        }
        catch (HttpRequestException)
        {
            const string message = "OpenAI-compatible endpoint is unavailable.";
            checks.Add(new AiDiagnosticCheck("connectivity", "unavailable", message));
            return Result(false, message, checks);
        }
    }

    public Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisInput input, CancellationToken ct) =>
        throw ProviderUnavailable();

    public Task<EvidenceMatchResult> MatchEvidenceAsync(EvidenceMatchInput input, CancellationToken ct) =>
        throw ProviderUnavailable();

    public Task<DraftGenerationResult> GenerateDraftAsync(DraftGenerationInput input, CancellationToken ct) =>
        throw ProviderUnavailable();

    public Task<ClaimAuditResult> AuditClaimsAsync(ClaimAuditInput input, CancellationToken ct) =>
        throw ProviderUnavailable();

    private AiDiagnosticsResult Result(bool isAvailable, string message, IReadOnlyList<AiDiagnosticCheck> checks) =>
        new("OpenAiCompatible", options.Model, options.Endpoint, isAvailable, message, checks);

    private static AiProviderUnavailableException ProviderUnavailable() =>
        new("OpenAI-compatible workflow calls are not implemented yet.");

    private sealed record OpenAiModelsResponse(IReadOnlyList<OpenAiModel?>? Data);

    private sealed record OpenAiModel(string Id);
}
