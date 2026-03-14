using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Kotoba.Modules.Infrastructure.Services.Identity
{
    public class PresenceBroadcastService : IPresenceBroadcastService
    {
        private readonly IPresenceService _presenceService;
        private readonly UserManager<User> _userManager;

        public PresenceBroadcastService(IPresenceService presenceService, UserManager<User> userManager)
        {
            _presenceService = presenceService;
            _userManager = userManager;
        }

        public Task<List<PresenceUpdateDto>> GetAllOnlineUsersAsync()
        {
            return GetAllOnlineUsersInternalAsync();
        }

        public Task<PresenceUpdateDto> NotifyUserOfflineAsync(string userId)
        {
            return BuildPresenceUpdateAsync(userId, isOnline: false);
        }

        public Task<PresenceUpdateDto> NotifyUserOnlineAsync(string userId)
        {
            return BuildPresenceUpdateAsync(userId, isOnline: true);
        }

        private async Task<List<PresenceUpdateDto>> GetAllOnlineUsersInternalAsync()
        {
            var onlineUserIds = await _presenceService.GetAllOnlineUsersAsync();
            var updates = new List<PresenceUpdateDto>(onlineUserIds.Count);

            foreach (var userId in onlineUserIds)
            {
                updates.Add(await BuildPresenceUpdateAsync(userId, isOnline: true));
            }

            return updates;
        }

        private async Task<PresenceUpdateDto> BuildPresenceUpdateAsync(string userId, bool isOnline)
        {
            var displayName = userId;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user is not null && !string.IsNullOrWhiteSpace(user.DisplayName))
                {
                    displayName = user.DisplayName;
                }
            }

            return new PresenceUpdateDto
            {
                UserId = userId,
                DisplayName = displayName,
                IsOnline = isOnline,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
