namespace Kotoba.Modules.Domain.Enums;

public enum AdminSecurityAlertType
{
    FailedAdminLoginBurst = 0,
    SuspiciousIpVelocity = 1,
    RoleChangeAttempt = 2,
    MassModerationAction = 3,
}
