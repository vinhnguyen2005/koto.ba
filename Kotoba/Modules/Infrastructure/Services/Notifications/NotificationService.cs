using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Repositories;

namespace Kotoba.Modules.Infrastructure.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationRepository _repo;

        public NotificationService(NotificationRepository repo)
        {
            _repo = repo;
        }

        public Task<List<NotificationDto>> GetNotificationsAsync(string recipientId)
            => _repo.GetByRecipientAsync(recipientId);

        public Task<int> GetUnreadCountAsync(string recipientId)
            => _repo.GetUnreadCountAsync(recipientId);

        public async Task<NotificationDto> CreateAsync(CreateNotificationRequest request)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                RecipientId = request.RecipientId,
                Type = request.Type,
                ActorId = request.ActorId,
                TargetId = request.TargetId,
                TargetType = request.TargetType,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(notification);

            return new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                ActorId = notification.ActorId,
                TargetId = notification.TargetId,
                TargetType = notification.TargetType,
                Message = notification.Message,
                IsRead = false,
                CreatedAt = notification.CreatedAt
            };
        }

        public Task MarkAsReadAsync(Guid notificationId)
            => _repo.MarkAsReadAsync(notificationId);

        public Task MarkAllAsReadAsync(string recipientId)
            => _repo.MarkAllAsReadAsync(recipientId);
    }
}
