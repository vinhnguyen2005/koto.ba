using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;

namespace Kotoba.Modules.Domain.Interfaces
{
    public interface IConversationRepository
    {
        Task<IEnumerable<Conversation>> GetAllAsync();
        Task<Conversation?> GetAsync(Guid conversationId);
        Task<ConversationDto?> GetDirectConversationAsync(string userAId, string userBId);        
    }
}
