using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.Entities
{
    public class StoryReaction
    {
        public Guid Id { get; set; }
        public Guid StoryId { get; set; }              // 🔁 replace MessageId
        public string UserId { get; set; } = string.Empty;

        public ReactionType Type { get; set; }         // 🔁 reuse enum
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Story Story { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
