using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Kotoba.Modules.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Kotoba.Modules.Infrastructure.Services.Monitoring;

public sealed class PlatformHealthService : IPlatformHealthService
{
    private readonly IDbContextFactory<KotobaDbContext> _dbContextFactory;
    private readonly IServiceProvider _serviceProvider;

    public PlatformHealthService(
        IDbContextFactory<KotobaDbContext> dbContextFactory,
        IServiceProvider serviceProvider)
    {
        _dbContextFactory = dbContextFactory;
        _serviceProvider = serviceProvider;
    }

    public async Task<PlatformHealthSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var checkedAtUtc = DateTime.UtcNow;
        var checks = new List<PlatformHealthCheckItemDto>
        {
            await CheckDatabaseAsync(cancellationToken),
            CheckSignalR(),
            CheckBackgroundJobs(),
        };

        return new PlatformHealthSnapshotDto
        {
            CheckedAtUtc = checkedAtUtc,
            IsHealthy = checks.All(check => check.IsHealthy),
            Checks = checks,
        };
    }

    private async Task<PlatformHealthCheckItemDto> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();

            return new PlatformHealthCheckItemDto
            {
                Name = "Database",
                IsHealthy = canConnect,
                Detail = canConnect
                    ? $"Connected in {stopwatch.ElapsedMilliseconds} ms"
                    : "Connection check failed",
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new PlatformHealthCheckItemDto
            {
                Name = "Database",
                IsHealthy = false,
                Detail = $"Error: {ex.Message}",
            };
        }
    }

    private PlatformHealthCheckItemDto CheckSignalR()
    {
        try
        {
            _ = _serviceProvider.GetRequiredService<IHubContext<ChatHub>>();
            _ = _serviceProvider.GetRequiredService<IHubContext<NotificationHub>>();

            return new PlatformHealthCheckItemDto
            {
                Name = "SignalR",
                IsHealthy = true,
                Detail = "Chat and notification hubs are registered",
            };
        }
        catch (Exception ex)
        {
            return new PlatformHealthCheckItemDto
            {
                Name = "SignalR",
                IsHealthy = false,
                Detail = $"Hub registration error: {ex.Message}",
            };
        }
    }

    private PlatformHealthCheckItemDto CheckBackgroundJobs()
    {
        var hostedServices = _serviceProvider
            .GetServices<IHostedService>()
            .Where(service => !service.GetType().Namespace?.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) == true)
            .Select(service => service.GetType().Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name)
            .ToList();

        if (hostedServices.Count == 0)
        {
            return new PlatformHealthCheckItemDto
            {
                Name = "Background Jobs",
                IsHealthy = true,
                Detail = "No custom background jobs registered",
            };
        }

        return new PlatformHealthCheckItemDto
        {
            Name = "Background Jobs",
            IsHealthy = true,
            Detail = $"{hostedServices.Count} registered: {string.Join(", ", hostedServices)}",
        };
    }
}
