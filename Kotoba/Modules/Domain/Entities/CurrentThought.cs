namespace Kotoba.Modules.Domain.Entities
{
    public class CurrentThought
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
