using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Repositories
{
    public class ReportRepository
    {
        private readonly IDbContextFactory<KotobaDbContext> _factory;

    public ReportRepository(IDbContextFactory<KotobaDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<ReportCategoryDto>> GetActiveCategoriesAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.ReportCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ReportCategoryDto
            {
                Id          = c.Id,
                Name        = c.Name,
                Description = c.Description
            })
            .ToListAsync();
    }

    public async Task AddAsync(Report report)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.Reports.Add(report);
        await ctx.SaveChangesAsync();
    }

    public async Task<bool> AlreadyReportedAsync(
        string reporterId, ReportTargetType targetType, string targetId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Reports.AnyAsync(r =>
            r.ReporterId  == reporterId  &&
            r.TargetType  == targetType  &&
            r.TargetId    == targetId    &&
            r.Status      == ReportStatus.Pending);
    }

        public async Task DeleteAsync(Guid reportId)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            var report = await ctx.Reports.FindAsync(reportId);
            if (report != null)
            {
                ctx.Reports.Remove(report);
                await ctx.SaveChangesAsync();
            }
        }
    }
}
