using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Repositories;

namespace Kotoba.Modules.Infrastructure.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly ReportRepository _repo;
        private readonly IAdminAuditService _adminAuditService;

        public ReportService(ReportRepository repo, IAdminAuditService adminAuditService)
        {
            _repo = repo;
            _adminAuditService = adminAuditService;
        }

        public Task<List<ReportCategoryDto>> GetCategoriesAsync()
            => _repo.GetActiveCategoriesAsync();

        public Task<List<AdminReportListItemDto>> GetReportsForReviewAsync()
            => _repo.GetReportsForReviewAsync();

        public async Task<(bool success, string error)> MarkReportReviewedAsync(Guid reportId, string reviewerId)
        {
            var result = await _repo.UpdateStatusAsync(reportId, Domain.Enums.ReportStatus.Reviewed, reviewerId);
            await TraceReportModerationAsync(
                reportId,
                reviewerId,
                "reviewed",
                result.success,
                result.error);

            return result;
        }

        public async Task<(bool success, string error)> DismissReportAsync(Guid reportId, string reviewerId)
        {
            var result = await _repo.UpdateStatusAsync(reportId, Domain.Enums.ReportStatus.Dismissed, reviewerId);
            await TraceReportModerationAsync(
                reportId,
                reviewerId,
                "dismissed",
                result.success,
                result.error);

            return result;
        }

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
                ReportedUserId = string.IsNullOrWhiteSpace(request.ReportedUserId) ? null : request.ReportedUserId,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                CategoryId = request.CategoryId,
                Description = request.Description?.Trim(),
                ReportedContent = string.IsNullOrWhiteSpace(request.ReportedContent) ? null : request.ReportedContent.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(report);
            return (true, string.Empty, report.Id);
        }

        public Task DeleteReportAsync(Guid reportId)
            => _repo.DeleteAsync(reportId);

        private Task TraceReportModerationAsync(
            Guid reportId,
            string reviewerId,
            string actionLabel,
            bool isSuccess,
            string errorMessage)
        {
            return _adminAuditService.TraceAsync(new AdminAuditEntryRequest
            {
                PerformedByAdminId = reviewerId,
                ActionType = Domain.Enums.AdminActionType.UserReportResolved,
                IsSuccess = isSuccess,
                TargetEntityType = "Report",
                TargetEntityId = reportId.ToString(),
                Summary = isSuccess
                    ? $"Report {actionLabel}."
                    : $"Failed to mark report as {actionLabel}: {errorMessage}",
            });
        }
    }
}
