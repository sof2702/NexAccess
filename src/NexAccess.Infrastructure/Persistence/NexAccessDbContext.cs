using Microsoft.EntityFrameworkCore;

namespace NexAccess.Infrastructure.Persistence;

public class NexAccessDbContext : DbContext
{
    public NexAccessDbContext(DbContextOptions<NexAccessDbContext> options) : base(options)
    {
    }
}
