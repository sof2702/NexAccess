using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class AccessEventConfiguration : IEntityTypeConfiguration<AccessEvent>
{
    public void Configure(EntityTypeBuilder<AccessEvent> builder)
    {
        builder.ToTable("evento_acceso");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(e => e.EmployeeId).HasColumnName("id_empleado");
        builder.Property(e => e.CredentialId).HasColumnName("id_credencial");
        builder.Property(e => e.DeviceId).HasColumnName("id_dispositivo");
        builder.Property(e => e.AreaId).HasColumnName("id_area");
        builder.Property(e => e.PresentedCode).HasColumnName("codigo_credencial").HasMaxLength(150);
        builder.Property(e => e.OccurredAt).HasColumnName("fecha_hora");
        builder.Property(e => e.Result).HasColumnName("resultado").HasMaxLength(20).HasConversion(new UpperSnakeEnumConverter<AccessResult>());
        builder.Property(e => e.Reason).HasColumnName("motivo").HasMaxLength(100);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId);
        builder.HasOne<Credential>().WithMany().HasForeignKey(e => e.CredentialId);
        builder.HasOne(e => e.Device).WithMany().HasForeignKey(e => e.DeviceId);
        builder.HasOne(e => e.Area).WithMany().HasForeignKey(e => e.AreaId);
    }
}
