var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "JobApplicationAssistant.Api",
    status = "ok"
}));

app.Run();
