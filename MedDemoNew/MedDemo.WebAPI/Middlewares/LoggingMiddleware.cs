using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using MedDemo.Domain.Utilities;

namespace MedDemo.WebAPI.Middleware
{

    public sealed class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        private const int MaxLogContentLength = 10_000_000; // 10 MB
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsApiRequest(context))
            {
                await next(context);
                return;
            }

            var startTime = DateTime.UtcNow;
            var traceId = context.TraceIdentifier;

            context.Request.EnableBuffering();

            var requestBody = await ReadRequestBodyAsync(context);
            LogRequestToELK(context, requestBody, traceId);

            var originalBodyStream = context.Response.Body;
            await using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await next(context);

                var responseBodyText = await ReadResponseBodyAsync(responseBody);
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                LogResponseToELK(context, responseBodyText, traceId, duration);

                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private static bool IsApiRequest(HttpContext context)
        {
            var path = context.Request.Path.Value;
            return path?.StartsWith("/api", StringComparison.OrdinalIgnoreCase) == true
                || context.Request.Headers.Accept.Any(h =>
                    h?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
        }

        private static async Task<string> ReadRequestBodyAsync(HttpContext context)
        {
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(
                context.Request.Body,
                leaveOpen: true,
                bufferSize: 4096);

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            return body;
        }

        private static async Task<string> ReadResponseBodyAsync(MemoryStream responseBody)
        {
            responseBody.Position = 0;
            using var reader = new StreamReader(responseBody, leaveOpen: true);
            var text = await reader.ReadToEndAsync();
            responseBody.Position = 0;
            return text;
        }

        private void LogRequestToELK(HttpContext context, string requestBody, string traceId)
        {
            var logData = new
            {
                Timestamp = DateTime.UtcNow,
                TraceId = traceId,
                Type = "Request",
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                QueryString = context.Request.QueryString.Value,
                Headers = context.Request.Headers
                    .Where(h => !IsSensitiveHeader(h.Key))
                    .ToDictionary(h => h.Key, h => h.Value.ToString()),
                Body = TruncateIfNeeded(requestBody),
                ContentLength = requestBody.Length,
                ClientIP = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                Scheme = context.Request.Scheme,
                Host = context.Request.Host.Value
            };

            logger.LogInformation("HTTP Request: {RequestLog}", JsonSerializer.Serialize(logData, JsonOptions));
        }

        private void LogResponseToELK(HttpContext context, string responseBody, string traceId, double durationMs)
        {
            var logData = new
            {
                Timestamp = DateTime.UtcNow,
                TraceId = traceId,
                Type = "Response",
                StatusCode = context.Response.StatusCode,
                StatusDescription = GetStatusDescription(context.Response.StatusCode),
                Headers = context.Response.Headers
                    .Where(h => !IsSensitiveHeader(h.Key))
                    .ToDictionary(h => h.Key, h => h.Value.ToString()),
                Body = TruncateIfNeeded(responseBody),
                ContentLength = responseBody.Length,
                DurationMs = durationMs,
                ContentType = context.Response.ContentType
            };

            var logLevel = context.Response.StatusCode >= 500 ? LogLevel.Error :
                           context.Response.StatusCode >= 400 ? LogLevel.Warning :
                           LogLevel.Information;

            logger.Log(logLevel, "HTTP Response: {ResponseLog}", JsonSerializer.Serialize(logData, JsonOptions));
        }

        private static string TruncateIfNeeded(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            if (content.Length <= MaxLogContentLength)
                return content;

            return $"[TRUNCATED - Original size: {content.Length} bytes] {content[..1000]}...";
        }

        private static bool IsSensitiveHeader(string headerName)
        {
            return headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("X-API-Key", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetStatusDescription(int statusCode) => statusCode switch
        {
            200 => "OK",
            201 => "Created",
            204 => "NoContent",
            400 => "BadRequest",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "NotFound",
            500 => "InternalServerError",
            502 => "BadGateway",
            503 => "ServiceUnavailable",
            _ => "Unknown"
        };
    }
}