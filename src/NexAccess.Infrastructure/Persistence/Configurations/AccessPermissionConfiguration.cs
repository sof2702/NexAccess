using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class AccessPermissionConfiguration : IEntityTypeConfiguration<AccessPermission>
{
    public void Configure(EntityTypeBuilder<AccessPermission> builder)
    {
        builder.ToTable("permiso_acceso");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(p => p.RoleId).HasColumnName("id_rol");
        builder.Property(p => p.AreaId).HasColumnName("id_area");
        builder.Property(p => p.StartTime).HasColumnName("hora_desde").HasColumnType("time");
        builder.Property(p => p.EndTime).HasColumnName("hora_hasta").HasColumnType("time");
        builder.Property(p => p.Monday).HasColumnName("lunes");
        builder.Property(p => p.Tuesday).HasColumnName("martes");
        builder.Property(p => p.Wednesday).HasColumnName("miercoles");
        builder.Property(p => p.Thursday).HasColumnName("jueves");
        builder.Property(p => p.Friday).HasColumnName("viernes");
        builder.Property(p => p.Saturday).HasColumnName("sabado");
        builder.Property(p => p.Sunday).HasColumnName("domingo");
        builder.Property(p => p.IsActive).HasColumnName("activo");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(p => new { p.RoleId, p.AreaId }).IsUnique().HasDatabaseName("uq_permiso_rol_area");
        builder.HasOne(p => p.Role).WithMany().HasForeignKey(p => p.RoleId);
        builder.HasOne(p => p.Area).WithMany().HasForeignKey(p => p.AreaId);
    }
}
