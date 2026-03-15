using Kotoba.Modules.Domain.DTOs;

namespace Kotoba.Modules.Domain.Interfaces;

/// <summary>
/// Service for sending and retrieving messages.
/// </summary>
public interface IMessageService
{
    Task<MessageDto?> SendMessageAsync(SendMessageRequest request);
    Task<List<MessageDto>> GetMessagesAsync(Guid conversationId, PagingRequest paging);
    Task<bool> UpdateMessageStatusAsync(UpdateMessageStatusRequest request);
}
