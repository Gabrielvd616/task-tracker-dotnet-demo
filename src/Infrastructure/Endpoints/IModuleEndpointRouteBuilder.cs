namespace TaskTrackerApi.Infrastructure.Endpoints;

/// <summary>
/// Minimal-API route builder scoped to a single <see cref="EndpointModule"/>. Each map call
/// automatically prepends the module's route prefix and applies its shared metadata, so modules
/// declare routes relative to their prefix (the group behaviour provided by MapGroup on .NET 7+).
/// </summary>
public interface IModuleEndpointRouteBuilder
{
    RouteHandlerBuilder MapGet(string pattern, Delegate handler);
    RouteHandlerBuilder MapPost(string pattern, Delegate handler);
    RouteHandlerBuilder MapPut(string pattern, Delegate handler);
    RouteHandlerBuilder MapDelete(string pattern, Delegate handler);
}
