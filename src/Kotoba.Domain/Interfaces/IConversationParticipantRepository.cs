using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kotoba.Domain.Entities;

namespace Kotoba.Domain.Interfaces
{
    public interface IConversationParticipantRepository
    {
        Task<IEnumerable<ConversationParticipant>> GetAllAsync();
        Task<bool> IsParticipant(Guid conversationId, string senderId);
    }
}
