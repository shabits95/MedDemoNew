namespace MedDemo.WebAPI.Middleware;

/// <summary>
/// Middleware to restrict access to health check endpoints based on IP addresses
/// </summary>
public sealed class HealthCheckIPRestrictionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedIPs;
    private static readonly PathString HealthUiPath = new("/health-ui");
    private static readonly PathString HealthDetailedPath = new("/health-detailed");
    private static readonly PathString HealthUiApiPath = new("/health-ui-api");

    public HealthCheckIPRestrictionMiddleware(RequestDelegate next, string[] allowedIPs)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _allowedIPs = allowedIPs?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? throw new ArgumentNullException(nameof(allowedIPs));

        // Add localhost IPs by default
        _allowedIPs.Add("::1");
        _allowedIPs.Add("127.0.0.1");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (IsProtectedEndpoint(context.Request.Path))
        {
            var remoteIp = GetClientIpAddress(context);

            if (string.IsNullOrEmpty(remoteIp) || !_allowedIPs.Contains(remoteIp))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Access denied",
                    message = "Your IP address is not authorized to access this endpoint"
                });
                return;
            }
        }

        await _next(context);
    }

    private static bool IsProtectedEndpoint(PathString path)
    {
        return path.StartsWithSegments(HealthUiPath) ||
               path.StartsWithSegments(HealthDetailedPath) ||
               path.StartsWithSegments(HealthUiApiPath);
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (for reverse proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Fallback to remote IP
        return context.Connection.RemoteIpAddress?.ToString();
    }
}