using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kotoba.Modules.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IDbContextFactory<KotobaDbContext> _factory;

        public MessageRepository(KotobaDbContext context, IDbContextFactory<KotobaDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<IEnumerable<Message>> GetAllAsync()
        {
            await using var _context = await _factory.CreateDbContextAsync();
            return await _context.Messages
                .Include(m => m.Receipts)
                .ToListAsync();
        }

        public async Task<Message?> GetAsync(Guid messageId)
        {
            await using var _context = await _factory.CreateDbContextAsync();
            return await _context.Messages
                .Include(m => m.Receipts)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public async Task<IEnumerable<Message>> GetAllByConversationIdAsync(Guid conversationId)
        {
            await using var _context = await _factory.CreateDbContextAsync();
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.Receipts)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Message message)
        {
            await using var _context = await _factory.CreateDbContextAsync();
            await _context.Messages.AddAsync(message);
        }

        public async Task<IEnumerable<Message>> GetMessagesPageAsync(Guid conversationId, int page, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(page, 1);
            await using var _context = await _factory.CreateDbContextAsync();

            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(m => m.Reactions)
                .Include(m => m.Attachments)
                .Include(m => m.Receipts)
                .AsNoTracking()
                .ToListAsync();

            return messages
                .OrderBy(m => m.CreatedAt)
                .ToList();
        }

        public async Task AddReceiptsAsync(IEnumerable<MessageReceipt> receipts)
        {
            await using var _context = await _factory.CreateDbContextAsync();
            await _context.MessageReceipts.AddRangeAsync(receipts);
        }

        public async Task<List<MessageReceipt>> GetReceiptsByMessageIdAsync(Guid messageId)
        {
            await using var _context = await _factory.CreateDbContextAsync();
            return await _context.MessageReceipts
                .Where(r => r.MessageId == messageId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<MessageReceipt?> GetReceiptAsync(Guid messageId, string userId)
        {
            await using var _context = await _factory.CreateDbContextAsync();
            return await _context.MessageReceipts
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);
        }

        public async Task UpdateReceiptStatusAsync(Guid messageId, string userId, MessageStatus status, DateTime updatedAtUtc)
        {
            var receipt = await GetReceiptAsync(messageId, userId);
            if (receipt is null)
            {
                return;
            }

            receipt.Status = status;
            receipt.UpdatedAt = updatedAtUtc;

            if (status == MessageStatus.Received && receipt.ReceivedAt is null)
            {
                receipt.ReceivedAt = updatedAtUtc;
            }

            if (status == MessageStatus.Read)
            {
                receipt.ReceivedAt ??= updatedAtUtc;
                receipt.ReadAt ??= updatedAtUtc;
            }
        }

        public async Task<List<MessageDto>> GetMessagesAsync(Guid conversationId)
        {
            await using var _context = await _factory.CreateDbContextAsync();
            var messages = await _context.Messages
                    .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                    .OrderBy(m => m.CreatedAt)
                    .Include(m => m.Sender)
                    .Include(m => m.Attachments)
                    .Include(m => m.Reactions)
                    .AsNoTracking()
                    .ToListAsync();

            return messages.Select(m => new MessageDto
            {
                TempId = m.Id.ToString(),
                MessageId = m.Id,
                SenderId = m.SenderId,
                Content = m.Content,
                ConversationId = conversationId,
                CreatedAt = m.CreatedAt,
                Status = MessageStatus.Sent,
                IsSystemMessage = m.IsSystemMessage,
                SystemMessageType = m.SystemMessageType,
                SystemMessageData = m.IsSystemMessage && !string.IsNullOrEmpty(m.SystemMessageData)
                    ? JsonSerializer.Deserialize<SystemMessageDataDto>(m.SystemMessageData) 
                    : null,
                Attachments = m.Attachments.Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    Url = a.Url,
                    Size = a.Size
                }).ToList(),
                Reactions = m.Reactions.Select(r => new ReactionDto
                {
                    MessageId = r.MessageId,
                    UserId = r.UserId,
                    Type = r.Type
                }).ToList()
            }).ToList();
        }
    }
}
