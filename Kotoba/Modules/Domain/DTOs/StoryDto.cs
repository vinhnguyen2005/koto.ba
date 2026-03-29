namespace Kotoba.Modules.Domain.DTOs;

public class StoryDto
{
    public Guid StoryId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Visibility { get; set; } = "public";
    public List<string>? AllowedUserIds { get; set; } = new();
}
