using Microsoft.EntityFrameworkCore;
using NexAccess.Domain.Entities;

namespace NexAccess.Infrastructure.Persistence;

public class NexAccessDbContext : DbContext
{
    public NexAccessDbContext(DbContextOptions<NexAccessDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<EmployeeRole> EmployeeRoles => Set<EmployeeRole>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<AccessPermission> AccessPermissions => Set<AccessPermission>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<AccessEvent> AccessEvents => Set<AccessEvent>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<DeviceCommand> DeviceCommands => Set<DeviceCommand>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<MobileSession> MobileSessions => Set<MobileSession>();
    public DbSet<UsedMobileRefreshToken> UsedMobileRefreshTokens  => Set<UsedMobileRefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexAccessDbContext).Assembly);
    }
}
