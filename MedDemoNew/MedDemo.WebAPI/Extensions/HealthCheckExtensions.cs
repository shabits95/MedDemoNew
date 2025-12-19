using HealthChecks.UI.Client;
using MedDemo.Application.Common;
using MedDemo.Domain.Constants;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MedDemo.WebAPI.Extensions;

/// <summary>
/// Provides extension methods for configuring health checks
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Registers health check services
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, AppSettings configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add health checks with tags for different purposes
        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: configuration.ConnectionStrings.DefaultConnection,
                name: HealthCheck.DBHealthCheck,
                failureStatus: HealthStatus.Unhealthy,
                tags: [HealthCheck.InfrastructureCheck, "ready", "db"]
            );

        // Add more health checks as needed (example)
        // .AddUrlGroup(new Uri("https://api.example.com"), name: "External API", tags: ["ready"])
        // .AddCheck<CustomHealthCheck>("custom-check", tags: ["ready"]);

        // Configure Health Check UI
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(30); // Evaluate every 30 seconds
            setup.MaximumHistoryEntriesPerEndpoint(50);
            setup.SetApiMaxActiveRequests(1);

            setup.AddHealthCheckEndpoint(
                "Application Health",
                $"{configuration.AppUrl}/healthz");
        }).AddPostgreSqlStorage(configuration.ConnectionStrings.DefaultConnection); // Use for production

        return services;
    }

    /// <summary>
    /// Maps health check endpoints
    /// </summary>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Liveness probe - always returns healthy (no dependencies checked)
        // Used by orchestrators to know if the app is running
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // No checks - just returns healthy if app is running
            AllowCachingResponses = false,
            ResponseWriter = (context, _) =>
            {
                context.Response.ContentType = "text/plain";
                return context.Response.WriteAsync("Healthy");
            }
        });

        // Readiness probe - checks dependencies
        // Used by orchestrators to know if the app can handle requests
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            AllowCachingResponses = false,
            ResponseWriter = (context, report) =>
            {
                context.Response.ContentType = "text/plain";
                return context.Response.WriteAsync(report.Status.ToString());
            }
        });

        // Public health endpoint - minimal info
        // For load balancers and external monitoring
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            AllowCachingResponses = false,
            ResponseWriter = (context, report) =>
            {
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow
                });
            }
        });

        // Detailed endpoint - IP restricted, comprehensive info
        app.MapHealthChecks("/health-detailed", new HealthCheckOptions
        {
            Predicate = _ => true,
            AllowCachingResponses = false,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var response = new
                {
                    status = report.Status.ToString(),
                    totalDuration = report.TotalDuration.ToString(),
                    timestamp = DateTime.UtcNow,
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description ?? "No description",
                        duration = entry.Value.Duration.ToString(),
                        exception = entry.Value.Exception?.Message,
                        data = entry.Value.Data.Count > 0 ? entry.Value.Data : null,
                        tags = entry.Value.Tags
                    })
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        });

        // UI endpoint - IP restricted
        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            Predicate = _ => true,
            AllowCachingResponses = false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Health Check UI dashboard - IP restricted
        app.UseHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui";
            options.ApiPath = "/health-ui-api";
            options.UseRelativeApiPath = false;
            options.UseRelativeResourcesPath = false;
        });

        return app;
    }
}