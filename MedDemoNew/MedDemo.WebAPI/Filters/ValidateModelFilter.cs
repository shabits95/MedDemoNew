using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MedDemo.Application.DTO.Errors;
using MedDemo.Application.Exceptions;
using MedDemo.Domain.Constants;
using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MedDemo.WebAPI.SchemaFilter;

/// <summary>
/// Action filter that validates model state and handles both FluentValidation 
/// and standard model validation errors with consistent error responses.
/// </summary>
public sealed class ValidateModelFilter : IActionFilter
{
    private const int MaxErrorsToReport = 100;
    private const int MaxErrorMessageLength = 500;

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No post-execution logic required
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.ModelState.IsValid)
        {
            // Priority 1: Check for FluentValidation errors
            if (TryHandleFluentValidationErrors(context))
            {
                return;
            }

            // Priority 2: Handle standard model validation errors
            HandleModelValidationErrors(context);
        }
    }

    private static bool TryHandleFluentValidationErrors(ActionExecutingContext context)
    {
        if (!context.ModelState.TryGetValue(
            ApplicationConstants.FluentValidationErrorKey,
            out var fluentErrorEntry))
        {
            return false;
        }

        if (!fluentErrorEntry.Errors.Any())
        {
            return false;
        }

        var firstError = fluentErrorEntry.Errors.First();

        if (firstError.Exception is not ValidationException validationException)
        {
            return false;
        }

        // Security: Ensure error response doesn't expose sensitive information
        var sanitizedResponse = SanitizeErrorResponse(validationException.ErrorResponse);

        context.Result = new BadRequestObjectResult(sanitizedResponse)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };

        return true;
    }

    private static void HandleModelValidationErrors(ActionExecutingContext context)
    {
        var errors = context.ModelState
            .Where(ms => ms.Value?.Errors.Count > 0)
            .Take(MaxErrorsToReport) // Security: Prevent excessive error enumeration
            .SelectMany(ms => ms.Value!.Errors.Select(error => CreateError(ms.Key, error, ms.Value)))
            .ToList();

        var errorResponse = new ErrorResponse
        {
            Errors = errors
        };

        context.Result = new BadRequestObjectResult(errorResponse)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }

    private static Error CreateError(string key, ModelError error, ModelStateEntry entry)
    {
        // Security: Sanitize error messages to prevent information leakage
        var sanitizedMessage = SanitizeErrorMessage(error.ErrorMessage);
        var sanitizedKey = SanitizePropertyName(key);

        return new Error(
            $"{ApplicationConstants.Name}.{ErrorRespondCode.BAD_REQUEST}",
            sanitizedMessage)
        {
            Property = sanitizedKey
        };
    }

    private static string SanitizeErrorMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Validation error occurred";
        }

        // Truncate excessively long messages to prevent log flooding
        var sanitized = message.Length > MaxErrorMessageLength
            ? message[..MaxErrorMessageLength] + "..."
            : message;

        // Remove potential sensitive patterns (adjust based on your needs)
        sanitized = RemoveSensitivePatterns(sanitized);

        return sanitized;
    }

    private static string SanitizePropertyName(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "Unknown";
        }

        // Remove array indices that might leak information about data structure
        // Example: "Users[5].Email" -> "Users.Email"
        return System.Text.RegularExpressions.Regex.Replace(
            propertyName,
            @"\[\d+\]",
            string.Empty);
    }

    private static string RemoveSensitivePatterns(string message)
    {
        // Remove potential file paths
        message = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"[A-Za-z]:\\[\w\\\.\-]+",
            "[path]");

        // Remove potential connection strings
        message = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"(Server|Data Source|Password|PWD)=[^;]+",
            "[redacted]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return message;
    }

    private static ErrorResponse SanitizeErrorResponse(ErrorResponse errorResponse)
    {
        ArgumentNullException.ThrowIfNull(errorResponse);

        // Limit number of errors to prevent response bloat
        if (errorResponse.Errors?.Count > MaxErrorsToReport)
        {
            errorResponse.Errors = errorResponse.Errors
                .Take(MaxErrorsToReport)
                .ToList();
        }

        // Sanitize each error message
        if (errorResponse.Errors != null)
        {
            foreach (var error in errorResponse.Errors)
            {
                if (error.Message != null)
                {
                    error.Message = SanitizeErrorMessage(error.Message);
                }

                if (error.Property != null)
                {
                    error.Property = SanitizePropertyName(error.Property);
                }
            }
        }

        return errorResponse;
    }
}