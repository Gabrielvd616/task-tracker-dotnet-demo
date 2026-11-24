using Microsoft.AspNetCore.HttpOverrides;
using TaskTrackerApi.Configuration;
using TaskTrackerApi.Features.Tasks.Services;
using TaskTrackerApi.Infrastructure.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable("Environment")
    ?? throw new InvalidOperationException("Environment is not configured.");
var isLocalEnvironment = string.Equals(environment, "local", StringComparison.OrdinalIgnoreCase);

// Locally, load settings from appsettings.Development.json; in hosted environments the host
// surfaces app settings as environment variables that bind to AppSettings. No Azure App Configuration is used.
if (isLocalEnvironment)
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
}

// Binds environment variables / configuration to AppSettings properties.
builder.Services.AddOptions<AppSettings>()
    .Configure(builder.Configuration.Bind)
    .ValidateDataAnnotations();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddEndpointModules();
builder.Services.ConfigureCors();

var app = builder.Build();

// Must be first so .NET knows Azure is forwarding HTTPS traffic
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Enforces secure connections before checking routes or CORS rules
if (!isLocalEnvironment && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Allows CORS and subsequent middleware to inspect route endpoint data
app.UseRouting();

// CORS runs before the exception handler so Access-Control-* headers are applied to every response,
// including error responses (otherwise a 500 reaches the browser as a misleading CORS failure).
app.UseCors();

// Centralized ProblemDetails handling for any unhandled exception.
app.UseProblemDetailsExceptionHandler();

// Fixed security headers (HSTS, X-Frame-Options, CSP) on every response.
app.UseSecurityHeaders();

// Configure the HTTP request pipeline.
if (isLocalEnvironment || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Diagnostic: proves which build is actually running. BuildTimeUtc changes on every publish, so if
// it doesn't change after a deploy, the running assembly was not replaced.
app.MapGet("/version", () =>
{
    var assembly = typeof(Program).Assembly;
    return Results.Ok(new
    {
        Application = "TaskTrackerApi",
        AssemblyVersion = assembly.GetName().Version?.ToString(),
        BuildTimeUtc = System.IO.File.GetLastWriteTimeUtc(assembly.Location)
    });
});

app.MapEndpointModules();

app.Run();

// Exposed for integration testing with WebApplicationFactory.
public partial class Program
{
}