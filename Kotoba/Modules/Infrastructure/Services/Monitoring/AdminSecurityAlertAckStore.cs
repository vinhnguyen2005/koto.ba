using System.Collections.Concurrent;

namespace Kotoba.Modules.Infrastructure.Services.Monitoring;

public sealed class AdminSecurityAlertAckStore
{
    private readonly ConcurrentDictionary<string, AlertAckInfo> _ackByKey = new(StringComparer.Ordinal);

    public bool MarkAcknowledged(string alertKey, string adminId, DateTime acknowledgedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(alertKey) || string.IsNullOrWhiteSpace(adminId))
        {
            return false;
        }

        _ackByKey[alertKey.Trim()] = new AlertAckInfo
        {
            AlertKey = alertKey.Trim(),
            AcknowledgedByAdminId = adminId.Trim(),
            AcknowledgedAtUtc = acknowledgedAtUtc,
        };

        return true;
    }

    public AlertAckInfo? Get(string alertKey)
    {
        if (string.IsNullOrWhiteSpace(alertKey))
        {
            return null;
        }

        return _ackByKey.TryGetValue(alertKey.Trim(), out var info)
            ? info
            : null;
    }

    public sealed class AlertAckInfo
    {
        public string AlertKey { get; init; } = string.Empty;
        public string AcknowledgedByAdminId { get; init; } = string.Empty;
        public DateTime AcknowledgedAtUtc { get; init; }
    }
}
