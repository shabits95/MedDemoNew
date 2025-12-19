using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Net.NetworkInformation;

namespace MedDemo.Infrastructure.Data.Configurations;

public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.ToTable("Medicine");

        builder.HasKey(x => x.Id);

        builder.Property(k => k.Id).UseIdentityColumn();

        builder.Property(x => x.Status).HasDefaultValue(Status.Active);

        //builder.HasMany(e => e.UserRoles)
        //    .WithOne(e => e.Role)
        //    .HasForeignKey(ur => ur.RoleId)
        //    .IsRequired()
        //    .OnDelete(DeleteBehavior.NoAction);

    }
}
