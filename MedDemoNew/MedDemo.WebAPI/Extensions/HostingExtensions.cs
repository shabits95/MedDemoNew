using MedDemo.Application;
using MedDemo.Application.Common;
using MedDemo.Infrastructure;
using MedDemo.Infrastructure.Data;
using MedDemo.Web.Middlewares;

namespace MedDemo.Web.Extensions;

public static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder, AppSettings appsettings)
    {
        builder.Services.AddInfrastructuresService(appsettings);
        builder.Services.AddApplicationService(appsettings);
        builder.Services.AddWebAPIService(appsettings);

        return builder.Build();
    }

    public static async Task<WebApplication> ConfigurePipelineAsync(this WebApplication app, AppSettings appsettings)
    {
        using var loggerFactory = LoggerFactory.Create(builder => { });
        using var scope = app.Services.CreateScope();

        if (!appsettings.UseInMemoryDatabase)
        {
            var initialize = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
            await initialize.InitializeAsync();
        }
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.ConfigureExceptionHandler(loggerFactory.CreateLogger("Exceptions"));
        app.UseMiddleware<LoggingMiddleware>();
        app.UseMiddleware<PerformanceMiddleware>();
        app.UseHttpsRedirection();
        app.UseCors("AllowSpecificOrigin");
        app.UseSwagger(appsettings);
        app.ConfigureHealthCheck();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

}
