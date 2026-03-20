using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Services.Messages
{
    public class MessageService : IMessageService
    {
        private readonly KotobaDbContext _context;

        public MessageService(KotobaDbContext context)
        {
            _context = context;
        }

        public async Task<MessageDto?> SendMessageAsync(SendMessageRequest request)
        {
            // TODO: Implement send message
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(p => p.ConversationId == request.ConversationId
                            && p.UserId == request.SenderId
                            && p.IsActive);

            if (!isParticipant) return null;
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = request.ConversationId,
                SenderId = request.SenderId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };
            _context.Messages.Add(message);
            var conversation = await _context.Conversations.FindAsync(request.ConversationId);
            if (conversation != null)
            {
                conversation.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return new MessageDto
            {
                MessageId = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                Status = MessageStatus.Sent,
                Reactions = new List<ReactionDto>(),
                Attachments = new List<AttachmentDto>()
            };
        }

        public async Task<List<MessageDto>> GetMessagesAsync(Guid conversationId, PagingRequest paging)
        {
            paging.PageSize = Math.Clamp(paging.PageSize, 1, 100);
            paging.Page = Math.Max(paging.Page, 1);

            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((paging.Page - 1) * paging.PageSize)
                .Take(paging.PageSize)
                .Include(m => m.Reactions)
                .Include(m => m.Attachments)
                .ToListAsync();
            return messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageDto
                {
                    MessageId = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    Status = MessageStatus.Sent,
                    Reactions = m.Reactions.Select(r => new ReactionDto
                    {
                        MessageId = r.MessageId,
                        UserId = r.UserId,
                        Type = r.Type,
                        CreatedAt = r.CreatedAt
                    }).ToList(),
                    Attachments = m.Attachments.Select(a => new AttachmentDto
                    {
                        AttachmentId = a.Id,
                        MessageId = a.MessageId,
                        FileName = a.FileName,
                        FileType = a.FileType,
                        FileUrl = a.FileUrl
                    }).ToList()
                })
                .ToList();
        }

        public Task<bool> UpdateMessageStatusAsync(UpdateMessageStatusRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
