using System.Net.Http.Json;
using System.Text.Json;

namespace JobApplicationAssistant.Api.Ai;

public sealed class OllamaAiProvider : IAiProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string JobAnalysisPrompt = LoadPrompt("job-analysis.md");

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

    public async Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisInput input, CancellationToken ct)
    {
        var responseText = await GenerateAsync(BuildJobAnalysisPrompt(input), attemptCount: 1, ct);
        try
        {
            return ParseJobAnalysis(responseText, attemptCount: 1);
        }
        catch (AiInvalidOutputException firstFailure)
        {
            var repairedText = await GenerateAsync(BuildRepairPrompt(responseText, firstFailure.Message), attemptCount: 2, ct);
            return ParseJobAnalysis(repairedText, attemptCount: 2);
        }
    }

    public Task<EvidenceMatchResult> MatchEvidenceAsync(EvidenceMatchInput input, CancellationToken ct) =>
        throw new NotSupportedException("Ollama workflow operations are not implemented in this milestone slice.");

    public Task<DraftGenerationResult> GenerateDraftAsync(DraftGenerationInput input, CancellationToken ct) =>
        throw new NotSupportedException("Ollama workflow operations are not implemented in this milestone slice.");

    public Task<ClaimAuditResult> AuditClaimsAsync(ClaimAuditInput input, CancellationToken ct) =>
        throw new NotSupportedException("Ollama workflow operations are not implemented in this milestone slice.");

    private AiDiagnosticsResult Unavailable(string message, IReadOnlyList<AiDiagnosticCheck> checks) =>
        new("Ollama", options.Model, options.Endpoint, false, message, checks);

    private async Task<string> GenerateAsync(string prompt, int attemptCount, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "/api/generate",
                new
                {
                    model = options.Model,
                    prompt,
                    stream = false,
                    format = "json"
                },
                JsonOptions,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new AiProviderUnavailableException(
                    $"Ollama returned {(int)response.StatusCode} {response.ReasonPhrase}.",
                    attemptCount);
            }

            OllamaGenerateResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(JsonOptions, ct);
            }
            catch (JsonException exception)
            {
                throw new AiInvalidOutputException("Ollama returned malformed generate JSON.", attemptCount, exception);
            }

            if (payload is null || string.IsNullOrWhiteSpace(payload.Response))
            {
                throw new AiInvalidOutputException("Ollama returned an empty response.", attemptCount);
            }

            return payload.Response;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new AiProviderUnavailableException("Ollama endpoint is unavailable.", attemptCount);
        }
        catch (HttpRequestException exception)
        {
            throw new AiProviderUnavailableException("Ollama endpoint is unavailable.", attemptCount, exception);
        }
    }

    private static JobAnalysisResult ParseJobAnalysis(string responseText, int attemptCount)
    {
        OllamaJobAnalysisResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OllamaJobAnalysisResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException("Ollama returned malformed job analysis JSON.", attemptCount, exception);
        }

        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.CompanyName) ||
            string.IsNullOrWhiteSpace(payload.RoleTitle) ||
            string.IsNullOrWhiteSpace(payload.DetectedLanguage) ||
            string.IsNullOrWhiteSpace(payload.SelectedLanguage) ||
            !IsSupportedLanguage(payload.DetectedLanguage) ||
            !IsSupportedLanguage(payload.SelectedLanguage) ||
            payload.JobSignals is null ||
            payload.JobSignals.RequiredSkills is null ||
            payload.JobSignals.PreferredSkills is null ||
            payload.JobSignals.Responsibilities is null ||
            payload.JobSignals.Signals is null)
        {
            throw new AiInvalidOutputException("Ollama returned structurally invalid job analysis JSON.", attemptCount);
        }

        var now = DateTimeOffset.UtcNow;
        if (payload.JobSignals.Signals.Any(signal =>
            signal is null ||
            string.IsNullOrWhiteSpace(signal.Id) ||
            string.IsNullOrWhiteSpace(signal.Label) ||
            string.IsNullOrWhiteSpace(signal.Category) ||
            !IsSupportedSignalCategory(signal.Category) ||
            signal.Keywords is null ||
            signal.Keywords.Count == 0 ||
            signal.Keywords.All(string.IsNullOrWhiteSpace)))
        {
            throw new AiInvalidOutputException("Ollama returned structurally invalid job signal JSON.", attemptCount);
        }

        var signals = payload.JobSignals.Signals
            .Select(signal => new JobSignal(
                signal.Id.Trim(),
                signal.Label.Trim(),
                signal.Category.Trim(),
                signal.Keywords.Where(keyword => !string.IsNullOrWhiteSpace(keyword)).Select(keyword => keyword.Trim()).ToList()))
            .ToList();

        if (signals.Count == 0)
        {
            throw new AiInvalidOutputException("Ollama returned job analysis without any job signals.", attemptCount);
        }

        var document = new JobSignalsDocument(
            "Ollama",
            now,
            payload.JobSignals.RequiredSkills.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList(),
            payload.JobSignals.PreferredSkills.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList(),
            payload.JobSignals.Responsibilities.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList(),
            signals);

        return new JobAnalysisResult(
            payload.CompanyName.Trim(),
            payload.RoleTitle.Trim(),
            NormalizeLanguage(payload.DetectedLanguage),
            NormalizeLanguage(payload.SelectedLanguage),
            document)
        {
            AttemptCount = attemptCount
        };
    }

    private static string BuildJobAnalysisPrompt(JobAnalysisInput input) =>
        $"""
        {JobAnalysisPrompt}

        Existing application metadata:
        Company name: {input.CompanyName}
        Role title: {input.RoleTitle}
        Selected language: {input.SelectedLanguage ?? "(none)"}

        Job posting:
        {input.JobPostingText}
        """;

    private static string BuildRepairPrompt(string invalidJson, string validationError) =>
        $"""
        Repair this job analysis JSON so it matches the required contract exactly.
        Return only strict JSON. Do not include markdown.

        Validation error:
        {validationError}

        Invalid JSON:
        {invalidJson}
        """;

    private static bool IsSupportedLanguage(string language) =>
        string.Equals(language.Trim(), "English", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(language.Trim(), "Danish", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeLanguage(string language) =>
        string.Equals(language.Trim(), "Danish", StringComparison.OrdinalIgnoreCase) ? "Danish" : "English";

    private static bool IsSupportedSignalCategory(string category) =>
        string.Equals(category.Trim(), "RequiredSkill", StringComparison.Ordinal) ||
        string.Equals(category.Trim(), "PreferredSkill", StringComparison.Ordinal) ||
        string.Equals(category.Trim(), "Responsibility", StringComparison.Ordinal);

    private static string LoadPrompt(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Ai", "Prompts", fileName);
        return File.ReadAllText(path);
    }

    private sealed record OllamaTagsResponse(IReadOnlyList<OllamaModel> Models);

    private sealed record OllamaModel(string Name);

    private sealed record OllamaGenerateResponse(string Response);

    private sealed record OllamaJobAnalysisResponse(
        string CompanyName,
        string RoleTitle,
        string DetectedLanguage,
        string SelectedLanguage,
        OllamaJobSignalsResponse JobSignals);

    private sealed record OllamaJobSignalsResponse(
        IReadOnlyList<string> RequiredSkills,
        IReadOnlyList<string> PreferredSkills,
        IReadOnlyList<string> Responsibilities,
        IReadOnlyList<OllamaJobSignalResponse> Signals);

    private sealed record OllamaJobSignalResponse(
        string Id,
        string Label,
        string Category,
        IReadOnlyList<string> Keywords);
}
