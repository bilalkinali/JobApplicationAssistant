using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IAiProvider, FakeAiProvider>();
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

app.Run();

public partial class Program;
