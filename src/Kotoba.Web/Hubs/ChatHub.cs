using Microsoft.AspNetCore.SignalR;

namespace Kotoba.Web.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinConversation(string conversationId)
        {
            Console.WriteLine(
                $"Client {Context.ConnectionId} joined {conversationId}"
            );

            await Clients.Caller.SendAsync(
                "UserJoined",
                new { ConversationId = conversationId, User = "test-user" }
            );
        }

        public async Task SendTestMessage(string message)
        {
            await Clients.All.SendAsync(
                "MessageSent",
                new
                {
                    MessageId = Guid.NewGuid(),
                    Content = message,
                    Time = DateTime.UtcNow
                }
            );
        }

        public async Task SendTestLeave()
        {
            await Clients.All.SendAsync(
                "UserLeft",
                new { User = "test-user" }
            );
        }
    }
}
