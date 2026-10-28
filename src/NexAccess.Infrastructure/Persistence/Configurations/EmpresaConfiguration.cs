
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresa");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .UseSerialColumn();

        builder.Property(e => e.RazonSocial)
            .HasColumnName("razon_social")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.NombreFantasia)
            .HasColumnName("nombre_fantasia")
            .HasMaxLength(200);

        builder.Property(e => e.Ruc)
            .HasColumnName("ruc")
            .HasMaxLength(20);

        builder.Property(e => e.Direccion)
            .HasColumnName("direccion")
            .HasMaxLength(300);

        builder.Property(e => e.Telefono)
            .HasColumnName("telefono")
            .HasMaxLength(50);

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(150);

        builder.Property(e => e.LogoPath)
            .HasColumnName("logo_path")
            .HasMaxLength(500);

        builder.Property(e => e.Activo)
            .HasColumnName("activo")
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");
    }
}

