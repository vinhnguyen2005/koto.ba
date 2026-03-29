using Kotoba.Modules.Domain.DTOs;

namespace Kotoba.Modules.Domain.Interfaces;

public interface IAdminAuditService
{
    Task TraceAsync(AdminAuditEntryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminAuditViewDto>> GetAuditsAsync(
        string? performedByAdminId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminAuditViewDto>> GetRecentAuditsAsync(
        string? performedByAdminId = null,
        int limit = 20,
        CancellationToken cancellationToken = default);
    Task<int> CountAuditsSinceAsync(
        DateTime sinceUtc,
        string? performedByAdminId = null,
        CancellationToken cancellationToken = default);
}
