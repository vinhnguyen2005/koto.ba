using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs
{
    public class CurrentThoughtDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ThoughtPrivacy Privacy { get; set; }
    }
}
