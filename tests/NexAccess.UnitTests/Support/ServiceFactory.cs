using Microsoft.Extensions.Options;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Options;
using NexAccess.Application.Services;
using NSubstitute;

namespace NexAccess.UnitTests.Support;

public class ServiceFactory
{
    public FakeClock Clock { get; } = new(TestData.MondayNoonUtc);
    public TestCurrentUser CurrentUser { get; } = new();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IAuditRepository AuditRepository { get; } = Substitute.For<IAuditRepository>();
    public IEmployeeRepository Employees { get; } = Substitute.For<IEmployeeRepository>();
    public IRoleRepository Roles { get; } = Substitute.For<IRoleRepository>();
    public IAreaRepository Areas { get; } = Substitute.For<IAreaRepository>();
    public ICredentialRepository Credentials { get; } = Substitute.For<ICredentialRepository>();
    public IDeviceRepository Devices { get; } = Substitute.For<IDeviceRepository>();
    public IAccessPermissionRepository Permissions { get; } = Substitute.For<IAccessPermissionRepository>();
    public IAccessEventRepository Events { get; } = Substitute.For<IAccessEventRepository>();
    public IAttendanceRepository Attendance { get; } = Substitute.For<IAttendanceRepository>();
    public IEnrollmentRepository Enrollments { get; } = Substitute.For<IEnrollmentRepository>();
    public IDeviceCommandRepository Commands { get; } = Substitute.For<IDeviceCommandRepository>();
    public IUserRepository Users { get; } = Substitute.For<IUserRepository>();
    public AccessOptions AccessOptions { get; } = new() { OpenDurationMs = 3000, TimeZoneId = "America/Asuncion" };
    public AttendanceOptions AttendanceOptions { get; } = new() { DuplicateWindowSeconds = 60, MaxReportDays = 31 };
    public EnrollmentOptions EnrollmentOptions { get; } = new() { DefaultExpirationMinutes = 5, MaxExpirationMinutes = 60 };

    public LocalTime LocalTime => new(Microsoft.Extensions.Options.Options.Create(AccessOptions));

    public AuditService Audit() => new(AuditRepository, CurrentUser, Clock);

    public AttendanceService AttendanceService() => new(
        Attendance, Employees, UnitOfWork, Audit(), CurrentUser, Clock, LocalTime, Microsoft.Extensions.Options.Options.Create(AttendanceOptions));

    public AccessService AccessService() => new(
        Devices,
        Credentials,
        Permissions,
        Events,
        AttendanceService(),
        UnitOfWork,
        Clock,
        LocalTime,
        Microsoft.Extensions.Options.Options.Create(AccessOptions),
        Microsoft.Extensions.Logging.Abstractions.NullLogger<AccessService>.Instance);

    public CredentialService CredentialService() => new(Credentials, Employees, UnitOfWork, Audit(), Clock);

    public EmployeeService EmployeeService() => new(Employees, Roles, UnitOfWork, Audit(), Clock);

    public EnrollmentService EnrollmentService() => new(
        Enrollments, Employees, Devices, Commands, CredentialService(), UnitOfWork, Audit(), CurrentUser, Clock, Microsoft.Extensions.Options.Options.Create(EnrollmentOptions));

    public DeviceCommandService DeviceCommandService() => new(Commands, UnitOfWork, Clock);

    public DeviceService DeviceService(IDeviceKeyService keys) => new(Devices, Areas, Events, keys, UnitOfWork, Audit(), Clock);

    public AuthService AuthService(IPasswordHasher hasher, ITokenService tokens) => new(Users, hasher, tokens, UnitOfWork, Audit(), Clock);
}
