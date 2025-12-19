using Microsoft.Extensions.DependencyInjection;

namespace MedDemo.Domain
{
    public static class DIConfiguration
    {
        public static IServiceCollection AddDIDomain(this IServiceCollection services)
        {
            // Add service configurations here
            return services;
        }
    }
}
