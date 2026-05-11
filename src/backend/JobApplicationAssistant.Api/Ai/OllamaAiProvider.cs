using System.Net.Http.Json;

namespace JobApplicationAssistant.Api.Ai;

public sealed class OllamaAiProvider : IAiProvider
{
    private readonly HttpClient httpClient;
    private readonly AiOptions options;

    public OllamaAiProvider(HttpClient httpClient, AiOptions options)
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
        var checks = new List<AiDiagnosticCheck>();

        try
        {
            using var response = await httpClient.GetAsync("/api/tags", ct);
            if (!response.IsSuccessStatusCode)
            {
                var message = $"Ollama returned {(int)response.StatusCode} {response.ReasonPhrase}.";
                checks.Add(new AiDiagnosticCheck("connectivity", "unavailable", message));
                return Unavailable(message, checks);
            }

            var tags = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(cancellationToken: ct);
            checks.Add(new AiDiagnosticCheck("connectivity", "ok", "Ollama endpoint is reachable."));

            if (tags?.Models.Any(model => string.Equals(model.Name, options.Model, StringComparison.OrdinalIgnoreCase)) == true)
            {
                checks.Add(new AiDiagnosticCheck("model", "ok", $"Model {options.Model} is available."));
                return new AiDiagnosticsResult(
                    "Ollama",
                    options.Model,
                    options.Endpoint,
                    true,
                    "Ollama provider is available.",
                    checks);
            }

            var modelMessage = $"Model {options.Model} was not found in Ollama.";
            checks.Add(new AiDiagnosticCheck("model", "unavailable", modelMessage));
            return Unavailable(modelMessage, checks);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            const string message = "Ollama endpoint is unavailable.";
            checks.Add(new AiDiagnosticCheck("connectivity", "unavailable", message));
            return Unavailable(message, checks);
        }
        catch (HttpRequestException)
        {
            const string message = "Ollama endpoint is unavailable.";
            checks.Add(new AiDiagnosticCheck("connectivity", "unavailable", message));
            return Unavailable(message, checks);
        }
    }

    public Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisInput input, CancellationToken ct) =>
        throw new NotSupportedException("Ollama workflow operations are not implemented in this milestone slice.");

    public Task<EvidenceMatchResult> MatchEvidenceAsync(EvidenceMatchInput input, CancellationToken ct) =>
        throw new NotSupportedException("Ollama workflow operations are not implemented in this milestone slice.");

    public Task<DraftGenerationResult> GenerateDraftAsync(DraftGenerationInput input, CancellationToken ct) =>
        throw new NotSupportedException("Ollama workflow operations are not implemented in this milestone slice.");

    public Task<ClaimAuditResult> AuditClaimsAsync(ClaimAuditInput input, CancellationToken ct) =>
        throw new NotSupportedException("Ollama workflow operations are not implemented in this milestone slice.");

    private AiDiagnosticsResult Unavailable(string message, IReadOnlyList<AiDiagnosticCheck> checks) =>
        new("Ollama", options.Model, options.Endpoint, false, message, checks);

    private sealed record OllamaTagsResponse(IReadOnlyList<OllamaModel> Models);

    private sealed record OllamaModel(string Name);
}
