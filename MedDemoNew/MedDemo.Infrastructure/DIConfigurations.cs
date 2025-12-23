using MedDemo.Application.Common;
using MedDemo.Infrastructure.Repositories.Common;
using MedDemo.Infrastructure.Data;
using MedDemo.Application.Interfaces.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedDemo.Infrastructure
{
    public static class DIConfiguration
    {
        public static IServiceCollection AddDIInfrastructure(this IServiceCollection services, AppSettings appSettings)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(appSettings.ConnectionStrings.DefaultConnection));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            // Add service configurations here
            return services;
        }
    }
}
