using MedDemo.Web.Extensions;
using MedDemo.Web.Middlewares;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MedDemo.Application.DTO;
using MedDemo.Domain.Authorization;
using MedDemo.WebAPI.Extensions;
using MedDemo.WebAPI.Middleware;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedDemo.WebAPI;

public static class DIConfiguration
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder, AppSettings appSettings)
    {
        var services = builder.Services;

        // Configure JSON Console Logging for ELK Stack
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            options.JsonWriterOptions = new JsonWriterOptions
            {
                Indented = false
            };
        });

        // Add Kubernetes-friendly logging
        if (builder.Environment.IsProduction())
        {
            builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            builder.Logging.AddFilter("System", LogLevel.Warning);
        }

        // Core Services
        services.AddEndpointsApiExplorer();

        // FluentValidation - Modern registration
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

        services.SetupMvc();

        // Authentication & Authorization
        ConfigureAuthentication(services, appSettings);
        services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

        // Middleware - Use Scoped for better lifecycle management
        services.AddScoped<GlobalExceptionMiddleware>();
        services.AddScoped<LoggingMiddleware>();
        services.AddScoped<PerformanceMiddleware>();
        services.AddSingleton<HealthCheckIPRestrictionMiddleware>();

        // Infrastructure Services
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });

        services.AddCompressionCustom();
        services.AddCorsCustom(appSettings);

        // HTTP Client - simple configuration without resilience extensions
        services.AddHttpClient();

        services.AddSwaggerOpenAPI(appSettings);
        services.SetupHealthCheck(appSettings);

        // JSON Serialization Configuration
        ConfigureJsonOptions(services);

        return builder;
    }

    private static void ConfigureAuthentication(IServiceCollection services, AppSettings appSettings)
    {
        if (appSettings.Identity.IsLocal)
        {
            services.AddAuthLocal(appSettings.Identity);
        }

        services.AddAuth(appSettings.Identity);
    }

    private static void ConfigureJsonOptions(IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.WriteIndented = false; // Compact JSON for production
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        });
    }

    // Extension method for configuring the app pipeline
    public static WebApplication ConfigureMiddleware(this WebApplication app, AppSettings appSettings)
    {
        // Exception handling first
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // Performance monitoring
        app.UseMiddleware<PerformanceMiddleware>();

        // Request/Response logging for ELK
        app.UseMiddleware<LoggingMiddleware>();

        // Standard middleware pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseResponseCompression();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        // Health checks
        app.UseMiddleware<HealthCheckIPRestrictionMiddleware>();
        app.MapHealthChecks("/health/ready", new()
        {
            Predicate = check => check.Tags.Contains("ready")
        });
        app.MapHealthChecks("/health/live", new()
        {
            Predicate = _ => false
        });

        return app;
    }
}