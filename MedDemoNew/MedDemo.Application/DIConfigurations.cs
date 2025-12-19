using MedDemo.Application.Common;
using MedDemo.Infrastructure.Interface;
using Microsoft.Extensions.DependencyInjection;
using MedDemo.Infrastructure.Repositories;

namespace MedDemo.Application
{
    public static class DIConfiguration
    {
        public static IServiceCollection AddDIApplication(this IServiceCollection services, AppSettings appSettings)
        {
            // Add service configurations here
            services.AddTransient<IMedicineRepository, MedicineRepository>();

            return services;
        }
    }
}
