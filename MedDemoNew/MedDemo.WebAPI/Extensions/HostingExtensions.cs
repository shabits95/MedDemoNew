using MedDemo.Application;
using MedDemo.Application.Common;
using MedDemo.Domain;
using MedDemo.WebAPI.Middleware;
using MedDemo.Infrastructure;
using MedDemo.WebAPI;
using MedDemo.WebAPI.Extensions;

namespace MedDemo.Web.Extensions;

public static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder, AppSettings appsettings)
    {
        builder.Services.AddDIApplication(appsettings).AddDIInfrastructure(appsettings);
        builder.AddConfigureServices(appsettings);

        return builder.Build();
    }

    public static async Task<WebApplication> ConfigurePipelineAsync(this WebApplication app, AppSettings appsettings)
    {
        using var loggerFactory = LoggerFactory.Create(builder => { });
        using var scope = app.Services.CreateScope();

        
        //app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseGlobalExceptionHandler(loggerFactory.CreateLogger("Exceptions"), app.Environment);
        app.UseMiddleware<LoggingMiddleware>();
        app.UseMiddleware<PerformanceMiddleware>();
        app.UseHsts();
        app.UseHttpsRedirection();
        app.UseCors("AllowSpecificOrigin");
        app.UseSwaggerDocumentation(appsettings);
        app.MapHealthCheckEndpoints();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

}
