using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace MedDemo.WebAPI.SchemaFilter;

/// <summary>
/// Document filter to add health check endpoints to Swagger documentation.
/// </summary>
public sealed class HealthChecksFilter : IDocumentFilter
{
    private const string SyntheticCheckPath = "/synthetic-check";
    private const string HealthCheckPath = "/health";

    public void Apply(OpenApiDocument openApiDocument, DocumentFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(openApiDocument);
        ArgumentNullException.ThrowIfNull(context);

        AddSyntheticHealthCheckEndpoint(openApiDocument);
        AddHealthCheckEndpoint(openApiDocument);
    }

    private static void AddSyntheticHealthCheckEndpoint(OpenApiDocument document)
    {
        var pathItem = CreateInstance("OpenApiPathItem");
        var operations = CreateInstance("Dictionary`2",
            new[] { typeof(HttpMethod), GetOpenApiType("OpenApiOperation") });

        var operation = CreateInstance("OpenApiOperation");
        SetProperty(operation, "Summary", "Synthetic health check");
        SetProperty(operation, "Description", "Displays the application's health status for monitoring systems.");
        SetProperty(operation, "OperationId", "GetSyntheticHealthCheck");

        var responses = CreateInstance("OpenApiResponses");
        var response200 = CreateInstance("OpenApiResponse");
        SetProperty(response200, "Description", "Application is healthy");

        var response503 = CreateInstance("OpenApiResponse");
        SetProperty(response503, "Description", "Application is unhealthy");

        AddToDictionary(responses, "200", response200);
        AddToDictionary(responses, "503", response503);

        SetProperty(operation, "Responses", responses);
        SetProperty(operation, "Security", CreateInstance("List`1", new[] { GetOpenApiType("OpenApiSecurityRequirement") }));

        AddToDictionary(operations, HttpMethod.Get, operation);
        SetProperty(pathItem, "Operations", operations);

        document.Paths.TryAdd(SyntheticCheckPath, pathItem);
    }

    private static void AddHealthCheckEndpoint(OpenApiDocument document)
    {
        var pathItem = CreateInstance("OpenApiPathItem");
        var operations = CreateInstance("Dictionary`2",
            new[] { typeof(HttpMethod), GetOpenApiType("OpenApiOperation") });

        var operation = CreateInstance("OpenApiOperation");
        SetProperty(operation, "Summary", "Health check endpoint");
        SetProperty(operation, "Description", "Returns a plain text health status for basic availability monitoring.");
        SetProperty(operation, "OperationId", "GetHealthCheck");

        var responses = CreateInstance("OpenApiResponses");
        var response200 = CreateInstance("OpenApiResponse");
        SetProperty(response200, "Description", "Application is responsive");

        AddToDictionary(responses, "200", response200);

        SetProperty(operation, "Responses", responses);
        SetProperty(operation, "Security", CreateInstance("List`1", new[] { GetOpenApiType("OpenApiSecurityRequirement") }));

        AddToDictionary(operations, HttpMethod.Get, operation);
        SetProperty(pathItem, "Operations", operations);

        document.Paths.TryAdd(HealthCheckPath, pathItem);
    }

    private static Type GetOpenApiType(string typeName)
    {
        var assembly = typeof(OpenApiDocument).Assembly;
        return assembly.GetTypes()
            .FirstOrDefault(t => t.Name == typeName && t.IsPublic)
            ?? throw new InvalidOperationException($"Type {typeName} not found");
    }

    private static dynamic CreateInstance(string typeName, Type[]? genericArgs = null)
    {
        if (genericArgs != null)
        {
            var genericType = Type.GetType($"System.Collections.Generic.{typeName}")
                ?? throw new InvalidOperationException($"Generic type {typeName} not found");
            var constructed = genericType.MakeGenericType(genericArgs);
            return Activator.CreateInstance(constructed)!;
        }

        var type = GetOpenApiType(typeName);
        return Activator.CreateInstance(type)!;
    }

    private static void SetProperty(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property?.SetValue(obj, value);
    }

    private static void AddToDictionary(dynamic dict, object key, object value)
    {
        dict.Add(key, value);
    }
}