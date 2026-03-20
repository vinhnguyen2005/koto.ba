using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Services.Realtime
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly KotobaDbContext _db;
        private readonly IPresenceBroadcastService _presenceBroadcastService;
        private readonly IReactionService _reactionService;
        private readonly IMessageService _messageService;

        public ChatHub(
            KotobaDbContext db,
            IPresenceBroadcastService presenceBroadcastService,
            IReactionService reactionService,
            IMessageService messageService)
        {
            _db = db;
            _presenceBroadcastService = presenceBroadcastService;
            _reactionService = reactionService;
            _messageService = messageService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var update = await _presenceBroadcastService.NotifyUserOnlineAsync(userId);
                await Clients.All.SendAsync("PresenceChanged", update);

                var onlineUsers = await _presenceBroadcastService.GetAllOnlineUsersAsync();
                await Clients.Caller.SendAsync("OnlineUsersSnapshot", onlineUsers);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var update = await _presenceBroadcastService.NotifyUserOfflineAsync(userId);
                await Clients.All.SendAsync("PresenceChanged", update);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinConversation(Guid conversationId)
        {
            var userId = Context.UserIdentifier;

            var isParticipant = await _db.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

            if (!isParticipant)
                throw new HubException("You are not a participant of this conversation.");

            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        public async Task LeaveConversation(Guid conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        public async Task<ConversationDto> CreateDirectConversation(string targetUserId)
        {
            var currentUserId = Context.UserIdentifier!;
            if (currentUserId == targetUserId)
                throw new HubException("You cannot create a conversation with yourself.");

            var existing = await _db.Conversations
                .Include(c => c.Participants)
                .Where(c => c.Type == ConversationType.Direct
                    && c.Participants.Any(p => p.UserId == currentUserId && p.IsActive)
                    && c.Participants.Any(p => p.UserId == targetUserId && p.IsActive))
                .FirstOrDefaultAsync();

            if (existing != null) return await MapConversationDto(existing);

            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Direct,
                CreatedAt = DateTime.UtcNow
            };

            _db.Conversations.Add(conversation);
            _db.ConversationParticipants.AddRange(
                new ConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = currentUserId
                },
                new ConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = targetUserId
                }
            );
            await _db.SaveChangesAsync();

            var created = await _db.Conversations
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .FirstAsync(c => c.Id == conversation.Id);

            return await MapConversationDto(created);
        }

        public async Task<List<ConversationDto>> GetConversations()
        {
            var userId = Context.UserIdentifier!;

            var conversations = await _db.Conversations
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                    .ThenInclude(m => m.Sender)
                .Where(c => c.Participants.Any(p => p.UserId == userId && p.IsActive))
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .ToListAsync();

            var result = new List<ConversationDto>();
            foreach (var c in conversations)
                result.Add(await MapConversationDto(c));

            return result;
        }

        public async Task SendMessage(SendMessageRequest request)
        {
            var userId = Context.UserIdentifier!;
            request.SenderId = userId;

            var message = await _messageService.SendMessageAsync(request);
            if (message == null)
                throw new HubException("Access denied or conversation not found.");

            await Clients
                .Group(request.ConversationId.ToString())
                .SendAsync("MessageConfirmed", message, request.TempId);
        }

        public async Task ReactToMessage(Guid conversationId, Guid messageId, ReactionType reactionType)
        {
            var userId = Context.UserIdentifier!;
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
            await AssertParticipantAsync(conversationId, userId);

            var removed = await _reactionService.RemoveReactionAsync(userId, messageId);
            if (!removed)
                throw new HubException("Message not found or no reaction to remove.");

            await Clients
                .Group(conversationId.ToString())
                .SendAsync("ReactionRemoved", new { messageId, userId });
        }

        // ── helpers ──────────────────────────────────────────────────────────
        private async Task AssertParticipantAsync(Guid conversationId, string userId)
        {
            var isParticipant = await _db.ConversationParticipants
                .AnyAsync(p => p.ConversationId == conversationId
                            && p.UserId == userId
                            && p.IsActive);

            if (!isParticipant)
                throw new HubException("Access denied.");
        }

        private async Task<ConversationDto> MapConversationDto(Conversation c)
        {
            var lastMessage = c.Messages
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            MessageDto? lastMsgDto = null;
            if (lastMessage != null)
            {
                lastMsgDto = new MessageDto
                {
                    MessageId = lastMessage.Id,
                    ConversationId = lastMessage.ConversationId,
                    SenderId = lastMessage.SenderId,
                    Content = lastMessage.Content,
                    CreatedAt = lastMessage.CreatedAt,
                    Status = MessageStatus.Sent
                };
            }

            return new ConversationDto
            {
                ConversationId = c.Id,
                Type = c.Type,
                GroupName = c.GroupName,
                Participants = c.Participants.Select(p => new UserProfile
                {
                    UserId = p.UserId,
                    DisplayName = p.User.DisplayName,
                    AvatarUrl = p.User.AvatarUrl,
                    IsOnline = p.User.IsOnline,
                    LastSeenAt = p.User.LastSeenAt
                }).ToList(),
                LastMessage = lastMsgDto,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            };
        }
    }
}
