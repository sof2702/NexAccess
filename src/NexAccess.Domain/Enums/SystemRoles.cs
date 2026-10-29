
namespace NexAccess.Domain.Enums;

public static class SystemRoles
{
    public const string Admin = "ADMIN";
    public const string Seguridad = "SEGURIDAD";
    public const string Rrhh = "RRHH";
    public const string Empleado = "EMPLEADO";

    // Roles administrativos
    public const string AdminOrSeguridad = Admin + "," + Seguridad;
    public const string AdminOrRrhh = Admin + "," + Rrhh;

    // IMPORTANTE: EMPLEADO no debe incluirse aquí.
    public const string All = Admin + "," + Seguridad + "," + Rrhh;

    public static string From(SystemRole role) => role switch
    {
        SystemRole.Admin => Admin,
        SystemRole.Seguridad => Seguridad,
        SystemRole.Rrhh => Rrhh,
        SystemRole.Empleado => Empleado,

        _ => throw new ArgumentOutOfRangeException(
            nameof(role),
            role,
            "Rol de sistema no reconocido.")
    };
}

