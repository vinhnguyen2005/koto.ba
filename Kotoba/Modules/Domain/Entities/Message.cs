using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.Entities
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        public bool IsSystemMessage { get; set; } = false;
        public SystemMessageType? SystemMessageType { get; set; }
        public string? SystemMessageData { get; set; }

        // Navigation properties
        public virtual Conversation Conversation { get; set; } = null!;
        public virtual User Sender { get; set; } = null!;
        
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }
        
        public Guid? ReplyToMessageId { get; set; }
        public virtual Message? ReplyToMessage { get; set; }

        public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public virtual ICollection<MessageReceipt> Receipts { get; set; } = new List<MessageReceipt>();
    }
}
