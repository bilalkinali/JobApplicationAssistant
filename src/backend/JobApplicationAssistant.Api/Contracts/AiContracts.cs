namespace JobApplicationAssistant.Api.Contracts;

public sealed record AiProviderStatusResponse(
    string Provider,
    string Model,
    string? Endpoint,
    bool IsAvailable,
    string Message);

public sealed record AiDiagnosticsResponse(
    string Provider,
    string Model,
    string? Endpoint,
    bool IsAvailable,
    string Message,
    IReadOnlyList<AiDiagnosticCheckResponse> Checks);

public sealed record AiDiagnosticCheckResponse(
    string Name,
    string Status,
    string Message);
