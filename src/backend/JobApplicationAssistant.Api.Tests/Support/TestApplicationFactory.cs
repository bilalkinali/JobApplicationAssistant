using JobApplicationAssistant.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JobApplicationAssistant.Api.Tests.Support;

public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? configureServices;

    public TestApplicationFactory(Action<IServiceCollection>? configureServices = null)
    {
        this.configureServices = configureServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var databaseName = $"job-application-assistant-{Guid.NewGuid():N}";

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            configureServices?.Invoke(services);
        });
    }
}
