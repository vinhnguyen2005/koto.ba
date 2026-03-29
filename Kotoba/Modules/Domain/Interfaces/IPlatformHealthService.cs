using Kotoba.Modules.Domain.DTOs;

namespace Kotoba.Modules.Domain.Interfaces;

public interface IPlatformHealthService
{
    Task<PlatformHealthSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
