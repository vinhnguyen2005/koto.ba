using Kotoba.Modules.Domain.DTOs;

namespace Kotoba.Modules.Domain.Interfaces;

/// <summary>
/// Service for managing direct and group conversations.
/// </summary>
public interface IConversationService
{
    Task<ConversationDto?> CreateDirectConversationAsync(string userAId, string userBId);
    Task<ConversationDto?> CreateGroupConversationAsync(CreateGroupRequest request);
    Task<List<ConversationDto>> GetUserConversationsAsync(string userId);
    Task<ConversationDto?> GetConversationDetailAsync(string conversationId);
    Task<List<ConversationDto>> FindGroupConversationsAsync(string userId, string searchValue);
    Task<ConversationDto?> FindDirectConversationsAsync(string userAId, string userBId);
    Task<List<UserProfile>> GetOtherUsersInConversationsAsync(string conversationId, string userId);
    Task<List<MessageDto>> GetMessagesAsync(string conversationId);
    Task<List<UserProfile>> GetAllUsersInConversationAsync(string conversationId);

}
