using Microsoft.EntityFrameworkCore;
using MedDemo.Domain.Entities;
using System.Reflection;

namespace MedDemo.Infrastructure.Data
{

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            builder.Seed();
        }
        public DbSet<Medicine> Medicine { get; set; }
    }
}
