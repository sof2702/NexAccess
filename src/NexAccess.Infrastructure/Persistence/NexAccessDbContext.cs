using Microsoft.EntityFrameworkCore;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence;

public class NexAccessDbContext : DbContext
{
    public NexAccessDbContext(DbContextOptions<NexAccessDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(eb =>
        {
            eb.ToTable("users");
            eb.HasKey(e => e.Id);
            eb.Property(e => e.Username).HasColumnName("username").IsRequired();
            eb.Property(e => e.DisplayName).HasColumnName("display_name");
        });
    }
}
