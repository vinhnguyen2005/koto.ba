using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs;

public sealed class NormalUserListItem
{
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public AccountStatus AccountStatus { get; init; }
    public bool IsOnline { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastSeenAt { get; init; }
}
