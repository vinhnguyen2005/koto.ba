using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Hubs;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Services.Follows
{
    public class FollowService : IFollowService
    {
        private readonly KotobaDbContext _context;
        private readonly IHubContext<NotificationHub> _notifHub;
        private readonly IUserService _userService;
        public FollowService(KotobaDbContext context,
            IHubContext<NotificationHub> notifHub,
            IUserService userService)
        {
            _context = context;
            _notifHub = notifHub;
            _userService = userService;
        }
        public async Task FollowAsync(string followerId, string followingId)
        {
            if (followerId == followingId) return;

            var exists = await _context.Follows
                .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (exists) return;

            var follow = new Follow
            {
                FollowerId = followerId,
                FollowingId = followingId
            };

            _context.Follows.Add(follow);

            var follower = await _userService.GetUserProfileAsync(followerId);

            var existing = await _context.Notifications
                .Where(n =>
                    n.RecipientId == followingId &&
                    n.ActorId == followerId &&
                    n.Type == NotificationType.NewFollower)
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.CreatedAt = DateTime.UtcNow;
                existing.IsRead = false;

                _context.Notifications.Update(existing);
            }
            else
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    RecipientId = followingId,
                    ActorId = followerId,
                    Type = NotificationType.NewFollower,
                    TargetId = followerId,
                    TargetType = "User",
                    Message = $"{follower.DisplayName} started following you",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);

                await _notifHub.Clients.Group(followingId)
                    .SendAsync("NotifyFollow", new NotificationDto
                    {
                        Id = notification.Id,
                        Type = notification.Type,
                        Message = notification.Message,
                        CreatedAt = notification.CreatedAt,
                        ActorId = followerId
                    });
            }

            await _context.SaveChangesAsync();
        }

        public async Task UnfollowAsync(string followerId, string followingId)
        {
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (follow != null)
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsFollowingAsync(string followerId, string followingId)
        {
            return await _context.Follows
                .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
        }

        public async Task<int> GetFollowersCount(string userId)
        {
            return await _context.Follows
                .CountAsync(f => f.FollowingId == userId);
        }

        public async Task<List<string>> GetFollowingIdsAsync(string userId)
        {
            return await _context.Follows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();
        }
    }
}
