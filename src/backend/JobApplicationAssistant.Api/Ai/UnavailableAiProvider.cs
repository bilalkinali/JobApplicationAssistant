namespace JobApplicationAssistant.Api.Ai;

public sealed class UnavailableAiProvider : IAiProvider
{
    private readonly AiOptions options;

    public UnavailableAiProvider(AiOptions options)
    {
        this.options = options;
    }

    public Task<AiProviderStatus> GetStatusAsync(CancellationToken ct) =>
        Task.FromResult(new AiProviderStatus(
            options.Provider,
            options.Model,
            options.Endpoint,
            false,
            UnsupportedProviderMessage()));

    public Task<AiDiagnosticsResult> RunDiagnosticsAsync(CancellationToken ct) =>
        Task.FromResult(new AiDiagnosticsResult(
            options.Provider,
            options.Model,
            options.Endpoint,
            false,
            UnsupportedProviderMessage(),
            [new AiDiagnosticCheck("provider", "unsupported", "Configure Ai:Provider as Fake or Ollama.")]));

    public Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisInput input, CancellationToken ct) =>
        throw ProviderUnavailable();

    public Task<EvidenceMatchResult> MatchEvidenceAsync(EvidenceMatchInput input, CancellationToken ct) =>
        throw ProviderUnavailable();

    public Task<DraftGenerationResult> GenerateDraftAsync(DraftGenerationInput input, CancellationToken ct) =>
        throw ProviderUnavailable();

    public Task<ClaimAuditResult> AuditClaimsAsync(ClaimAuditInput input, CancellationToken ct) =>
        throw ProviderUnavailable();

    private AiProviderUnavailableException ProviderUnavailable() =>
        new(UnsupportedProviderMessage());

    private string UnsupportedProviderMessage() =>
        $"AI provider '{options.Provider}' is not supported. Configure Ai:Provider as Fake or Ollama.";
}
