using Microsoft.AspNetCore.Diagnostics;
using MedDemo.Application.Exceptions;
using MedDemo.Domain.Constants;
using MedDemo.Domain.Enums;
//using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace MedDemo.WebAPI.Extensions;

/// <summary>
/// Provides global exception handling middleware configuration.
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    private const string ContentType = "application/problem+json";
    private const int MaxErrorMessageLength = 500;

    /// <summary>
    /// Configures global exception handler with structured error responses.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(
        this IApplicationBuilder app,
        ILogger logger,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(environment);

        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            AllowStatusCode404Response = true,
            ExceptionHandler = async context =>
            {
                await HandleExceptionAsync(context, logger, environment);
            }
        });

        return app;
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        ILogger logger,
        IHostEnvironment environment)
    {
        var errorId = Guid.NewGuid();
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.ContentType = ContentType;

        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature?.Error is null)
        {
            return;
        }

        var exception = exceptionFeature.Error;
        var (statusCode, errorCode, errorMessage) = MapExceptionToResponse(exception, environment);

        context.Response.StatusCode = statusCode;

        // Security: Sanitize error message to prevent information leakage
        var sanitizedMessage = SanitizeErrorMessage(errorMessage, environment);

        var errorResponse = CreateErrorResponse(
            errorCode,
            sanitizedMessage,
            errorId,
            traceId,
            context.Request.Path,
            environment);

        // Log the exception with full details
        LogException(logger, exception, errorId, traceId, statusCode, context);

        await WriteJsonResponseAsync(context.Response, errorResponse);
    }

    private static (int StatusCode, string ErrorCode, string Message) MapExceptionToResponse(
        Exception exception,
        IHostEnvironment environment)
    {
        return exception switch
        {
            UserFriendlyException ufe => MapUserFriendlyException(ufe),
            ValidationException ve => (
                (int)HttpStatusCode.BadRequest,
                $"{ApplicationConstants.Name}.{ErrorRespondCode.BAD_REQUEST}",
                ve.Message),
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                $"{ApplicationConstants.Name}.{ErrorRespondCode.UNAUTHORIZED}",
                "Unauthorized access"),
            ArgumentException ae => (
                (int)HttpStatusCode.BadRequest,
                $"{ApplicationConstants.Name}.{ErrorRespondCode.BAD_REQUEST}",
                ae.Message),
            KeyNotFoundException => (
                (int)HttpStatusCode.NotFound,
                $"{ApplicationConstants.Name}.{ErrorRespondCode.NOT_FOUND}",
                "Resource not found"),
            TimeoutException => (
                (int)HttpStatusCode.RequestTimeout,
                $"{ApplicationConstants.Name}.{ErrorRespondCode.TIMEOUT}",
                "Request timeout"),
            OperationCanceledException => (
                (int)HttpStatusCode.RequestTimeout,
                $"{ApplicationConstants.Name}.{ErrorRespondCode.REQUEST_CANCELLED}",
                "Request was cancelled"),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                $"{ApplicationConstants.Name}.{ErrorRespondCode.INTERNAL_ERROR}",
                environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred")
        };
    }

    private static (int StatusCode, string ErrorCode, string Message) MapUserFriendlyException(
        UserFriendlyException exception)
    {
        var errorCode = exception.ErrorCode switch
        {
            ErrorCode.NotFound => (
                (int)HttpStatusCode.NotFound,
                ErrorRespondCode.NOT_FOUND),
            ErrorCode.VersionConflict => (
                (int)HttpStatusCode.Conflict,
                ErrorRespondCode.VERSION_CONFLICT),
            ErrorCode.ItemAlreadyExists => (
                (int)HttpStatusCode.Conflict,
                ErrorRespondCode.ITEM_ALREADY_EXISTS),
            ErrorCode.Conflict => (
                (int)HttpStatusCode.Conflict,
                ErrorRespondCode.CONFLICT),
            ErrorCode.BadRequest => (
                (int)HttpStatusCode.BadRequest,
                ErrorRespondCode.BAD_REQUEST),
            ErrorCode.Unauthorized => (
                (int)HttpStatusCode.Unauthorized,
                ErrorRespondCode.UNAUTHORIZED),
            ErrorCode.Forbidden => (
                (int)HttpStatusCode.Forbidden,
                ErrorRespondCode.FORBIDDEN),
            ErrorCode.UnprocessableEntity => (
                (int)HttpStatusCode.UnprocessableEntity,
                ErrorRespondCode.UNPROCESSABLE_ENTITY),
            ErrorCode.Internal => (
                (int)HttpStatusCode.InternalServerError,
                ErrorRespondCode.INTERNAL_ERROR),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                ErrorRespondCode.GENERAL_ERROR)
        };

        return (
            errorCode.Item1,
            $"{ApplicationConstants.Name}.{errorCode.Item2}",
            exception.UserFriendlyMessage);
    }

    private static string SanitizeErrorMessage(string message, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "An error occurred";
        }

        // In production, limit message length and remove sensitive patterns
        if (!environment.IsDevelopment())
        {
            message = message.Length > MaxErrorMessageLength
                ? message[..MaxErrorMessageLength] + "..."
                : message;

            // Remove potential file paths
            message = System.Text.RegularExpressions.Regex.Replace(
                message,
                @"[A-Za-z]:\\[\w\\\.\-]+",
                "[path]");

            // Remove stack trace information if accidentally included
            if (message.Contains("at ", StringComparison.OrdinalIgnoreCase))
            {
                var atIndex = message.IndexOf("at ", StringComparison.OrdinalIgnoreCase);
                message = message[..atIndex].TrimEnd();
            }
        }

        return message;
    }

    private static object CreateErrorResponse(
        string errorCode,
        string message,
        Guid errorId,
        string traceId,
        PathString path,
        IHostEnvironment environment)
    {
        var response = new Dictionary<string, object>
        {
            ["type"] = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            ["title"] = "An error occurred",
            ["status"] = GetStatusCodeFromErrorCode(errorCode),
            ["errorCode"] = errorCode,
            ["message"] = message,
            ["errorId"] = errorId,
            ["traceId"] = traceId,
            ["instance"] = path.Value ?? "/"
        };

        // Security: Only include timestamp in response, not full stack trace
        response["timestamp"] = DateTime.UtcNow;

        // Development only: Include additional debug information
        if (environment.IsDevelopment())
        {
            response["environment"] = "Development";
        }

        return response;
    }

    private static int GetStatusCodeFromErrorCode(string errorCode)
    {
        return errorCode switch
        {
            var code when code.Contains("NOT_FOUND") => 404,
            var code when code.Contains("BAD_REQUEST") => 400,
            var code when code.Contains("UNAUTHORIZED") => 401,
            var code when code.Contains("FORBIDDEN") => 403,
            var code when code.Contains("CONFLICT") => 409,
            var code when code.Contains("UNPROCESSABLE") => 422,
            var code when code.Contains("TIMEOUT") => 408,
            _ => 500
        };
    }

    private static void LogException(
        ILogger logger,
        Exception exception,
        Guid errorId,
        string traceId,
        int statusCode,
        HttpContext context)
    {
        var logLevel = statusCode >= 500 ? LogLevel.Error : LogLevel.Warning;

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["ErrorId"] = errorId,
            ["TraceId"] = traceId,
            ["StatusCode"] = statusCode,
            ["Path"] = context.Request.Path.Value ?? "/",
            ["Method"] = context.Request.Method,
            ["UserAgent"] = context.Request.Headers.UserAgent.ToString(),
            ["RemoteIp"] = GetClientIpAddress(context)
        });

        logger.Log(
            logLevel,
            exception,
            "Unhandled exception occurred. ErrorId: {ErrorId}, TraceId: {TraceId}, StatusCode: {StatusCode}, Path: {Path}",
            errorId,
            traceId,
            statusCode,
            context.Request.Path);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Security: Get real client IP considering proxies
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private static async Task WriteJsonResponseAsync(HttpResponse response, object data)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        try
        {
            await response.WriteAsJsonAsync(data, options);
        }
        catch (Exception ex)
        {
            // Fallback if JSON serialization fails
            await response.WriteAsync($"{{\"errorCode\":\"SERIALIZATION_ERROR\",\"message\":\"Failed to serialize error response\",\"details\":\"{ex.Message}\"}}");
        }
    }
}
