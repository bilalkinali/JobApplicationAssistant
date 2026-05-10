using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
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
