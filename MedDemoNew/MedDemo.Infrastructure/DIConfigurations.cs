using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace MedDemo.Infrastructure
{
    public static IServiceCollection AddDIInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        // Add service configurations here
        return services;
    }
}
