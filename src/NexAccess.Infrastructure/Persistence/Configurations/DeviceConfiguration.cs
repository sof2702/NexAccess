using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("dispositivo");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(d => d.Code).HasColumnName("codigo").HasMaxLength(50).IsRequired();
        builder.Property(d => d.Name).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(d => d.Type).HasColumnName("tipo").HasMaxLength(20).HasConversion(new UpperSnakeEnumConverter<DeviceType>());
        builder.Property(d => d.AreaId).HasColumnName("id_area");
        builder.Property(d => d.Description).HasColumnName("descripcion").HasMaxLength(255);
        builder.Property(d => d.IsActive).HasColumnName("activo");
        builder.Property(d => d.KeyHash).HasColumnName("clave_hash").HasMaxLength(128).IsRequired();
        builder.Property(d => d.KeyRotatedAt).HasColumnName("clave_rotada_at");
        builder.Property(d => d.LastConnectionAt).HasColumnName("ultima_conexion");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(d => d.ControlsAccess);
        builder.Ignore(d => d.RegistersAttendance);
        builder.HasIndex(d => d.Code).IsUnique().HasDatabaseName("uq_dispositivo_codigo");
        builder.HasIndex(d => d.KeyHash).IsUnique().HasDatabaseName("uq_dispositivo_clave_hash");
        builder.HasOne(d => d.Area).WithMany().HasForeignKey(d => d.AreaId);
    }
}
