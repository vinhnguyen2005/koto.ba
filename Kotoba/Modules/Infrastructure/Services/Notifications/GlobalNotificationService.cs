using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Services.Settings;

namespace Kotoba.Modules.Infrastructure.Services.Notifications;

/// <summary>
/// Scoped service (one per browser tab / Blazor circuit).
/// Holds a permanent SignalR connection so notification sounds fire on
/// every page, not just when ChatMain is mounted.
/// </summary>
public class GlobalNotificationService : IAsyncDisposable
{
    public event Action<PresenceUpdateDto>? PresenceChanged;

    private readonly INotificationSettingsService _settings;
    private readonly IJSRuntime _js;
    private readonly CircuitCookieService _cookieSvc;
    private readonly IUserService _userService;
    private readonly ChatNotificationState _state;

    private HubConnection? _hub;
    private string? _currentUserId;
    private bool _started;

    public GlobalNotificationService(
        INotificationSettingsService settings,
        IJSRuntime js,
        CircuitCookieService cookieSvc,
        IUserService userService,
        ChatNotificationState state)
    {
        _settings = settings;
        _js = js;
        _cookieSvc = cookieSvc;
        _userService = userService;
        _state = state;
    }

    public async Task StartAsync(string userId, string hubUrl)
    {
        if (_started || string.IsNullOrEmpty(userId)) return;
        _currentUserId = userId;

        var cookieHeader = _cookieSvc.CookieHeader;

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                if (!string.IsNullOrEmpty(cookieHeader))
                    options.Headers["Cookie"] = cookieHeader;
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.Reconnected += async _ =>
        {
            if (!string.IsNullOrWhiteSpace(_currentUserId))
            {
                await _hub.InvokeAsync("Register", _currentUserId);
            }
        };

        _hub.On<MessageDto>("NotifyMessage", async (msg) =>
        {
            if (msg.SenderId == _currentUserId) return;

            var s = await _settings.LoadAsync();
            if (!ShouldNotify(s, NotificationEvent.DirectMessage)) return;

            var profile = await _userService.GetUserProfileAsync(msg.SenderId);

            var title = s.ShowSender ? profile.DisplayName ?? "Kotoba" : "Kotoba";
            var body = s.ShowPreview ? msg.Content : "You have a new message.";
            var avatarUrl = profile.AvatarUrl;
            if (avatarUrl == null || !s.ShowSender)
            {
                avatarUrl = "/favicon.png";
            }
            _state.Add(msg.ConversationId.ToString());
            await FireAsync(s, title, body, avatarUrl);
        });

        _hub.On<string>("UserOnline", async (onlineUserId) =>
        {
            if (onlineUserId == _currentUserId) return;
            var s = await _settings.LoadAsync();
            var profile = await _userService.GetUserProfileAsync(onlineUserId);
            var avatarUrl = profile.AvatarUrl;
            if (avatarUrl == null || !s.ShowSender)
            {
                avatarUrl = "/favicon.png";
            }
            if (!ShouldNotify(s, NotificationEvent.SomeoneOnline)) return;
            await FireAsync(s, "Kotoba", $"{profile.DisplayName} is now online", avatarUrl);
        });

        _hub.On<PresenceUpdateDto>("PresenceChanged", (update) =>
        {
            PresenceChanged?.Invoke(update);
        });

        _hub.On<List<PresenceUpdateDto>>("OnlineUsersSnapshot", (updates) =>
        {
            if (updates == null)
            {
                return;
            }

            foreach (var update in updates)
            {
                PresenceChanged?.Invoke(update);
            }
        });

        try
        {
            await _hub.StartAsync();
            await _hub.InvokeAsync("Register", userId);
            _started = true;
            Console.WriteLine($"[GlobalNotif] Registered user: {userId}");
            Console.WriteLine($"[GlobalNotif] Hub connected. State={_hub.State}");
        }
        catch (Exception ex)
        {
            _started = false;
            Console.Error.WriteLine($"[GlobalNotif] Hub connect failed: {ex.Message}");
        }
    }

    private async Task FireAsync(NotificationSettingsDto s, string title, string body, string avatarUrl)
    {
        try
        {
            await _js.InvokeVoidAsync("notifSettings.showNotification", title, body, avatarUrl);

            if (s.SoundEnabled)
                await _js.InvokeVoidAsync("notifSettings.playSound", s.Volume / 100.0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GlobalNotif] FireAsync: {ex.Message}");
        }
    }

    private static bool ShouldNotify(NotificationSettingsDto s, NotificationEvent evt)
    {
        if (!s.MasterEnabled) return false;

        var eventEnabled = evt switch
        {
            NotificationEvent.DirectMessage => s.DirectMessages,
            NotificationEvent.GroupMessage => s.GroupMessages,
            NotificationEvent.Mention => s.Mentions,
            NotificationEvent.SomeoneOnline => s.SomeoneOnline,
            _ => true
        };
        if (!eventEnabled) return false;

        if (!s.QuietHoursEnabled) return true;

        var now = TimeOnly.FromDateTime(DateTime.Now);
        var from = TimeOnly.Parse(s.QuietFrom);
        var to = TimeOnly.Parse(s.QuietTo);

        // Handles overnight windows e.g. 22:00 → 08:00
        var inWindow = from <= to
            ? now >= from && now <= to
            : now >= from || now <= to;

        return !inWindow;
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            try
            {
                if (_hub.State == HubConnectionState.Connected && !string.IsNullOrWhiteSpace(_currentUserId))
                {
                    await _hub.InvokeAsync("Unregister", _currentUserId);
                }
            }
            catch
            {
                // Best-effort unregister; OnDisconnectedAsync still handles hard disconnects.
            }

            await _hub.DisposeAsync();
        }
    }
}
