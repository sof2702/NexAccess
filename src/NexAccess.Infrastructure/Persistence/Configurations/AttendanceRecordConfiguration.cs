using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("marcacion");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(m => m.EmployeeId).HasColumnName("id_empleado");
        builder.Property(m => m.CredentialId).HasColumnName("id_credencial");
        builder.Property(m => m.DeviceId).HasColumnName("id_dispositivo");
        builder.Property(m => m.Type).HasColumnName("tipo").HasMaxLength(20).HasConversion(new UpperSnakeEnumConverter<AttendanceType>());
        builder.Property(m => m.OccurredAt).HasColumnName("fecha_hora");
        builder.Property(m => m.Source).HasColumnName("origen").HasMaxLength(20).HasConversion(new UpperSnakeEnumConverter<AttendanceSource>());
        builder.Property(m => m.Note).HasColumnName("observacion").HasMaxLength(255);
        builder.Property(m => m.RegisteredByUserId).HasColumnName("id_usuario_registro");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.HasOne(m => m.Employee).WithMany().HasForeignKey(m => m.EmployeeId);
        builder.HasOne<Credential>().WithMany().HasForeignKey(m => m.CredentialId);
        builder.HasOne(m => m.Device).WithMany().HasForeignKey(m => m.DeviceId);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.RegisteredByUserId);
    }
}
