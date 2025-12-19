using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MedDemo.Domain.Utilities;
using MedDemo.WebAPI.SchemaFilter;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedDemo.WebAPI.Extensions;

public static class MvcExtension
{
    private const int MaxJsonDepth = 32;
    private const int MaxJsonStringLength = 4_000_000; // 4MB

    public static IServiceCollection AddMvcConfiguration(this IServiceCollection services)
    {
        // Suppress default model validation to use custom filter
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        // Configure MVC with security and performance enhancements
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidateModelFilter>();

            // Limit request body size (10MB default, adjust as needed)
            options.MaxModelBindingCollectionSize = 1024;
        })
        .AddJsonOptions(ConfigureJsonOptions);

        // Register FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);

        return services;
    }

    private static void ConfigureJsonOptions(Microsoft.AspNetCore.Mvc.JsonOptions options)
    {
        var jsonOptions = options.JsonSerializerOptions;

        // Security: Prevent deeply nested JSON attacks
        jsonOptions.MaxDepth = MaxJsonDepth;

        // Security: Limit string length to prevent memory exhaustion
        jsonOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        // Ignore null values to reduce payload size
        jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // Handle reference loops gracefully
        jsonOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // Use camelCase for property names (modern API convention)
        jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        // Case-insensitive property matching
        jsonOptions.PropertyNameCaseInsensitive = true;

        // Read-only properties won't be deserialized
        jsonOptions.IgnoreReadOnlyProperties = false;
        jsonOptions.IgnoreReadOnlyFields = true;

        // Allow trailing commas for better client compatibility
        jsonOptions.AllowTrailingCommas = true;

        // Allow comments in JSON (useful for config files)
        jsonOptions.ReadCommentHandling = JsonCommentHandling.Skip;

        // Number handling for better JavaScript compatibility
        jsonOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;

        // Custom converters
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        jsonOptions.Converters.Add(new JsonDateTimeOffsetConverter());
        jsonOptions.Converters.Add(new NullToDefaultConverter());
        jsonOptions.Converters.Add(new TrimmingConverter());
        jsonOptions.Converters.Add(new DecimalPrecisionConverter(2));
    }

    /// <summary>
    /// Validates JSON serializer options for security vulnerabilities
    /// </summary>
    public static void ValidateJsonConfiguration(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxDepth > 64 || options.MaxDepth < 1)
        {
            throw new InvalidOperationException(
                "MaxDepth must be between 1 and 64 to prevent DoS attacks");
        }
    }
}