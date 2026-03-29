namespace Kotoba.Modules.Domain.DTOs;

public sealed class PlatformHealthSnapshotDto
{
    public bool IsHealthy { get; init; }
    public DateTime CheckedAtUtc { get; init; }
    public List<PlatformHealthCheckItemDto> Checks { get; init; } = new();
}

public sealed class PlatformHealthCheckItemDto
{
    public string Name { get; init; } = string.Empty;
    public bool IsHealthy { get; init; }
    public string Detail { get; init; } = string.Empty;
}
