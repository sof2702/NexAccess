namespace NexAccess.Domain.Enums;

public static class SystemRoles
{
    public const string Admin = "ADMIN";
    public const string Seguridad = "SEGURIDAD";
    public const string Rrhh = "RRHH";

    public const string AdminOrSeguridad = Admin + "," + Seguridad;
    public const string AdminOrRrhh = Admin + "," + Rrhh;
    public const string All = Admin + "," + Seguridad + "," + Rrhh;

    public static string From(SystemRole role) => role switch
    {
        SystemRole.Admin => Admin,
        SystemRole.Seguridad => Seguridad,
        _ => Rrhh
    };
}
