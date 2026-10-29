
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class MobileSessionConfiguration
    : IEntityTypeConfiguration<MobileSession>
{
    public void Configure(EntityTypeBuilder<MobileSession> builder)
    {
        builder.ToTable("sesion_movil");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.UserId)
            .HasColumnName("id_usuario")
            .IsRequired();

        builder.Property(s => s.InstallationId)
            .HasColumnName("instalacion_id")
            .IsRequired();

        builder.Property(s => s.RefreshTokenHash)
            .HasColumnName("refresh_token_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .HasColumnName("activa")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.LastUsedAt)
            .HasColumnName("ultimo_uso");

        builder.Property(s => s.TokenRotatedAt)
            .HasColumnName("token_rotado_at");

        builder.Property(s => s.RevokedAt)
            .HasColumnName("revocada_at");

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.RefreshTokenHash)
            .IsUnique()
            .HasDatabaseName("uq_sesion_movil_refresh_hash");

        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("idx_sesion_movil_usuario");

        builder.HasIndex(s => s.InstallationId)
            .HasDatabaseName("idx_sesion_movil_instalacion")
            .HasFilter("activa = TRUE");

        builder.HasIndex(s => s.RevokedAt)
            .HasDatabaseName("idx_sesion_movil_revocada")
            .HasFilter("revocada_at IS NOT NULL");

        builder.Ignore(s => s.IsRevoked);
    }
}

