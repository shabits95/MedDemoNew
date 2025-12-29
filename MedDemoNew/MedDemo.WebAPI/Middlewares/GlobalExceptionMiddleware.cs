using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace MedDemo.WebAPI.Middleware;

public sealed class GlobalExceptionMiddleware(
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment environment) : IMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var (statusCode, problemDetails) = MapExceptionToProblemDetails(exception, traceId);

        // Log the exception with structured data for ELK
        LogExceptionToELK(exception, context, traceId, statusCode);

        // Set response
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions));
    }

    private (int StatusCode, ProblemDetails ProblemDetails) MapExceptionToProblemDetails(
        Exception exception,
        string traceId)
    {
        return exception switch
        {
            ValidationException validationEx => (
                StatusCode: (int)HttpStatusCode.BadRequest,
                ProblemDetails: CreateValidationProblemDetails(validationEx, traceId)
            ),

            ArgumentNullException or ArgumentException => (
                StatusCode: (int)HttpStatusCode.BadRequest,
                ProblemDetails: CreateProblemDetails(
                    "Bad Request",
                    "The request contains invalid parameters.",
                    (int)HttpStatusCode.BadRequest,
                    traceId,
                    environment.IsDevelopment() ? exception.Message : null
                )
            ),

            KeyNotFoundException => (
                StatusCode: (int)HttpStatusCode.NotFound,
                ProblemDetails: CreateProblemDetails(
                    "Not Found",
                    "The requested resource was not found.",
                    (int)HttpStatusCode.NotFound,
                    traceId
                )
            ),

            UnauthorizedAccessException => (
                StatusCode: (int)HttpStatusCode.Forbidden,
                ProblemDetails: CreateProblemDetails(
                    "Forbidden",
                    "You do not have permission to access this resource.",
                    (int)HttpStatusCode.Forbidden,
                    traceId
                )
            ),

            TimeoutException => (
                StatusCode: (int)HttpStatusCode.RequestTimeout,
                ProblemDetails: CreateProblemDetails(
                    "Request Timeout",
                    "The request took too long to process.",
                    (int)HttpStatusCode.RequestTimeout,
                    traceId
                )
            ),

            InvalidOperationException => (
                StatusCode: (int)HttpStatusCode.Conflict,
                ProblemDetails: CreateProblemDetails(
                    "Conflict",
                    "The request could not be completed due to a conflict.",
                    (int)HttpStatusCode.Conflict,
                    traceId,
                    environment.IsDevelopment() ? exception.Message : null
                )
            ),

            _ => (
                StatusCode: (int)HttpStatusCode.InternalServerError,
                ProblemDetails: CreateProblemDetails(
                    "Internal Server Error",
                    "An unexpected error occurred. Please try again later.",
                    (int)HttpStatusCode.InternalServerError,
                    traceId,
                    environment.IsDevelopment() ? exception.Message : null
                )
            )
        };
    }

    private static ProblemDetails CreateProblemDetails(
        string title,
        string detail,
        int status,
        string traceId,
        string? developerMessage = null)
    {
        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status,
            Instance = traceId
        };

        if (!string.IsNullOrEmpty(developerMessage))
        {
            problemDetails.Extensions["developerMessage"] = developerMessage;
        }

        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        return problemDetails;
    }

    private static ValidationProblemDetails CreateValidationProblemDetails(
        ValidationException validationException,
        string traceId)
    {
        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Status = (int)HttpStatusCode.BadRequest,
            Instance = traceId
        };

        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        return problemDetails;
    }

    private void LogExceptionToELK(
        Exception exception,
        HttpContext context,
        string traceId,
        int statusCode)
    {
        var logData = new
        {
            Timestamp = DateTime.UtcNow,
            TraceId = traceId,
            ExceptionType = exception.GetType().Name,
            Message = exception.Message,
            StackTrace = environment.IsDevelopment() ? exception.StackTrace : null,
            InnerException = exception.InnerException?.Message,
            StatusCode = statusCode,
            Path = context.Request.Path.Value,
            Method = context.Request.Method,
            QueryString = context.Request.QueryString.Value,
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            ClientIP = context.Connection.RemoteIpAddress?.ToString(),
            UserId = context.User?.Identity?.Name,
            Environment = environment.EnvironmentName
        };

        var logLevel = statusCode >= 500 ? LogLevel.Error : LogLevel.Warning;

        logger.Log(
            logLevel,
            exception,
            "Exception occurred: {ExceptionLog}",
            JsonSerializer.Serialize(logData, JsonOptions)
        );
    }
}