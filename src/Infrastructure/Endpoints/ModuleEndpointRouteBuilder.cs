using Microsoft.AspNetCore.Routing;

namespace TaskTrackerApi.Infrastructure.Endpoints;

internal sealed class ModuleEndpointRouteBuilder : IModuleEndpointRouteBuilder
{
    private readonly IEndpointRouteBuilder _endpoints;
    private readonly EndpointModule _module;

    public ModuleEndpointRouteBuilder(IEndpointRouteBuilder endpoints, EndpointModule module)
    {
        _endpoints = endpoints;
        _module = module;
    }

    public RouteHandlerBuilder MapGet(string pattern, Delegate handler)
        => Apply(_endpoints.MapGet(Combine(pattern), handler));

    public RouteHandlerBuilder MapPost(string pattern, Delegate handler)
        => Apply(_endpoints.MapPost(Combine(pattern), handler));

    public RouteHandlerBuilder MapPut(string pattern, Delegate handler)
        => Apply(_endpoints.MapPut(Combine(pattern), handler));

    public RouteHandlerBuilder MapDelete(string pattern, Delegate handler)
        => Apply(_endpoints.MapDelete(Combine(pattern), handler));

    private string Combine(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return _module.RoutePrefix;
        }

        var suffix = pattern.StartsWith('/') ? pattern : "/" + pattern;
        return $"{_module.RoutePrefix}{suffix}";
    }

    private RouteHandlerBuilder Apply(RouteHandlerBuilder builder)
    {
        // Group endpoints under an OpenAPI tag for UI grouping. Note: the ApiExplorer GroupName
        // (WithGroupName) selects the Swagger *document*, so setting it here would exclude the
        // endpoints from the default "v1" document — use tags instead.
        var tag = _module.GroupName ?? _module.Name;
        if (!string.IsNullOrEmpty(tag))
        {
            builder.WithTags(tag);
        }

        return builder;
    }
}
