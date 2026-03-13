using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kotoba.Domain.Entities;

namespace Kotoba.Domain.Interfaces
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetAllAsync();
        Task AddAsync(Message message);
        Task<Message?> GetAsync(Guid messageId);
        Task<IEnumerable<Message>> GetAllByConversationIdAsync(Guid conversationId);
        Task<IEnumerable<Message>> GetMessagesPageAsync(Guid conversationId, int page, int pageSize);

    }
}
