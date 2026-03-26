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
            var participants = await _context.ConversationParticipants
                .Where(p => p.ConversationId.ToString() == conversationId && p.IsActive)
                .Select(p => p.UserId)
                .ToListAsync();
            await Clients.Users(participants).SendAsync("ConversationListChanged");
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
        public async Task NotifyGroupCreated(List<string> participantIds)
        {
            foreach (var userId in participantIds)
            {
                await Clients.User(userId).SendAsync("ConversationListChanged");
            }
        }
        public async Task KickMember(string conversationId, string targetUserId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            var convId = Guid.Parse(conversationId);

            var targetUser = await _context.Users.FindAsync(targetUserId);
            var displayName = targetUser?.DisplayName ?? "User";

            var systemMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = convId,
                SenderId = userId,
                Content = $"{displayName} has been removed",
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.MemberRemoved,
                SystemMessageData = System.Text.Json.JsonSerializer.Serialize(
                    new SystemMessageDataDto { UserId = targetUserId, DisplayName = displayName }
                )
            };
            await _context.Messages.AddAsync(systemMsg);

            await _context.ConversationParticipants
                .Where(p => p.ConversationId == convId && p.UserId == targetUserId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.IsActive, false)
                    .SetProperty(p => p.LeftAt, DateTime.UtcNow));

            await _context.SaveChangesAsync();

            // Notify bị kick
            await Clients.User(targetUserId).SendAsync("RemovedFromGroup", conversationId);

            // Broadcast system message
            var sysMsgDto = new MessageDto
            {
                MessageId = systemMsg.Id,
                SenderId = userId,
                Content = systemMsg.Content,
                ConversationId = convId,
                CreatedAt = systemMsg.CreatedAt,
                Status = MessageStatus.Sent,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.MemberRemoved,
                SystemMessageData = new SystemMessageDataDto { UserId = targetUserId, DisplayName = displayName }
            };

            await Clients.Group(conversationId).SendAsync("MessageConfirmed", sysMsgDto, systemMsg.Id.ToString());
            await Clients.Group(conversationId).SendAsync("ConversationListChanged");
            await Clients.Group(conversationId).SendAsync("MembersUpdated");
        }

        public async Task LeaveGroup(string conversationId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            var convId = Guid.Parse(conversationId);

            var user = await _context.Users.FindAsync(userId);
            var displayName = user?.DisplayName ?? "User";

            var systemMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = convId,
                SenderId = userId,
                Content = $"{displayName} has left the chat",
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.UserLeft,
                SystemMessageData = System.Text.Json.JsonSerializer.Serialize(
                    new SystemMessageDataDto { UserId = userId, DisplayName = displayName }
                )
            };
            await _context.Messages.AddAsync(systemMsg);

            await _context.ConversationParticipants
                .Where(p => p.ConversationId == convId && p.UserId == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.IsActive, false)
                    .SetProperty(p => p.LeftAt, DateTime.UtcNow));

            await _context.SaveChangesAsync();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);

            var sysMsgDto = new MessageDto
            {
                MessageId = systemMsg.Id,
                SenderId = userId,
                Content = systemMsg.Content,
                ConversationId = convId,
                CreatedAt = systemMsg.CreatedAt,
                Status = MessageStatus.Sent,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.UserLeft,
                SystemMessageData = new SystemMessageDataDto { UserId = userId, DisplayName = displayName }
            };

            await Clients.OthersInGroup(conversationId).SendAsync("MessageConfirmed", sysMsgDto, systemMsg.Id.ToString());
            await Clients.Group(conversationId).SendAsync("ConversationListChanged");
            await Clients.OthersInGroup(conversationId).SendAsync("MembersUpdated");
        }

        public async Task NotifyMemberAdded(string conversationId, string addedUserId, string addedUserName)
        {
            var convId = Guid.Parse(conversationId);

            var systemMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = convId,
                SenderId = addedUserId,
                Content = $"{addedUserName} is added to the group",
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.MemberAdded,

                SystemMessageData = System.Text.Json.JsonSerializer.Serialize(
                    new SystemMessageDataDto { UserId = addedUserId, DisplayName = addedUserName }
                )
            };
            await _context.Messages.AddAsync(systemMsg);
            await _context.SaveChangesAsync();

            var sysMsgDto = new MessageDto
            {
                MessageId = systemMsg.Id,
                SenderId = addedUserId,
                Content = systemMsg.Content,
                ConversationId = convId,
                CreatedAt = systemMsg.CreatedAt,
                Status = MessageStatus.Sent,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.MemberAdded,
                SystemMessageData = new SystemMessageDataDto { UserId = addedUserId, DisplayName = addedUserName }
            };

            await Clients.Group(conversationId).SendAsync("MessageConfirmed", sysMsgDto, systemMsg.Id.ToString());
            await Clients.User(addedUserId).SendAsync("ConversationListChanged");
            await Clients.Group(conversationId).SendAsync("ConversationListChanged");
            await Clients.Group(conversationId).SendAsync("MembersUpdated");
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
