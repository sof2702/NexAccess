namespace NexAccess.Domain.Enums;

public static class AccessReason
{
    public const string Authorized = "AUTHORIZED";
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string DeviceMismatch = "DEVICE_MISMATCH";
    public const string DeviceNotAccessControl = "DEVICE_NOT_ACCESS_CONTROL";
    public const string DeviceWithoutArea = "DEVICE_WITHOUT_AREA";
    public const string CredentialNotFound = "CREDENTIAL_NOT_FOUND";
    public const string CredentialInactive = "CREDENTIAL_INACTIVE";
    public const string CredentialRevoked = "CREDENTIAL_REVOKED";
    public const string CredentialExpired = "CREDENTIAL_EXPIRED";
    public const string EmployeeInactive = "EMPLOYEE_INACTIVE";
    public const string AreaInactive = "AREA_INACTIVE";
    public const string NoPermission = "NO_PERMISSION";
    public const string OutsideSchedule = "OUTSIDE_SCHEDULE";
}
