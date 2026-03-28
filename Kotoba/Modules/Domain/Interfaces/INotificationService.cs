using Kotoba.Modules.Domain.DTOs;

namespace Kotoba.Modules.Domain.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetNotificationsAsync(string recipientId);
        Task<int> GetUnreadCountAsync(string recipientId);
        Task<NotificationDto> CreateAsync(CreateNotificationRequest request);
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync(string recipientId);
    }

}
