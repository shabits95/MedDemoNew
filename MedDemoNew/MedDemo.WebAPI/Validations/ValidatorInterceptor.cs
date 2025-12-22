using System.Net;
using MedDemo.Domain.Constants;
using MedDemo.Application.DTO.Errors;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ValidationException = MedDemo.Application.Exceptions.ValidationException;

namespace MedDemo.Web.Validations;

public class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Validate each action parameter
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var argument) || argument == null)
                continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = _serviceProvider.GetService(validatorType) as IValidator;

            if (validator != null)
            {
                var validationContext = new ValidationContext<object>(argument);
                var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                if (!validationResult.IsValid)
                {
                    HandleValidationErrors(context, validationResult);
                    return; // Short-circuit the pipeline
                }
            }
        }

        // If validation passed, continue with the action execution
        await next();
    }

    private void HandleValidationErrors(ActionExecutingContext context, ValidationResult result)
    {
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        List<Error> errors = new();

        foreach (var error in result.Errors)
        {
            var validationError = new Error(
                $"{ApplicationConstants.Name}.{ErrorRespondCode.BAD_REQUEST}",
                error.ErrorMessage
            );

            validationError.AddErrorProperty(
                new ErrorProperty(
                    error.PropertyName,
                    error.AttemptedValue?.ToString() ?? "null"
                )
            );

            errors.Add(validationError);
        }

        var errorResponse = new ErrorResponse(errors);

        // Add to ModelState
        context.ModelState.TryAddModelException(
            ApplicationConstants.FluentValidationErrorKey,
            new ValidationException(errorResponse)
        );

        // Set the result to return BadRequest
        context.Result = new BadRequestObjectResult(context.ModelState);
    }
}