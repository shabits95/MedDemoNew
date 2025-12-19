using MedDemo.Application.Common;
using MedDemo.Application.Interfaces.IServices;
using Microsoft.Extensions.DependencyInjection;
using MedDemo.Application.Services;

namespace MedDemo.Application
{
    public static class DIConfiguration
    {
        public static IServiceCollection AddDIApplication(this IServiceCollection services, AppSettings appSettings)
        {
            // Add service configurations here
            services.AddTransient<IMedicineService, MedicineService>();

            return services;
        }
    }
}
