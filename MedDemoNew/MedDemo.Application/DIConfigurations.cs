using AutoMapper;
using MedDemo.Application.Common;
using MedDemo.Application.Interfaces.IServices;
using MedDemo.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MedDemo.Application
{
    public static class DIConfiguration
    {
        public static IServiceCollection AddDIApplication(this IServiceCollection services, AppSettings appSettings)
        {
            // Add service configurations here
            // AutoMapper - Manual Configuration
            // Just install: AutoMapper (base package only)
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(typeof(IMedicineService).Assembly);
            });
            services.AddTransient<IMedicineService, MedicineService>();

            return services;
        }
    }
}
