using Kotoba.Application.DTOs;

namespace Kotoba.Application.Interfaces;

/// <summary>
/// Service for managing conversations (1-1 and group)
/// Owner: Nga (Conversation Management)
/// </summary>
public interface IConversationService
{
    Task<ConversationDto?> CreateDirectConversationAsync(string userAId, string userBId);
    Task<ConversationDto?> CreateGroupConversationAsync(CreateGroupRequest request);
    Task<(List<ConversationDto> Conversations, List<MessageDto> Messages)> GetUserConversationsAsync(string userId);
    Task AddMessage(string conversationId, string messageContent);
    Task<ConversationDto?> GetConversationDetailAsync(Guid conversationId);
}

