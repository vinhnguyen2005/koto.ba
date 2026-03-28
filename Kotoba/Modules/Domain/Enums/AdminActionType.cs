namespace Kotoba.Modules.Domain.Enums;

public enum AdminActionType
{
    Unknown = 0,
    AdminLoginSucceeded = 1,
    AdminLoginFailed = 2,
    AdminCreated = 3,
    AdminDisabled = 4,
    UserViewed = 5,
    UserReportResolved = 6,
    StatisticsViewed = 7,
    StoryModerated = 8,
    SecurityPolicyChanged = 9,
    UserDeactivated = 10,
    UserReactivated = 11,
    UserBanned = 12,
    UserUnbanned = 13,
}
