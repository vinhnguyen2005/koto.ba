namespace Kotoba.Modules.Domain.Entities
{
    public class Story
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? MediaUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public string Visibility { get; set; } = "public";

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<StoryPermission> Permissions { get; set; } = new List<StoryPermission>();

        public virtual ICollection<StoryView> Views { get; set; } = new List<StoryView>();

        public virtual ICollection<StoryReaction> Reactions { get; set; } = new List<StoryReaction>();
    }
}
