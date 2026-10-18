using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class EmployeeRoleConfiguration : IEntityTypeConfiguration<EmployeeRole>
{
    public void Configure(EntityTypeBuilder<EmployeeRole> builder)
    {
        builder.ToTable("empleado_rol");
        builder.HasKey(er => new { er.EmployeeId, er.RoleId });
        builder.Property(er => er.EmployeeId).HasColumnName("id_empleado");
        builder.Property(er => er.RoleId).HasColumnName("id_rol");
        builder.Property(er => er.CreatedAt).HasColumnName("created_at");
        builder.HasOne(er => er.Role).WithMany().HasForeignKey(er => er.RoleId);
    }
}
