namespace JobApplicationAssistant.Api.Ai;

public sealed class AiOptions
{
    public string Provider { get; set; } = "Fake";

    public string Endpoint { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "fake-deterministic";

    public int TimeoutSeconds { get; set; } = 120;

    public bool StoreRawPayloads { get; set; }
}
