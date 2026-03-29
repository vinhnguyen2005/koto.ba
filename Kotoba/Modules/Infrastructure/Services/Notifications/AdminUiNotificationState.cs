using System.Collections.Concurrent;

namespace Kotoba.Modules.Infrastructure.Services.Notifications;

public enum AdminNotificationAudience
{
    System,
    Business,
}

public sealed class AdminUiNotificationItem
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public bool IsRead { get; set; }
    public string ActionLabel { get; init; } = string.Empty;
    public string ActionUrl { get; init; } = string.Empty;
}

public sealed class AdminUiNotificationState
{
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<AdminNotificationAudience, List<AdminUiNotificationItem>> _store = new();

    public event Action? OnChange;

    public IReadOnlyList<AdminUiNotificationItem> GetNotifications(AdminNotificationAudience audience)
    {
        lock (_lock)
        {
            EnsureSeeded(audience);
            return _store[audience]
                .OrderByDescending(item => item.CreatedAtUtc)
                .Select(Clone)
                .ToList();
        }
    }

    public int GetUnreadCount(AdminNotificationAudience audience)
    {
        lock (_lock)
        {
            EnsureSeeded(audience);
            return _store[audience].Count(item => !item.IsRead);
        }
    }

    public void MarkAsRead(AdminNotificationAudience audience, Guid id)
    {
        lock (_lock)
        {
            EnsureSeeded(audience);
            var existing = _store[audience].FirstOrDefault(item => item.Id == id);
            if (existing is null || existing.IsRead)
            {
                return;
            }

            existing.IsRead = true;
        }

        OnChange?.Invoke();
    }

    public void MarkAllAsRead(AdminNotificationAudience audience)
    {
        lock (_lock)
        {
            EnsureSeeded(audience);
            foreach (var item in _store[audience])
            {
                item.IsRead = true;
            }
        }

        OnChange?.Invoke();
    }

    private void EnsureSeeded(AdminNotificationAudience audience)
    {
        if (_store.ContainsKey(audience))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var seeded = audience == AdminNotificationAudience.System
            ? new List<AdminUiNotificationItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Critical login anomaly",
                    Message = "Multiple failed root-login attempts from new IP range.",
                    Category = "Security",
                    CreatedAtUtc = now.AddMinutes(-18),
                    IsRead = false,
                    ActionLabel = "Open security alerts",
                    ActionUrl = "/admin/system/security-alerts"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Audit export ready",
                    Message = "Daily compliance export was generated and stored.",
                    Category = "Audit",
                    CreatedAtUtc = now.AddHours(-2),
                    IsRead = false,
                    ActionLabel = "Open audits",
                    ActionUrl = "/admin/system/audits"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Platform check recovered",
                    Message = "Storage latency returned to normal levels.",
                    Category = "Platform",
                    CreatedAtUtc = now.AddDays(-1).AddHours(-2),
                    IsRead = true,
                    ActionLabel = "Open dashboard",
                    ActionUrl = "/admin/system/dashboard"
                },
            }
            : new List<AdminUiNotificationItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "High-priority report",
                    Message = "A new violence report requires immediate moderation.",
                    Category = "Moderation",
                    CreatedAtUtc = now.AddMinutes(-11),
                    IsRead = false,
                    ActionLabel = "Open reports",
                    ActionUrl = "/admin/business/reports"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "User appeal submitted",
                    Message = "A deactivated user requested account re-evaluation.",
                    Category = "Users",
                    CreatedAtUtc = now.AddHours(-3),
                    IsRead = false,
                    ActionLabel = "Open users",
                    ActionUrl = "/admin/business/users"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Queue load normalized",
                    Message = "Average moderation queue wait time is below SLA.",
                    Category = "Operations",
                    CreatedAtUtc = now.AddDays(-2),
                    IsRead = true,
                    ActionLabel = "Open dashboard",
                    ActionUrl = "/admin/business/dashboard"
                },
            };

        _store[audience] = seeded;
    }

    private static AdminUiNotificationItem Clone(AdminUiNotificationItem source)
    {
        return new AdminUiNotificationItem
        {
            Id = source.Id,
            Title = source.Title,
            Message = source.Message,
            Category = source.Category,
            CreatedAtUtc = source.CreatedAtUtc,
            IsRead = source.IsRead,
            ActionLabel = source.ActionLabel,
            ActionUrl = source.ActionUrl,
        };
    }
}
