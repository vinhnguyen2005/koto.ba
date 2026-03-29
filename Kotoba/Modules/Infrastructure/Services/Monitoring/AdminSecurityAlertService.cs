using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Services.Monitoring;

public sealed class AdminSecurityAlertService : IAdminSecurityAlertService
{
    private static readonly AdminActionType[] RoleChangeActions =
    {
        AdminActionType.AdminCreated,
        AdminActionType.AdminDisabled,
        AdminActionType.SecurityPolicyChanged,
    };

    private static readonly AdminActionType[] ModerationActions =
    {
        AdminActionType.UserDeactivated,
        AdminActionType.UserReactivated,
        AdminActionType.UserBanned,
        AdminActionType.UserUnbanned,
    };

    private readonly IDbContextFactory<KotobaDbContext> _dbContextFactory;
    private readonly AdminSecurityAlertAckStore _ackStore;

    public AdminSecurityAlertService(
        IDbContextFactory<KotobaDbContext> dbContextFactory,
        AdminSecurityAlertAckStore ackStore)
    {
        _dbContextFactory = dbContextFactory;
        _ackStore = ackStore;
    }

    public async Task<IReadOnlyList<AdminSecurityAlertDto>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var lookbackUtc = nowUtc.AddHours(-24);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var logs = await dbContext.AdminAuditLogs
            .AsNoTracking()
            .Where(log => log.TimestampUtc >= lookbackUtc)
            .Select(log => new AlertLogRow
            {
                Id = log.Id,
                TimestampUtc = log.TimestampUtc,
                PerformedByAdminId = log.PerformedByAdminId,
                ActionType = log.ActionType,
                IsSuccess = log.IsSuccess,
                TargetEntityId = log.TargetEntityId,
                SourceIp = log.SourceIp,
                Summary = log.Summary,
            })
            .ToListAsync(cancellationToken);

        var alerts = new List<AdminSecurityAlertDto>();

        alerts.AddRange(BuildFailedLoginAlerts(logs, nowUtc));
        alerts.AddRange(BuildSuspiciousIpAlerts(logs, nowUtc));
        alerts.AddRange(BuildRoleChangeAttemptAlerts(logs, nowUtc));
        alerts.AddRange(BuildMassModerationAlerts(logs, nowUtc));

        foreach (var alert in alerts)
        {
            var ack = _ackStore.Get(alert.AlertKey);
            if (ack is null)
            {
                continue;
            }

            alert.IsAcknowledged = true;
            alert.AcknowledgedByAdminId = ack.AcknowledgedByAdminId;
            alert.AcknowledgedAtUtc = ack.AcknowledgedAtUtc;
        }

