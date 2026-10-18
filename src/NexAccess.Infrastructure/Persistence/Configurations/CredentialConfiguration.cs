using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Infrastructure.Persistence.Configurations;

public class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("credencial");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseSerialColumn();
        builder.Property(c => c.EmployeeId).HasColumnName("id_empleado");
        builder.Property(c => c.CredentialCode).HasColumnName("codigo_credencial").HasMaxLength(150).IsRequired();
        builder.Property(c => c.Type).HasColumnName("tipo").HasMaxLength(20).HasConversion(new UpperSnakeEnumConverter<CredentialType>());
        builder.Property(c => c.Name).HasColumnName("nombre").HasMaxLength(100);
        builder.Property(c => c.IsActive).HasColumnName("activa");
        builder.Property(c => c.IssuedAt).HasColumnName("fecha_emision");
        builder.Property(c => c.ExpiresAt).HasColumnName("fecha_expiracion");
        builder.Property(c => c.RevokedAt).HasColumnName("revocada_at");
        builder.Property(c => c.RevocationReason).HasColumnName("motivo_revocacion").HasMaxLength(255);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(c => c.IsRevoked);
        builder.HasIndex(c => c.CredentialCode).IsUnique().HasDatabaseName("uq_credencial_codigo");
    }
}
