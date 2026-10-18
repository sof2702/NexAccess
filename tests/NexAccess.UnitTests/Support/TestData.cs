using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.UnitTests.Support;

public static class TestData
{
    public static readonly DateTime MondayNoonUtc = new(2026, 10, 19, 15, 0, 0, DateTimeKind.Utc);

    public static Employee Employee(long id = 10, bool active = true) => new()
    {
        Id = id,
        DocumentNumber = $"C{id}",
        FirstName = "Ana",
        LastName = "Pérez",
        IsActive = active
    };

    public static Area Area(long id = 5, bool restricted = true, bool active = true) => new()
    {
        Id = id,
        Code = $"AREA{id}",
        Name = $"Área {id}",
        IsRestricted = restricted,
        IsActive = active
    };

    public static Device Device(DeviceType type = DeviceType.Acceso, Area? area = null, long id = 3) => new()
    {
        Id = id,
        Code = "PUERTA-RRHH-01",
        Name = "Puerta RRHH",
        Type = type,
        Area = area,
        AreaId = area?.Id,
        IsActive = true,
        KeyHash = "hash"
    };

    public static Credential Credential(Employee employee, string code = "04A1B2C3D4", long id = 20) => new()
    {
        Id = id,
        EmployeeId = employee.Id,
        Employee = employee,
        CredentialCode = code,
        Type = CredentialType.Fisica,
        IsActive = true
    };

    public static AccessPermission AllDayPermission(long roleId = 1, long areaId = 5) => new()
    {
        Id = 1,
        RoleId = roleId,
        AreaId = areaId,
        Monday = true,
        Tuesday = true,
        Wednesday = true,
        Thursday = true,
        Friday = true,
        Saturday = true,
        Sunday = true,
        IsActive = true
    };
}
