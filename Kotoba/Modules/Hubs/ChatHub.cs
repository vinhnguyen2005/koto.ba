using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Hubs
{
    public class ChatHub : Hub
    {
        private readonly KotobaDbContext _context;
        private readonly IReactionService _reactionService;
        private readonly ICurrentThoughtService _thoughtService;
        public ChatHub(KotobaDbContext context, IReactionService reactionService, ICurrentThoughtService thoughtService)
        {
            _context = context;
            _reactionService = reactionService;
            _thoughtService = thoughtService;
        }

        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task SendMessage(string tempId, string conversationId, string senderId, string content, List<AttachmentDto> uploadedFiles)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("User not authenticated.");

            await AssertUserCanWriteAsync(userId);

            var convId = Guid.Parse(conversationId);
            var conversation = await _context.Conversations
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == convId);

            if (conversation == null)
                throw new HubException("Conversation not found.");

            var isParticipant = conversation.Participants
                .Any(p => p.UserId == userId && p.IsActive);

            if (!isParticipant)
                throw new HubException("Access denied.");

            if (conversation.Type == ConversationType.Direct)
            {
                var other = conversation.Participants
                    .FirstOrDefault(p => p.UserId != userId && p.IsActive);

                if (other?.User?.AccountStatus == AccountStatus.Deleted)
                {
                    throw new HubException("Cannot send messages to a deleted account.");
                }
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = convId,
                SenderId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Messages.AddAsync(message);

            var finalAttachmentDtos = new List<AttachmentDto>();
            if (uploadedFiles != null && uploadedFiles.Any())
            {
                foreach (var file in uploadedFiles)
                {
                    var attachment = new Attachment
                    {
                        Id = Guid.NewGuid(),
                        MessageId = message.Id,
                        FileName = file.FileName,
                        SavedName = Path.GetFileName(file.Url),
                        ContentType = file.ContentType,
                        Url = file.Url,
                        Size = file.Size
                    };
                    _context.Attachments.Add(attachment);
                    finalAttachmentDtos.Add(new AttachmentDto
                    {
                        Id = attachment.Id,
                        FileName = attachment.FileName,
                        ContentType = attachment.ContentType,
                        Url = attachment.Url,
                        Size = attachment.Size
                    });
                }
            }

            await _context.SaveChangesAsync();

            var dto = new MessageDto
            {
                TempId = tempId,
                MessageId = message.Id,
                SenderId = userId,
                Content = content,
                ConversationId = convId,
                CreatedAt = message.CreatedAt,
                Status = MessageStatus.Sent,
                Attachments = finalAttachmentDtos
            };

            await Clients.Group(conversationId).SendAsync("MessageConfirmed", dto, tempId);
        }

        public async Task ReactToMessage(Guid conversationId, Guid messageId, ReactionType reactionType)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            await AssertParticipantAsync(conversationId, userId);

            var reaction = await _reactionService.AddOrUpdateReactionAsync(userId, messageId, reactionType);
            if (reaction == null)
                throw new HubException("Message not found.");

            await Clients
                .Group(conversationId.ToString())
                .SendAsync("ReactionUpdated", reaction);
        }

        public async Task RemoveReaction(Guid conversationId, Guid messageId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            await AssertParticipantAsync(conversationId, userId);

            var removed = await _reactionService.RemoveReactionAsync(userId, messageId);
            if (!removed)
                throw new HubException("Message not found or no reaction to remove.");

            await Clients
                .Group(conversationId.ToString())
                .SendAsync("ReactionRemoved", new { messageId, userId });
        }

        public async Task SendTyping(Guid conversationId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            await Clients
                .OthersInGroup(conversationId.ToString())
                .SendAsync("UserTyping", new TypingStatusDto
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    IsTyping = true
                });
        }

        public async Task StopTyping(Guid conversationId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            await Clients
                .OthersInGroup(conversationId.ToString())
                .SendAsync("UserTyping", new TypingStatusDto
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    IsTyping = false
                });
        }
        public async Task UpdateThought(string content)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            await _thoughtService.SetThoughtAsync(userId, content);
            await Clients.All.SendAsync("ThoughtUpdated", new { userId, content });
        }

        private async Task AssertParticipantAsync(Guid conversationId, string userId)
        {
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(p => p.ConversationId == conversationId
                            && p.UserId == userId
                            && p.IsActive);

            if (!isParticipant)
                throw new HubException("Access denied.");
        }

        private async Task AssertUserCanWriteAsync(string userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new HubException("User not found.");

            if (user.AccountStatus == AccountStatus.Deleted)
                throw new HubException("Account is deleted.");

            if (user.AccountStatus == AccountStatus.Deactivated)
                throw new HubException("Account is deactivated.");
        }

        private string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }
    }
}
