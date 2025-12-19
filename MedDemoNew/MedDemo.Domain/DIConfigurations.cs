namespace MedDemo.Domain
{
    public static class DIConfiguration
    {
        public static IServiceCollection AddDIDomain(this IServiceCollection services, IConfiguration configuration)
        {
            // Add service configurations here
            return services;
        }
    }
}
