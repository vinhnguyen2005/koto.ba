namespace Kotoba.Modules.Domain.DTOs;

public class CreateStoryRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
}
