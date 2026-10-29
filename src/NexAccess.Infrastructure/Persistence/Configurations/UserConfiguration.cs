using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("usuario");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(u => u.EmployeeId).HasColumnName("id_empleado");
        builder.Property(u => u.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(u => u.SystemRole).HasColumnName("rol_sistema").HasMaxLength(30).HasConversion(new UpperSnakeEnumConverter<SystemRole>());
        builder.Property(u => u.WebAccessEnabled).HasColumnName("acceso_web").IsRequired().HasDefaultValue(false);
        builder.Property(u => u.MobileAccessEnabled).HasColumnName("acceso_movil").IsRequired().HasDefaultValue(false);
        builder.Property(u => u.IsActive).HasColumnName("activo");
        builder.Property(u => u.LastLoginAt).HasColumnName("ultimo_login");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(u => u.Username).IsUnique().HasDatabaseName("uq_usuario_username");
        builder.HasOne<Employee>().WithMany().HasForeignKey(u => u.EmployeeId);
    }
}
