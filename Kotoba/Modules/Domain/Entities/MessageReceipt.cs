using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.Entities
{
    public class MessageReceipt
    {
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public MessageStatus Status { get; set; } = MessageStatus.Pending;
        public DateTime? ReceivedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Message Message { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
