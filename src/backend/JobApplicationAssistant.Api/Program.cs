using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton(_ =>
{
    var options = new AiOptions();
    builder.Configuration.GetSection("Ai").Bind(options);
    return options;
});
builder.Services.AddHttpClient<OllamaAiProvider>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<AiOptions>();
    client.BaseAddress = new Uri(options.Endpoint);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
});
builder.Services.AddSingleton<IAiProvider>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<AiOptions>();
    return string.Equals(options.Provider, "Ollama", StringComparison.OrdinalIgnoreCase)
        ? serviceProvider.GetRequiredService<OllamaAiProvider>()
        : new FakeAiProvider(new FakeAiProviderOptions(options.Model));
});
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

public partial class Program;
