using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public string RecipientId { get; set; } = string.Empty;   // người nhận
        public NotificationType Type { get; set; }
        public string? ActorId { get; set; }                       // người trigger (nullable vì admin system)
        public string? TargetId { get; set; }                      // storyId, reportId, userId...
        public string? TargetType { get; set; }                    // "Story", "Report", "User"...
        public string Message { get; set; } = string.Empty;        // nội dung hiển thị
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual User Recipient { get; set; } = null!;
        public virtual User? Actor { get; set; }
    }
}
