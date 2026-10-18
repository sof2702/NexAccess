using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class DeviceCommandConfiguration : IEntityTypeConfiguration<DeviceCommand>
{
    public void Configure(EntityTypeBuilder<DeviceCommand> builder)
    {
        builder.ToTable("comando_dispositivo");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(c => c.DeviceId).HasColumnName("id_dispositivo");
        builder.Property(c => c.Type).HasColumnName("tipo").HasMaxLength(30).HasConversion(new UpperSnakeEnumConverter<DeviceCommandType>());
        builder.Property(c => c.EnrollmentId).HasColumnName("id_enrolamiento");
        builder.Property(c => c.Status).HasColumnName("estado").HasMaxLength(20).HasConversion(new UpperSnakeEnumConverter<DeviceCommandStatus>());
        builder.Property(c => c.ExpiresAt).HasColumnName("fecha_expiracion");
        builder.Property(c => c.DeliveredAt).HasColumnName("entregado_at");
        builder.Property(c => c.CompletedAt).HasColumnName("completado_at");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Ignore(c => c.IsOpen);
        builder.HasOne<Device>().WithMany().HasForeignKey(c => c.DeviceId);
        builder.HasOne(c => c.Enrollment).WithMany().HasForeignKey(c => c.EnrollmentId);
    }
}
