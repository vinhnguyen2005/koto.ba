namespace Kotoba.Modules.Domain.DTOs;

public class CreateStoryRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string Visibility { get; set; } = "public";
    public List<string>? AllowedUserIds { get; set; }
}