        return alerts
            .OrderByDescending(alert => alert.Severity)
            .ThenByDescending(alert => alert.OccurredAtUtc)
            .ToList();
    }

    public Task<bool> AcknowledgeAsync(string alertKey, string acknowledgedByAdminId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var success = _ackStore.MarkAcknowledged(alertKey, acknowledgedByAdminId, DateTime.UtcNow);
        return Task.FromResult(success);
    }

    private static IEnumerable<AdminSecurityAlertDto> BuildFailedLoginAlerts(List<AlertLogRow> logs, DateTime nowUtc)
    {
        var recentFailures = logs
            .Where(log => log.ActionType == AdminActionType.AdminLoginFailed
                && log.TimestampUtc >= nowUtc.AddMinutes(-30))
            .ToList();

        var groups = recentFailures
            .GroupBy(log => new
            {
                log.PerformedByAdminId,
                SourceIp = string.IsNullOrWhiteSpace(log.SourceIp) ? "unknown-ip" : log.SourceIp,
            })
            .Where(group => group.Count() >= 3);

        foreach (var group in groups)
        {
            var count = group.Count();
            var latest = group.MaxBy(log => log.TimestampUtc)!;
            var severity = count >= 8
                ? AdminSecurityAlertSeverity.Critical
                : AdminSecurityAlertSeverity.High;
            var search = Uri.EscapeDataString($"{group.Key.PerformedByAdminId} {group.Key.SourceIp}");

            yield return new AdminSecurityAlertDto
            {
                AlertKey = $"failed-login:{group.Key.PerformedByAdminId}:{group.Key.SourceIp}",
                Type = AdminSecurityAlertType.FailedAdminLoginBurst,
                Severity = severity,
                Title = "Repeated failed admin logins",
                Message = $"{count} failed admin login attempts in 30 minutes for admin {group.Key.PerformedByAdminId} from IP {group.Key.SourceIp}.",
                OccurredAtUtc = latest.TimestampUtc,
                EvidenceCount = count,
                RelatedUserId = group.Key.PerformedByAdminId,
                RelatedIp = group.Key.SourceIp,
                AuditTrailUrl = $"/admin/system/audits?q={search}&range=24h",
                UserUrl = $"/admin/business/users?q={Uri.EscapeDataString(group.Key.PerformedByAdminId)}",
            };
        }
    }

    private static IEnumerable<AdminSecurityAlertDto> BuildSuspiciousIpAlerts(List<AlertLogRow> logs, DateTime nowUtc)
    {
        var recentFailures = logs
            .Where(log => log.ActionType == AdminActionType.AdminLoginFailed
                && log.TimestampUtc >= nowUtc.AddMinutes(-15)
                && !string.IsNullOrWhiteSpace(log.SourceIp));

        var groups = recentFailures
            .GroupBy(log => log.SourceIp!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                SourceIp = group.Key,
                Attempts = group.Count(),
                DistinctAdmins = group.Select(log => log.PerformedByAdminId).Distinct(StringComparer.Ordinal).Count(),
                Latest = group.MaxBy(log => log.TimestampUtc),
            })
            .Where(group => group.Attempts >= 5 && group.DistinctAdmins >= 3);

        foreach (var group in groups)
        {
            var search = Uri.EscapeDataString(group.SourceIp);
            yield return new AdminSecurityAlertDto
            {
                AlertKey = $"ip-velocity:{group.SourceIp}",
                Type = AdminSecurityAlertType.SuspiciousIpVelocity,
                Severity = group.Attempts >= 10 ? AdminSecurityAlertSeverity.Critical : AdminSecurityAlertSeverity.High,
                Title = "Suspicious IP velocity",
                Message = $"IP {group.SourceIp} triggered {group.Attempts} failed admin logins across {group.DistinctAdmins} admin accounts in 15 minutes.",
                OccurredAtUtc = group.Latest?.TimestampUtc ?? nowUtc,
                EvidenceCount = group.Attempts,
                RelatedIp = group.SourceIp,
                AuditTrailUrl = $"/admin/system/audits?q={search}&range=24h",
            };
        }
    }

    private static IEnumerable<AdminSecurityAlertDto> BuildRoleChangeAttemptAlerts(List<AlertLogRow> logs, DateTime nowUtc)
    {
        var recentRoleChangeFailures = logs
            .Where(log => RoleChangeActions.Contains(log.ActionType)
                && !log.IsSuccess
                && log.TimestampUtc >= nowUtc.AddHours(-24))
            .ToList();

        if (recentRoleChangeFailures.Count == 0)
        {
            yield break;
        }

        var latest = recentRoleChangeFailures.MaxBy(log => log.TimestampUtc)!;
        var count = recentRoleChangeFailures.Count;
        var search = Uri.EscapeDataString("AdminCreated AdminDisabled SecurityPolicyChanged");

        yield return new AdminSecurityAlertDto
        {
            AlertKey = "role-change-attempts:24h",
            Type = AdminSecurityAlertType.RoleChangeAttempt,
            Severity = count >= 3 ? AdminSecurityAlertSeverity.High : AdminSecurityAlertSeverity.Medium,
            Title = "Role change attempts failed",
            Message = $"{count} failed role or admin-policy change attempts in the last 24 hours.",
            OccurredAtUtc = latest.TimestampUtc,
            EvidenceCount = count,
            RelatedUserId = latest.TargetEntityId,
            AuditTrailUrl = $"/admin/system/audits?q={search}&range=24h",
            UserUrl = string.IsNullOrWhiteSpace(latest.TargetEntityId)
                ? null
                : $"/admin/business/users?q={Uri.EscapeDataString(latest.TargetEntityId)}",
        };
    }

    private static IEnumerable<AdminSecurityAlertDto> BuildMassModerationAlerts(List<AlertLogRow> logs, DateTime nowUtc)
    {
        var recentModeration = logs
            .Where(log => ModerationActions.Contains(log.ActionType)
                && log.IsSuccess
                && log.TimestampUtc >= nowUtc.AddMinutes(-15));

        var groups = recentModeration
            .GroupBy(log => log.PerformedByAdminId)
            .Where(group => group.Count() >= 5);

        foreach (var group in groups)
        {
            var count = group.Count();
            var latest = group.MaxBy(log => log.TimestampUtc)!;
            var sampleTarget = group.Select(log => log.TargetEntityId).FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
            var search = Uri.EscapeDataString(group.Key);

            yield return new AdminSecurityAlertDto
            {
                AlertKey = $"mass-moderation:{group.Key}",
                Type = AdminSecurityAlertType.MassModerationAction,
                Severity = count >= 10 ? AdminSecurityAlertSeverity.Critical : AdminSecurityAlertSeverity.High,
                Title = "Mass moderation activity",
                Message = $"Admin {group.Key} executed {count} moderation actions in 15 minutes.",
                OccurredAtUtc = latest.TimestampUtc,
                EvidenceCount = count,
                RelatedUserId = sampleTarget,
                AuditTrailUrl = $"/admin/system/audits?q={search}&range=24h",
                UserUrl = string.IsNullOrWhiteSpace(sampleTarget)
                    ? null
                    : $"/admin/business/users?q={Uri.EscapeDataString(sampleTarget)}",
            };
        }
    }

    private sealed class AlertLogRow
    {
        public int Id { get; init; }
        public DateTime TimestampUtc { get; init; }
        public string PerformedByAdminId { get; init; } = string.Empty;
        public AdminActionType ActionType { get; init; }
        public bool IsSuccess { get; init; }
        public string? TargetEntityId { get; init; }
        public string? SourceIp { get; init; }
        public string? Summary { get; init; }
    }
}
