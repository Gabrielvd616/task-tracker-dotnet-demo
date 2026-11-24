using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TaskTrackerApi.Features.Tasks;
using TaskTrackerApi.Features.Tasks.Services;
using TaskTrackerApi.Infrastructure.Endpoints;
using Xunit;

namespace TaskTrackerApi.UnitTests.Infrastructure.Endpoints;

public class EndpointModuleExtensionsTests
{
    [Fact]
    public void AddEndpointModules_DiscoversConcreteModules()
    {
        var services = new ServiceCollection();

        services.AddEndpointModules(typeof(TaskEndpointModule).Assembly);

        var provider = services.BuildServiceProvider();
        provider.GetServices<EndpointModule>().Should().ContainSingle(module => module is TaskEndpointModule);
    }

    [Fact]
    public void MapEndpointModules_MapsRoutesUnderModulePrefix()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped(_ => new Mock<ITaskService>().Object);
        builder.Services.AddEndpointModules(typeof(TaskEndpointModule).Assembly);

        var app = builder.Build();
        app.MapEndpointModules();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToList();

        routes.Should().Contain("/tasks");
        routes.Should().Contain("/tasks/{id:int}");
    }
}
