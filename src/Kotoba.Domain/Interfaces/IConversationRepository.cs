using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kotoba.Domain.Entities;

namespace Kotoba.Domain.Interfaces
{
    public interface IConversationRepository
    {
        Task<IEnumerable<Conversation>> GetAllAsync();
        Task<Conversation?> GetAsync(Guid conversationId);
    }
}
