using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs
{
    public class CreateNotificationRequest
    {
        public string RecipientId { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string? ActorId { get; set; }
        public string? TargetId { get; set; }
        public string? TargetType { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
