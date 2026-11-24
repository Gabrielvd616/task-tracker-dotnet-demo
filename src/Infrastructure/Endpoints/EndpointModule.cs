namespace TaskTrackerApi.Infrastructure.Endpoints;

/// <summary>
/// Base class for self-contained endpoint modules. Mirrors the shape of the
/// proprietary AvidXchange.Enterprise.AspNetCore.MinimalApi EndpointModule so feature
/// modules can group a route prefix and metadata, then register their own routes.
/// </summary>
public abstract class EndpointModule
{
    protected EndpointModule(string routePrefix)
    {
        RoutePrefix = routePrefix;
    }

    public string RoutePrefix { get; }
    public string? Name { get; private set; }
    public string? GroupName { get; private set; }
    public string? Description { get; private set; }
    public string? Summary { get; private set; }
    public double? ApiVersion { get; private set; }

    protected void HasApiVersion(double version) => ApiVersion = version;
    protected void WithName(string name) => Name = name;
    protected void WithGroupName(string groupName) => GroupName = groupName;
    protected void WithDescription(string description) => Description = description;
    protected void WithSummary(string summary) => Summary = summary;

    /// <summary>Registers the module's routes. All patterns are relative to <see cref="RoutePrefix"/>.</summary>
    public abstract void MapEndpoints(IModuleEndpointRouteBuilder endpoints);
}
