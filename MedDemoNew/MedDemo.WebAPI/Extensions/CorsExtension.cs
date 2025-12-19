using MedDemo.Application.Common;

namespace MedDemo.Web.Extensions;

public static class CorsExtension
{
    public static IServiceCollection AddCorsCustom(this IServiceCollection services, AppSettings appSettings)
    {
        return services.AddCors(options => options.AddPolicy("AllowSpecificOrigin",
             builder => builder
                 .WithOrigins(appSettings.Cors)
                 .AllowCredentials() // Allow credentials
                 .AllowAnyHeader()
                 .AllowAnyMethod()));
    }
}
