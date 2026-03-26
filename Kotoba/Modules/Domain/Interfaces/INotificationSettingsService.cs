using Kotoba.Modules.Domain.DTOs;

namespace Kotoba.Modules.Domain.Interfaces
{
    public interface INotificationSettingsService
    {
        Task<NotificationSettingsDto> LoadAsync();
        Task SaveAsync(NotificationSettingsDto settings);
    }
}
