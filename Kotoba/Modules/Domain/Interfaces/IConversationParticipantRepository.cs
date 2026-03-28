using Kotoba.Modules.Domain.Entities;

namespace Kotoba.Modules.Domain.Interfaces
{
    public interface IConversationParticipantRepository
    {
        Task<IEnumerable<ConversationParticipant>> GetAllAsync();
        Task<bool> IsParticipant(Guid conversationId, string senderId);
    }
}
