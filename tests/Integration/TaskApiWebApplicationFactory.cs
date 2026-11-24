using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using TaskTrackerApi.Features.Tasks.Services;

namespace TaskTrackerApi.IntegrationTests;

/// <summary>
/// Boots the real application pipeline in-memory (TestServer) and replaces ITaskService with a Moq
/// mock, so endpoint routing, validation, and the error-handling middleware can be exercised over
/// HTTP without a real SQL Server. The DbContext stays registered but is never used.
/// </summary>
public class TaskApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<ITaskService> TaskServiceMock { get; } = new();

    public TaskApiWebApplicationFactory()
    {
        // Program reads these from configuration before the host is built, so set them as environment
        // variables (bound to AppSettings). The connection string is a placeholder and never connects.
        Environment.SetEnvironmentVariable("Environment", "local");
        Environment.SetEnvironmentVariable(
            "SqlServerDbConnectionString",
            "Server=(localdb)\\MSSQLLocalDB;Database=TaskTrackerIntegrationTests;Trusted_Connection=True;");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITaskService>();
            services.AddScoped(_ => TaskServiceMock.Object);
        });
    }
}
