using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrolamiento");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(e => e.EmployeeId).HasColumnName("id_empleado");
        builder.Property(e => e.DeviceId).HasColumnName("id_dispositivo");
        builder.Property(e => e.CredentialType).HasColumnName("tipo_credencial").HasMaxLength(20).HasConversion(new UpperSnakeEnumConverter<CredentialType>());
        builder.Property(e => e.CredentialName).HasColumnName("nombre_credencial").HasMaxLength(100);
        builder.Property(e => e.Status).HasColumnName("estado").HasMaxLength(20).HasConversion(new UpperSnakeEnumConverter<EnrollmentStatus>());
        builder.Property(e => e.ExpiresAt).HasColumnName("fecha_expiracion");
        builder.Property(e => e.CredentialId).HasColumnName("id_credencial");
        builder.Property(e => e.RequestedByUserId).HasColumnName("id_usuario_solicitante");
        builder.Property(e => e.CompletedAt).HasColumnName("completado_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId);
        builder.HasOne(e => e.Device).WithMany().HasForeignKey(e => e.DeviceId);
        builder.HasOne(e => e.Credential).WithMany().HasForeignKey(e => e.CredentialId);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.RequestedByUserId);
    }
}
