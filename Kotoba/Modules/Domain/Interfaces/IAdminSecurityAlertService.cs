using Kotoba.Modules.Domain.DTOs;

namespace Kotoba.Modules.Domain.Interfaces;

public interface IAdminSecurityAlertService
{
    Task<IReadOnlyList<AdminSecurityAlertDto>> GetAlertsAsync(CancellationToken cancellationToken = default);
    Task<bool> AcknowledgeAsync(string alertKey, string acknowledgedByAdminId, CancellationToken cancellationToken = default);
}
