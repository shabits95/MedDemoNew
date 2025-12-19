using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MedDemo.WebAPI.Middleware;

/// <summary>
/// Middleware for measuring and logging HTTP request performance metrics.
/// </summary>
public sealed partial class PerformanceMiddleware : IMiddleware
{
    private readonly ILogger<PerformanceMiddleware> _logger;

    // Use ValueStopwatch for better performance (struct, no allocations)
    // Or ObjectPool for Stopwatch reuse if needed
    public PerformanceMiddleware(ILogger<PerformanceMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var timestamp = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(timestamp);

            // Use structured logging with LoggerMessage source generation for better performance
            LogRequestPerformance(
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds
            );
        }
    }

    // High-performance logging using source generation (compile-time)
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:0.0000}ms")]
    private partial void LogRequestPerformance(
        string method,
        string path,
        int statusCode,
        double elapsedMs);
}

// Extension method for clean registration
public static class PerformanceMiddlewareExtensions
{
    public static IServiceCollection AddPerformanceMiddleware(this IServiceCollection services)
    {
        return services.AddScoped<PerformanceMiddleware>();
    }

    public static IApplicationBuilder UsePerformanceMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PerformanceMiddleware>();
    }
}