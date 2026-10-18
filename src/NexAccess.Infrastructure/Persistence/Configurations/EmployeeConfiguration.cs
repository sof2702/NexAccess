using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("empleado");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(e => e.DocumentNumber).HasColumnName("cedula").HasMaxLength(20).IsRequired();
        builder.Property(e => e.FirstName).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasColumnName("apellido").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(150);
        builder.Property(e => e.Phone).HasColumnName("telefono").HasMaxLength(30);
        builder.Property(e => e.IsActive).HasColumnName("activo");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(e => e.FullName);
        builder.HasIndex(e => e.DocumentNumber).IsUnique().HasDatabaseName("uq_empleado_cedula");
        builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("uq_empleado_email");
        builder.HasMany(e => e.Roles).WithOne(r => r.Employee).HasForeignKey(r => r.EmployeeId);
        builder.HasMany(e => e.Credentials).WithOne(c => c.Employee).HasForeignKey(c => c.EmployeeId);
    }
}
