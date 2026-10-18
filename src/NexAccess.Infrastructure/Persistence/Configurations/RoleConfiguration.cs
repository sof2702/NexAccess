using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("rol");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(r => r.Code).HasColumnName("codigo").HasMaxLength(50).IsRequired();
        builder.Property(r => r.Name).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasColumnName("descripcion").HasMaxLength(255);
        builder.Property(r => r.IsActive).HasColumnName("activo");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(r => r.Code).IsUnique().HasDatabaseName("uq_rol_codigo");
    }
}
