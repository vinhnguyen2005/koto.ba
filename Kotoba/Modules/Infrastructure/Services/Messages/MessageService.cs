using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Kotoba.Modules.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using static Kotoba.Modules.Domain.Interfaces.IMessageService;

namespace Kotoba.Modules.Infrastructure.Services.Messages
{
    public class MessageService : IMessageService
    {
        private readonly MessageRepository _messageRepository;
        private readonly UserProfileRepository _userRepository;
        private readonly ConversationRepository _conversationRepository;

        public MessageService(
            MessageRepository messageRepository,
            UserProfileRepository userRepository,
            ConversationRepository conversationRepository)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _conversationRepository = conversationRepository;
        }

        public async Task<MessageDto?> SendMessageAsync(SendMessageRequest request)
        {
            var isParticipant = await _messageRepository
                .IsParticipantAsync(request.ConversationId, request.SenderId);
            if (!isParticipant) return null;

            var user = await _userRepository.GetByIdAsync(request.SenderId);
            if (user == null || user.AccountStatus != AccountStatus.Active)
                return null;

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = request.ConversationId,
                SenderId = request.SenderId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(message);
            await _conversationRepository.TouchUpdatedAtAsync(request.ConversationId);

            return MapToDto(message, replyPreview: null);
        }

        public async Task<List<MessageDto>> GetMessagesAsync(
            Guid conversationId, PagingRequest paging)
        {
            paging.PageSize = Math.Clamp(paging.PageSize, 1, 100);
            paging.Page = Math.Max(paging.Page, 1);

            var messages = await _messageRepository
                .GetByConversationAsync(conversationId, paging.Page, paging.PageSize);

            return messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => MapToDto(m, BuildReplyPreview(m.ReplyToMessage)))
                .ToList();
        }

        public async Task RevokeMessageAsync(Guid messageId)
        {
            var msg = await _messageRepository.GetByIdAsync(messageId);
            if (msg == null) return;

            msg.IsRevoked = true;
            msg.RevokedAt = DateTime.UtcNow;
            msg.Content = string.Empty;

            await _messageRepository.UpdateAsync(msg);
        }

        public async Task<MessageDto> SendReplyAsync(SendReplyRequest request)
        {
            // Lấy message gốc để build preview
            var original = await _messageRepository.GetByIdWithSenderAsync(request.ReplyToMessageId);

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = request.ConversationId,
                SenderId = request.SenderId,
                Content = request.Content ?? string.Empty,
                ReplyToMessageId = request.ReplyToMessageId,
                CreatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(message);

            // Build ReplyPreview
            ReplyPreviewDto? preview = null;
            if (original != null)
            {
                preview = new ReplyPreviewDto
                {
                    MessageId = original.Id,
                    SenderId = original.SenderId,
                    SenderName = original.Sender?.DisplayName ?? "User",
                    Content = original.IsRevoked ? null : original.Content,
                    AttachmentType = original.Attachments?.Any(a => a.ContentType.StartsWith("image/")) == true
                                     ? "image" :
                                     original.Attachments?.Any() == true ? "file" : null,
                    IsRevoked = original.IsRevoked
                };
            }

            return new MessageDto
            {
                MessageId = message.Id,
                TempId = request.TempId,
                SenderId = message.SenderId,
                Content = message.Content,
                ConversationId = message.ConversationId,
                CreatedAt = message.CreatedAt,
                Status = MessageStatus.Sent,
                ReplyToMessageId = message.ReplyToMessageId,
                ReplyTo = preview
            };
        }

        public async Task<Message?> GetMessageByIdAsync(Guid messageId)
            => await _messageRepository.GetByIdAsync(messageId);

        // ────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────

        private static MessageDto MapToDto(Message m,
            ReplyPreviewDto? replyPreview, string tempId = "")
            => new()
            {
                TempId = tempId,
                MessageId = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                Content = m.IsRevoked ? string.Empty : m.Content,
                CreatedAt = m.CreatedAt,
                Status = MessageStatus.Sent,
                IsRevoked = m.IsRevoked,
                ReplyToMessageId = m.ReplyToMessageId,
                ReplyTo = replyPreview,
                Reactions = m.Reactions?.Select(r => new ReactionDto
                {
                    MessageId = r.MessageId,
                    UserId = r.UserId,
                    Type = r.Type,
                    CreatedAt = r.CreatedAt
                }).ToList() ?? new(),
                Attachments = m.Attachments?.Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    MessageId = a.MessageId,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    Url = a.Url
                }).ToList() ?? new()
            };

        private static ReplyPreviewDto? BuildReplyPreview(Message? original)
        {
            if (original == null) return null;

            return new ReplyPreviewDto
            {
                MessageId = original.Id,
                SenderId = original.SenderId,
                SenderName = original.Sender?.DisplayName ?? "User",
                Content = original.IsRevoked ? null : original.Content,
                AttachmentType = original.Attachments?.Any(
                                     a => a.ContentType.StartsWith("image/")) == true
                                 ? "image"
                                 : original.Attachments?.Any() == true ? "file" : null,
                IsRevoked = original.IsRevoked
            };
        }

        public Task<bool> UpdateMessageStatusAsync(UpdateMessageStatusRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
