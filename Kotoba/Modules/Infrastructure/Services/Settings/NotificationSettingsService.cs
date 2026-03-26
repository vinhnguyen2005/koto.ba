using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Interfaces;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Kotoba.Modules.Infrastructure.Services.Settings
{
    public class NotificationSettingsService : INotificationSettingsService
    {
        private const string StorageKey = "kotoba_notif_settings";
        private readonly IJSRuntime _js;

        public NotificationSettingsService(IJSRuntime js) => _js = js;

        public async Task<NotificationSettingsDto> LoadAsync()
        {
            try
            {
                var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
                if (!string.IsNullOrEmpty(json))
                    return JsonSerializer.Deserialize<NotificationSettingsDto>(json)
                           ?? new NotificationSettingsDto();
            }
            catch { /* first load or SSR — return defaults */ }

            return new NotificationSettingsDto();
        }

        public async Task SaveAsync(NotificationSettingsDto settings)
        {
            var json = JsonSerializer.Serialize(settings);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
    }
}
