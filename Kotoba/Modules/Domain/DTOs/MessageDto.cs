using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs
{
    public class MessageDto
    {
        public string TempId { get; set; } = string.Empty;
        public Guid MessageId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Guid ConversationId { get; set; }
        public DateTime CreatedAt { get; set; }
        public MessageStatus Status { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new();
        public List<ReactionDto> Reactions { get; set; } = new();
        public bool IsSystemMessage { get; set; } = false;
        public SystemMessageType? SystemMessageType { get; set; }
        public SystemMessageDataDto? SystemMessageData { get; set; }
    }

    public class SystemMessageDataDto
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
