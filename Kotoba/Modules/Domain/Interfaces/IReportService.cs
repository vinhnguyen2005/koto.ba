using Kotoba.Modules.Domain.DTOs;

namespace Kotoba.Modules.Domain.Interfaces
{
    public interface IReportService
    {
        Task<List<ReportCategoryDto>> GetCategoriesAsync();
        Task<List<AdminReportListItemDto>> GetReportsForReviewAsync();
        Task<(bool success, string error)> MarkReportReviewedAsync(Guid reportId, string reviewerId);
        Task<(bool success, string error)> DismissReportAsync(Guid reportId, string reviewerId);
        Task<(bool success, string error, Guid? reportId)> SubmitReportAsync(CreateReportRequest request);
        Task DeleteReportAsync(Guid reportId);
    }
}
