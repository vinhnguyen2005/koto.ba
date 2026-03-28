using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs
{
    public class CreateReportRequest
    {
        public string ReporterId { get; set; } = string.Empty;
        public ReportTargetType TargetType { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string? Description { get; set; }
    }
}
