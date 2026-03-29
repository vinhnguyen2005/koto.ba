using Kotoba.Modules.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Kotoba.Modules.Domain.Entities
{
    public class User : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
        public DateTime? DeactivatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<MessageReceipt> MessageReceipts { get; set; } = new List<MessageReceipt>();
        public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
        public virtual ICollection<Story> Stories { get; set; } = new List<Story>();
        public virtual CurrentThought? CurrentThought { get; set; }
    }
}
