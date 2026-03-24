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

        public ChatHub(KotobaDbContext context, IReactionService reactionService)
        {
            _context = context;
            _reactionService = reactionService;
        }

        // Join room theo conversationId
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task SendMessage(SendMessageRequest request)
        {
            var userId = Context.UserIdentifier!;
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = request.ConversationId,
                SenderId = userId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            var dto = new MessageDto
            {
                TempId = request.TempId,
                MessageId = message.Id,
                SenderId = userId,
                Content = request.Content,
                ConversationId = request.ConversationId,
                CreatedAt = message.CreatedAt,
                Status = MessageStatus.Sent
            };

            await Clients
                .Group(request.ConversationId.ToString())
                .SendAsync("MessageConfirmed", dto, request.TempId);
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

        private async Task AssertParticipantAsync(Guid conversationId, string userId)
        {
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(p => p.ConversationId == conversationId
                            && p.UserId == userId
                            && p.IsActive);

            if (!isParticipant)
                throw new HubException("Access denied.");
        }

        public async Task SendTyping(Guid conversationId)
        {
            var userId = Context.UserIdentifier!;
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
            await Clients
                .OthersInGroup(conversationId.ToString())
                .SendAsync("UserTyping", new TypingStatusDto
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    IsTyping = false
                });
        }
    }
}
