using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Identity.Client;

namespace Kotoba.Modules.Hubs
{
    public class ChatHub : Hub
    {
        private readonly KotobaDbContext _context;

        public ChatHub(KotobaDbContext context)
        {
            _context = context;
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

        public async Task SendMessage(string tempId, string conversationId, string senderId, string content)
        {
            // Lưu DB
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = Guid.Parse(conversationId),
                SenderId = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            var dto = new MessageDto
            {
                TempId = tempId,
                MessageId = message.Id,
                SenderId = senderId,
                Content = content,
                ConversationId = Guid.Parse(conversationId),
                CreatedAt = message.CreatedAt,
                Status = MessageStatus.Sent
            };

            // Broadcast chỉ trong group (conversation)
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", dto);
        }
    }
}
