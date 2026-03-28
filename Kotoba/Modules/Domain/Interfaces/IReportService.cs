using Kotoba.Modules.Domain.DTOs;

namespace Kotoba.Modules.Domain.Interfaces
{
    public interface IReportService
    {
        Task<List<ReportCategoryDto>> GetCategoriesAsync();
        Task<(bool success, string error, Guid? reportId)> SubmitReportAsync(CreateReportRequest request);
        Task DeleteReportAsync(Guid reportId);
    }
}
