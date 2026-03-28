using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Repositories
{
    public class NotificationRepository
    {
        private readonly IDbContextFactory<KotobaDbContext> _factory;

        public NotificationRepository(IDbContextFactory<KotobaDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<NotificationDto>> GetByRecipientAsync(string recipientId)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            return await ctx.Notifications
                .Where(n => n.RecipientId == recipientId)
                .OrderByDescending(n => n.CreatedAt)
                .Include(n => n.Actor)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    ActorId = n.ActorId,
                    ActorName = n.Actor != null ? n.Actor.DisplayName : null,
                    ActorAvatar = n.Actor != null ? n.Actor.AvatarUrl : null,
                    TargetId = n.TargetId,
                    TargetType = n.TargetType,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string recipientId)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            return await ctx.Notifications
                .CountAsync(n => n.RecipientId == recipientId && !n.IsRead);
        }

        public async Task AddAsync(Notification notification)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            ctx.Notifications.Add(notification);
            await ctx.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            var n = await ctx.Notifications.FindAsync(notificationId);
            if (n == null) return;
            n.IsRead = true;
            await ctx.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(string recipientId)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            await ctx.Notifications
                .Where(n => n.RecipientId == recipientId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }
    }
}
