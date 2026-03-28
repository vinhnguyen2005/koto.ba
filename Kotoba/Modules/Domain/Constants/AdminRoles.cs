namespace Kotoba.Modules.Domain.Constants;

public static class AdminRoles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string BusinessAdmin = "BusinessAdmin";
    public const string AnyAdmin = SystemAdmin + "," + BusinessAdmin;
}
