namespace Kotoba.Modules.Domain.DTOs
{
    public class UserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }
}
