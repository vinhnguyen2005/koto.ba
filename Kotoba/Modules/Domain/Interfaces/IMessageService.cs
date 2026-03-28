using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;

namespace Kotoba.Modules.Domain.Interfaces;

/// <summary>
/// Service for sending and retrieving messages.
/// </summary>
public interface IMessageService
{
    Task<MessageDto?> SendMessageAsync(SendMessageRequest request);
    Task<List<MessageDto>> GetMessagesAsync(Guid conversationId, PagingRequest paging);
    Task<bool> UpdateMessageStatusAsync(UpdateMessageStatusRequest request);
    Task RevokeMessageAsync(Guid messageId);
    Task<MessageDto> SendReplyAsync(SendReplyRequest request);
    Task<Message?> GetMessageByIdAsync(Guid messageId);

    public class SendReplyRequest
    {
        public string TempId { get; set; } = string.Empty;
        public Guid ConversationId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string? Content { get; set; }
        public Guid ReplyToMessageId { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new();
    }
}
