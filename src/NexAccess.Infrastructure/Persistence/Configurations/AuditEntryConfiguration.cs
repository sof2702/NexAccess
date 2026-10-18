using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("auditoria");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(a => a.UserId).HasColumnName("id_usuario");
        builder.Property(a => a.DeviceId).HasColumnName("id_dispositivo");
        builder.Property(a => a.Action).HasColumnName("accion").HasMaxLength(60).IsRequired();
        builder.Property(a => a.Entity).HasColumnName("entidad").HasMaxLength(60).IsRequired();
        builder.Property(a => a.EntityId).HasColumnName("id_entidad");
        builder.Property(a => a.Detail).HasColumnName("detalle").HasMaxLength(1000);
        builder.Property(a => a.IpAddress).HasColumnName("direccion_ip").HasMaxLength(45);
        builder.Property(a => a.OccurredAt).HasColumnName("fecha_hora");
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId);
        builder.HasOne<Device>().WithMany().HasForeignKey(a => a.DeviceId);
    }
}
