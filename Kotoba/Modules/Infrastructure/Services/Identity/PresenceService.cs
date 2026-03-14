using Kotoba.Modules.Domain.Interfaces;
using System.Collections.Concurrent;

namespace Kotoba.Modules.Infrastructure.Services.Identity
{
    public class PresenceService : IPresenceService
    {
        private readonly ConcurrentDictionary<string, DateTime> _onlineUsers = new();

        public Task<List<string>> GetAllOnlineUsersAsync()
        {
            return Task.FromResult(_onlineUsers.Keys.ToList());
        }

        public Task<bool> GetUserPresenceAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(_onlineUsers.ContainsKey(userId));
        }

        public Task SetOfflineAsync(string userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                _onlineUsers.TryRemove(userId, out _);
            }

            return Task.CompletedTask;
        }

        public Task SetOnlineAsync(string userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                _onlineUsers.AddOrUpdate(userId, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
            }

            return Task.CompletedTask;
        }
    }
}
