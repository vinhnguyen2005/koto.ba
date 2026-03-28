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

        public MessageRepository(IDbContextFactory<KotobaDbContext> factory)
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
                .AsSplitQuery()
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
                .Include(m => m.Attachments)
                .Include(m => m.Reactions)
                .Include(m => m.ReplyToMessage)          // ← thêm
                    .ThenInclude(r => r!.Sender)
                .Include(m => m.ReplyToMessage)          // ← thêm
                    .ThenInclude(r => r!.Attachments)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync();

            return messages.Select(m => new MessageDto
            {
                TempId = m.Id.ToString(),
                MessageId = m.Id,
                SenderId = m.SenderId,
                Content = m.IsRevoked ? string.Empty : m.Content,  // ← thêm
                IsRevoked = m.IsRevoked,                              // ← thêm
                ConversationId = conversationId,
                CreatedAt = m.CreatedAt,
                Status = MessageStatus.Sent,
                IsSystemMessage = m.IsSystemMessage,
                SystemMessageType = m.SystemMessageType,
                SystemMessageData = m.IsSystemMessage && !string.IsNullOrEmpty(m.SystemMessageData)
                    ? JsonSerializer.Deserialize<SystemMessageDataDto>(m.SystemMessageData)
                    : null,
                ReplyToMessageId = m.ReplyToMessageId,                       // ← thêm
                ReplyTo = m.ReplyToMessage == null ? null : new ReplyPreviewDto  // ← thêm
                {
                    MessageId = m.ReplyToMessage.Id,
                    SenderId = m.ReplyToMessage.SenderId,
                    SenderName = m.ReplyToMessage.Sender?.DisplayName ?? "User",
                    Content = m.ReplyToMessage.IsRevoked ? null : m.ReplyToMessage.Content,
                    AttachmentType = m.ReplyToMessage.Attachments?.Any(
                                         a => a.ContentType.StartsWith("image/")) == true
                                     ? "image"
                                     : m.ReplyToMessage.Attachments?.Any() == true ? "file" : null,
                    IsRevoked = m.ReplyToMessage.IsRevoked
                },
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

        public async Task<Message?> GetByIdAsync(Guid id)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            return await ctx.Messages
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Message?> GetByIdWithSenderAsync(Guid id)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            return await ctx.Messages
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task AddAsync(Message message)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            ctx.Messages.Add(message);
            await ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(Message message)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            ctx.Messages.Update(message);
            await ctx.SaveChangesAsync();
        }

        public async Task<List<Message>> GetByConversationAsync(
    Guid conversationId, int page, int pageSize)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            return await ctx.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(m => m.Reactions)
                .Include(m => m.Attachments)
                .Include(m => m.ReplyToMessage) 
                    .ThenInclude(r => r!.Sender)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(r => r!.Attachments)
                .ToListAsync();
        }

        public async Task<bool> IsParticipantAsync(Guid conversationId, string userId)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            return await ctx.ConversationParticipants
                .AnyAsync(p => p.ConversationId == conversationId
                            && p.UserId == userId
                            && p.IsActive);
        }

    }
}
