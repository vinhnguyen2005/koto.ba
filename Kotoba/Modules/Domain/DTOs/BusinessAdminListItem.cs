using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs;

public class BusinessAdminListItem
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AccountStatus AccountStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}
