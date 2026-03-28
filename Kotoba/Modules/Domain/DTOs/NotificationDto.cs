using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public NotificationType Type { get; set; }
        public string? ActorId { get; set; }
        public string? ActorName { get; set; }
        public string? ActorAvatar { get; set; }
        public string? TargetId { get; set; }
        public string? TargetType { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
