using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Interfaces;

namespace Kotoba.Modules.Infrastructure.Services.Messages
{
    public class MessageService : IMessageService
    {
        public Task<List<MessageDto>> GetMessagesAsync(Guid conversationId, PagingRequest paging)
        {
            throw new NotImplementedException();
        }

        public Task<MessageDto?> SendMessageAsync(SendMessageRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateMessageStatusAsync(UpdateMessageStatusRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
