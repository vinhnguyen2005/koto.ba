using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.Interfaces
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetAllAsync();
        Task AddAsync(Message message);
        Task<Message?> GetAsync(Guid messageId);
        Task<IEnumerable<Message>> GetAllByConversationIdAsync(Guid conversationId);
        Task<IEnumerable<Message>> GetMessagesPageAsync(Guid conversationId, int page, int pageSize);
        Task AddReceiptsAsync(IEnumerable<MessageReceipt> receipts);
        Task<List<MessageReceipt>> GetReceiptsByMessageIdAsync(Guid messageId);
        Task<MessageReceipt?> GetReceiptAsync(Guid messageId, string userId);
        Task UpdateReceiptStatusAsync(Guid messageId, string userId, MessageStatus status, DateTime updatedAtUtc);
    }
}
