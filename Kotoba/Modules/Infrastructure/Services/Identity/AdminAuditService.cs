using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Services.Identity;

public class AdminAuditService : IAdminAuditService
{
    private readonly KotobaDbContext _dbContext;

    public AdminAuditService(KotobaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task TraceAsync(AdminAuditEntryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PerformedByAdminId))
        {
            return;
        }

        var audit = new AdminAuditLog
        {
            TimestampUtc = request.TimestampUtc ?? DateTime.UtcNow,
            PerformedByAdminId = request.PerformedByAdminId.Trim(),
            ActionType = request.ActionType,
            IsSuccess = request.IsSuccess,
            TargetEntityType = NormalizeValue(request.TargetEntityType, 80),
            TargetEntityId = NormalizeValue(request.TargetEntityId, 128),
            Summary = NormalizeValue(request.Summary, 500),
            MetadataJson = NormalizeValue(request.MetadataJson, 4000),
            CorrelationId = NormalizeValue(request.CorrelationId, 100),
            SourceIp = NormalizeValue(request.SourceIp, 64),
        };

        _dbContext.AdminAuditLogs.Add(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminAuditViewDto>> GetRecentAuditsAsync(
        string? performedByAdminId = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = limit <= 0 ? 20 : Math.Min(limit, 200);
        var query = _dbContext.AdminAuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(performedByAdminId))
        {
            query = query.Where(a => a.PerformedByAdminId == performedByAdminId);
        }

        var audits = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Take(safeLimit)
            .Select(a => new AdminAuditViewDto
            {
                Id = a.Id,
                TimestampUtc = a.TimestampUtc,
                PerformedByAdminId = a.PerformedByAdminId,
                PerformedByAdminDisplayName = a.PerformedByAdmin != null ? a.PerformedByAdmin.DisplayName : null,
                PerformedByAdminEmail = a.PerformedByAdmin != null ? a.PerformedByAdmin.Email : null,
                ActionType = a.ActionType,
                IsSuccess = a.IsSuccess,
                TargetEntityType = a.TargetEntityType,
                TargetEntityId = a.TargetEntityId,
                MetadataJson = a.MetadataJson,
                Summary = a.Summary,
            })
            .ToListAsync(cancellationToken);

        var targetUserIds = audits
            .Where(audit => string.Equals(audit.TargetEntityType, "User", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(audit.TargetEntityId))
            .Select(audit => audit.TargetEntityId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (targetUserIds.Count == 0)
        {
            return audits;
        }

        var targetUsers = await _dbContext.Users
            .AsNoTracking()
            .Where(user => targetUserIds.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                user.DisplayName,
                user.Email,
            })
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        foreach (var audit in audits)
        {
            if (!string.Equals(audit.TargetEntityType, "User", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(audit.TargetEntityId))
            {
                continue;
            }

            if (!targetUsers.TryGetValue(audit.TargetEntityId, out var targetUser))
            {
                continue;
            }

            audit.TargetDisplayName = targetUser.DisplayName;
            audit.TargetEmail = targetUser.Email;
        }

        return audits;
    }

    public async Task<int> CountAuditsSinceAsync(
        DateTime sinceUtc,
        string? performedByAdminId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AdminAuditLogs
            .AsNoTracking()
            .Where(a => a.TimestampUtc >= sinceUtc);

        if (!string.IsNullOrWhiteSpace(performedByAdminId))
        {
            query = query.Where(a => a.PerformedByAdminId == performedByAdminId);
        }

        return await query.CountAsync(cancellationToken);
    }

    private static string? NormalizeValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
