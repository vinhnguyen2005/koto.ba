using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.Interfaces
{
    public interface IGroupAdminService
    {
        Task<GroupRole?> GetUserRoleAsync(Guid conversationId, string userId);
        Task<bool> IsOwnerAsync(Guid conversationId, string userId);
        Task<bool> IsAdminAsync(Guid conversationId, string userId);
        Task<bool> IsOwnerOrAdminAsync(Guid conversationId, string userId);
        Task<bool> PromoteToAdminAsync(Guid conversationId, string targetUserId);
        Task<bool> DemoteFromAdminAsync(Guid conversationId, string targetUserId);
        Task<bool> TransferOwnershipAsync(Guid conversationId, string currentOwnerId, string newOwnerId);
        Task<bool> UpdateGroupNameAsync(Guid conversationId, string newName);
        Task<string?> AutoTransferOwnershipOnLeaveAsync(Guid conversationId);
        Task<List<string>> GetAdminsAsync(Guid conversationId);
        Task<bool> AddMemberAsync(Guid conversationId, string userId);
        Task<bool> RemoveMemberAsync(Guid conversationId, string userId); 
        Task<bool> LeaveConversationAsync(Guid conversationId, string userId);
    }
}
