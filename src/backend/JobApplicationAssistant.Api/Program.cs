using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Endpoints;
using JobApplicationAssistant.Api.Imports;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton(_ =>
{
    var options = new AiOptions();
    builder.Configuration.GetSection("Ai").Bind(options);
    if (string.Equals(options.Provider, "OpenAiCompatible", StringComparison.OrdinalIgnoreCase))
    {
        options.Endpoint = OpenAiCompatibleAiProvider.NormalizeEndpoint(options.Endpoint);
    }

    return options;
});
builder.Services.AddHttpClient<OllamaAiProvider>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<AiOptions>();
    client.BaseAddress = new Uri(EnsureTrailingSlash(options.Endpoint));
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
});
builder.Services.AddHttpClient<OpenAiCompatibleAiProvider>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<AiOptions>();
    client.BaseAddress = new Uri(EnsureTrailingSlash(options.Endpoint));
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
});
builder.Services.AddSingleton<IAiProvider>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<AiOptions>();
    if (string.Equals(options.Provider, "Fake", StringComparison.OrdinalIgnoreCase))
    {
        return new FakeAiProvider(new FakeAiProviderOptions(options.Model));
    }

    if (string.Equals(options.Provider, "Ollama", StringComparison.OrdinalIgnoreCase))
    {
        return serviceProvider.GetRequiredService<OllamaAiProvider>();
    }

    if (string.Equals(options.Provider, "OpenAiCompatible", StringComparison.OrdinalIgnoreCase))
    {
        return serviceProvider.GetRequiredService<OpenAiCompatibleAiProvider>();
    }

    return new UnavailableAiProvider(options);
});
builder.Services.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ??
        [
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:5175",
            "http://127.0.0.1:5173",
            "http://127.0.0.1:5174",
            "http://127.0.0.1:5175",
            "http://[::1]:5173",
            "http://[::1]:5174",
            "http://[::1]:5175"
        ];

    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Ok(new
{
    service = "JobApplicationAssistant.Api",
    status = "ok"
}));

app.MapProfileEndpoints();
app.MapApplicationEndpoints();
app.MapAiEndpoints();

app.Run();

static string EnsureTrailingSlash(string endpoint) =>
    endpoint.EndsWith("/", StringComparison.Ordinal) ? endpoint : $"{endpoint}/";

public partial class Program;
