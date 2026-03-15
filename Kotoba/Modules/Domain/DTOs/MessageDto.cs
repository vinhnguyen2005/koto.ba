using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs;

public class MessageDto
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    // Backward-compatible aggregate/current-user status.
    public MessageStatus? Status { get; set; }
    public List<MessageReceiptDto> Receipts { get; set; } = new();
    public List<ReactionDto> Reactions { get; set; } = new();
    public List<AttachmentDto> Attachments { get; set; } = new();
}
