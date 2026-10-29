using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class UsedMobileRefreshTokenConfiguration
    : IEntityTypeConfiguration<UsedMobileRefreshToken>
{
    public void Configure(
        EntityTypeBuilder<UsedMobileRefreshToken> builder)
    {
        builder.ToTable("sesion_movil_token_usado");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .UseSerialColumn();

        builder.Property(t => t.SessionId)
            .HasColumnName("id_sesion")
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.UsedAt)
            .HasColumnName("usado_at")
            .IsRequired();

        builder.HasOne(t => t.Session)
            .WithMany()
            .HasForeignKey(t => t.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_token_usado_hash");

        builder.HasIndex(t => t.SessionId)
            .HasDatabaseName("idx_token_usado_sesion");
    }
}

