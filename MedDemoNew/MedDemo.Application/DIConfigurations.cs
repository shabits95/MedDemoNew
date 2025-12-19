namespace MedDemo.Application
{
    public static class DIConfiguration
    {
        public static IServiceCollection AddDIApplication(this IServiceCollection services, IConfiguration configuration)
        {
            // Add service configurations here
            return services;
        }
    }
}
