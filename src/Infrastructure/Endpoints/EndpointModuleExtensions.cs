using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace TaskTrackerApi.Infrastructure.Endpoints;

/// <summary>
/// Registration and mapping helpers that replace the proprietary AddEndpointModules /
/// MapdEndpointModules extensions: discover every concrete <see cref="EndpointModule"/> in the
/// given assemblies, register them in DI, and map their routes during request-pipeline setup.
/// </summary>
public static class EndpointModuleExtensions
{
    public static IServiceCollection AddEndpointModules(this IServiceCollection services, params Assembly[] assemblies)
    {
        var scanned = assemblies.Length > 0 ? assemblies : new[] { Assembly.GetCallingAssembly() };

        var moduleTypes = scanned
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(EndpointModule).IsAssignableFrom(type)
                           && type is { IsAbstract: false, IsInterface: false }
                           && type.GetConstructor(Type.EmptyTypes) is not null);

        foreach (var moduleType in moduleTypes)
        {
            services.AddSingleton(typeof(EndpointModule), moduleType);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapEndpointModules(this IEndpointRouteBuilder endpoints)
    {
        var modules = endpoints.ServiceProvider.GetServices<EndpointModule>();

        foreach (var module in modules)
        {
            module.MapEndpoints(new ModuleEndpointRouteBuilder(endpoints, module));
        }

        return endpoints;
    }
}
