using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Repositories;

namespace Kotoba.Modules.Infrastructure.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly ReportRepository _repo;

        public ReportService(ReportRepository repo)
        {
            _repo = repo;
        }

        public Task<List<ReportCategoryDto>> GetCategoriesAsync()
            => _repo.GetActiveCategoriesAsync();

        public async Task<(bool success, string error, Guid? reportId)> SubmitReportAsync(
    CreateReportRequest request)
        {
            var duplicate = await _repo.AlreadyReportedAsync(
                request.ReporterId, request.TargetType, request.TargetId);

            if (duplicate)
                return (false, "You have already reported this.", null);

            var report = new Report
            {
                Id = Guid.NewGuid(),
                ReporterId = request.ReporterId,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                CategoryId = request.CategoryId,
                Description = request.Description?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(report);
            return (true, string.Empty, report.Id);
        }

        public Task DeleteReportAsync(Guid reportId)
            => _repo.DeleteAsync(reportId);
    }
}
