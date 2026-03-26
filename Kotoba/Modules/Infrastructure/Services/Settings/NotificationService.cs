using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Interfaces;
using Microsoft.JSInterop;

namespace Kotoba.Modules.Infrastructure.Services.Settings
{
    public class NotificationService
    {
        private readonly INotificationSettingsService _settingsService;
        private readonly IJSRuntime _js;

        public NotificationService(INotificationSettingsService settingsService, IJSRuntime js)
        {
            _settingsService = settingsService;
            _js = js;
        }

        public async Task NotifyAsync(NotificationEvent evt, string title, string body)
        {
            var s = await _settingsService.LoadAsync();

            if (!s.MasterEnabled) return;
            if (!IsEventEnabled(evt, s)) return;


            // Browser push notification
            var displayTitle = s.ShowSender ? title : "Kotoba";
            var displayBody = s.ShowPreview ? body : "You have a new message.";

            await _js.InvokeVoidAsync(
                "notifSettings.showNotification",
                displayTitle,
                displayBody);

            // Sound
            if (s.SoundEnabled)
                await _js.InvokeVoidAsync("notifSettings.playSound", s.Volume / 100.0);
        }


        private static bool IsEventEnabled(NotificationEvent evt, NotificationSettingsDto s) => evt switch
        {
            NotificationEvent.DirectMessage => s.DirectMessages,
            NotificationEvent.GroupMessage => s.GroupMessages,
            NotificationEvent.Mention => s.Mentions,
            NotificationEvent.SomeoneOnline => s.SomeoneOnline,
            _ => true
        };

        private static bool IsQuietNow(NotificationSettingsDto s)
        {
            if (!s.QuietHoursEnabled) return false;

            var now = TimeOnly.FromDateTime(DateTime.Now);
            var from = TimeOnly.Parse(s.QuietFrom);
            var to = TimeOnly.Parse(s.QuietTo);

            return from <= to
                ? now >= from && now <= to
                : now >= from || now <= to;
        }
    }

    public enum NotificationEvent
    {
        DirectMessage,
        GroupMessage,
        Mention,
        SomeoneOnline
    }
}
