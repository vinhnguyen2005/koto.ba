using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Identity.Client;

namespace Kotoba.Modules.Hubs
{
    public class ChatHub : Hub
    {
        //private KotobaDbContext _context;

        public async Task SendMessage(string user, string content)
        {
            //var senderId = Context.UserIdentifier!;

            //var message = new Message
            //{
            //    Id = Guid.NewGuid(),
            //    ConversationId = conversationId,
            //    SenderId = senderId,
            //    Content = content,
            //    CreatedAt = DateTime.UtcNow
            //};

            //await _context.Messages.AddAsync(message);
            //await _context.SaveChangesAsync();

            //await Clients.Users(senderId).SendAsync("MessageConfirmed", new
            //{
            //    TempId = tempId,
            //    MessageId = message.Id,
            //    CreatedAt = message.CreatedAt
            //});

            //await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
            //{
            //    ConversationId = conversationId,
            //    Content = content,
            //    TempId = tempId
            //});

            await Clients.All.SendAsync("ReceiveMessage", user, content);



        }
    }
}
