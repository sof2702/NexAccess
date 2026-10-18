using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("area");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(a => a.Code).HasColumnName("codigo").HasMaxLength(50).IsRequired();
        builder.Property(a => a.Name).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Description).HasColumnName("descripcion").HasMaxLength(255);
        builder.Property(a => a.IsRestricted).HasColumnName("restringida");
        builder.Property(a => a.IsActive).HasColumnName("activo");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(a => a.Code).IsUnique().HasDatabaseName("uq_area_codigo");
    }
}
