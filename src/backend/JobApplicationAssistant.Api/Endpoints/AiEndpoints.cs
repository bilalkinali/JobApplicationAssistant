using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Contracts;

namespace JobApplicationAssistant.Api.Endpoints;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai");

        group.MapGet("/status", async Task<IResult> (IAiProvider provider, CancellationToken ct) =>
        {
            var status = await provider.GetStatusAsync(ct);
            return Results.Ok(ToResponse(status));
        });

        group.MapPost("/diagnostics", async Task<IResult> (IAiProvider provider, CancellationToken ct) =>
        {
            var diagnostics = await provider.RunDiagnosticsAsync(ct);
            return Results.Ok(ToResponse(diagnostics));
        });

        return app;
    }

    private static AiProviderStatusResponse ToResponse(AiProviderStatus status) =>
        new(status.Provider, status.Model, status.Endpoint, status.IsAvailable, status.Message);

    private static AiDiagnosticsResponse ToResponse(AiDiagnosticsResult diagnostics) =>
        new(
            diagnostics.Provider,
            diagnostics.Model,
            diagnostics.Endpoint,
            diagnostics.IsAvailable,
            diagnostics.Message,
            diagnostics.Checks.Select(check => new AiDiagnosticCheckResponse(check.Name, check.Status, check.Message)).ToList());
}
