using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTrackerApi.Infrastructure.Data;

namespace TaskTrackerApi.Configuration;

public static class StartupExtensions
{
    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration (including environment variables) is bound to AppSettings, mirroring the
        // PaymentLifecycleOrchestrator configuration approach. No Azure App Configuration is used.
        var appSettings = new AppSettings();
        configuration.Bind(appSettings);

        var connectionString = appSettings.SqlServerDbConnectionString
            ?? throw new InvalidOperationException("SqlServerDbConnectionString is not configured.");

        services.AddDbContext<TaskDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServerOptions =>
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(1),
                    errorNumbersToAdd: null)));

        return services;
    }

    /// <summary>
    /// Centralized exception handling: converts any unhandled exception into a sanitized
    /// RFC 7807 ProblemDetails 500 response and logs the underlying error. Endpoints may still
    /// catch specific exceptions to return a more precise status before reaching this handler.
    /// </summary>
    public static WebApplication UseProblemDetailsExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("UnhandledException");
                logger.LogError(error, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred.",
                    Instance = context.Request.Path
                };

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                // Pass the content type explicitly; WriteAsJsonAsync otherwise forces application/json.
                await context.Response.WriteAsJsonAsync(
                    problem, options: null, contentType: "application/problem+json");
            });
        });

        return app;
    }

    /// <summary>
    /// CORS policy: the allowed origins with credentials. Any header/method is permitted (still
    /// compatible with credentials because the origins are explicit), which avoids preflight
    /// rejections when the client sends a header not in a fixed allow-list.
    /// </summary>
    public static IServiceCollection ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy
                    // Browsers send Origin without a trailing slash, so the trailing slash in the
                    // config is dropped here for the match (and Allow-Credentials) to work.
                    .WithOrigins(
                        "https://goldfish-app-zhi87.ondigitalocean.app"
                        // , "http://localhost:4200" // Can be uncommented locally for development
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        return services;
    }

    /// <summary>
    /// Adds the fixed security response headers (HSTS, X-Frame-Options, CSP) to every response.
    /// </summary>
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["Strict-Transport-Security"] = "max-age=100000";
            headers["X-Frame-Options"] = "DENY";
            headers["Content-Security-Policy"] = "frame-ancestors 'none'";
            await next();
        });

        return app;
    }
}
