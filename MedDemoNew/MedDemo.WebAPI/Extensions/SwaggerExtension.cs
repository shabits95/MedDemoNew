using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.OpenApi;
using MedDemo.Application.DTO;
using MedDemo.WebAPI.SchemaFilter;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MedDemo.WebAPI.Extensions;

/// <summary>
/// Extension methods for configuring Swagger/OpenAPI documentation
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Adds Swagger/OpenAPI services to the application
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(appSettings);

        services.AddFluentValidationRulesToSwagger();

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            // API Information
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = appSettings.ApplicationDetail.ApplicationName,
                Version = "v1",
                Description = appSettings.ApplicationDetail.Description,
                Contact = new OpenApiContact
                {
                    Name = "Shabi T S",
                    Email = "shabits95@gmail.com",
                    Url = new Uri(appSettings.ApplicationDetail.ContactWebsite)
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Include XML comments for better documentation
            IncludeXmlComments(options);

            // JWT Bearer Authentication
            ConfigureJwtAuthentication(options);

            // Custom filters
            options.DocumentFilter<HealthChecksFilter>();

            // Enable Swagger annotations
            options.EnableAnnotations();

            // Order actions by their relative path
            options.OrderActionsBy(apiDesc => apiDesc.RelativePath);

            // Custom schema IDs to avoid conflicts
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
        });

        return services;
    }

    /// <summary>
    /// Configures Swagger UI middleware
    /// </summary>
    public static WebApplication UseSwaggerDocumentation(this WebApplication app, AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(appSettings);

        // Enable middleware only in Development or when explicitly configured
        if (app.Environment.IsDevelopment() || appSettings.EnableSwagger)
        {
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "swagger/{documentName}/swagger.json";
                options.PreSerializeFilters.Add((swaggerDoc, httpRequest) =>
                {
                    // Set the server URL based on the request or configuration
                    swaggerDoc.Servers =
                    [
                        new OpenApiServer
                        {
                            Url = appSettings.AppUrl,
                            Description = "Application Server"
                        }
                    ];
                });
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{appSettings.ApplicationDetail.ApplicationName} v1");
                options.RoutePrefix = "swagger";

                // UI Customization
                options.DocumentTitle = $"{appSettings.ApplicationDetail.ApplicationName} - API Documentation";
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                options.DefaultModelsExpandDepth(-1); // Hide schemas section by default
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
                options.ShowExtensions();

                // Enable "Try it out" by default
                options.EnableTryItOutByDefault();

                // Persist authorization data
                options.EnablePersistAuthorization();
            });
        }

        return app;
    }

    /// <summary>
    /// Configures JWT Bearer authentication for Swagger
    /// </summary>
    private static void ConfigureJwtAuthentication(SwaggerGenOptions options)
    {
        const string schemeId = "Bearer";

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };

        options.AddSecurityDefinition(schemeId, securityScheme);

        // In .NET 10, AddSecurityRequirement expects a delegate
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(schemeId, document)] = []
        });
    }

    /// <summary>
    /// Includes XML documentation comments in Swagger
    /// </summary>
    private static void IncludeXmlComments(SwaggerGenOptions options)
    {
        // Include XML comments from the API project
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }

        // Optionally include XML comments from other projects (e.g., Application, Domain)
        var applicationXml = Path.Combine(AppContext.BaseDirectory, "MedDemo.Application.xml");
        if (File.Exists(applicationXml))
        {
            options.IncludeXmlComments(applicationXml);
        }

        var domainXml = Path.Combine(AppContext.BaseDirectory, "MedDemo.Domain.xml");
        if (File.Exists(domainXml))
        {
            options.IncludeXmlComments(domainXml);
        }
    }
}