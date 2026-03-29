using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs
{
    public class AdminReportListItemDto
    {
        public Guid ReportId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ReporterId { get; set; } = string.Empty;
        public string ReporterDisplayName { get; set; } = string.Empty;
        public string ReporterEmail { get; set; } = string.Empty;
        public ReportTargetType TargetType { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public string TargetPreview { get; set; } = string.Empty;
        public bool TargetExists { get; set; }
        public DateTime? TargetCreatedAtUtc { get; set; }
        public string? TargetConversationLabel { get; set; }
        public string? TargetPreviousPreview { get; set; }
        public string? TargetPreviousPreviewSecondary { get; set; }
        public string? TargetNextPreview { get; set; }
        public string? TargetNextPreviewSecondary { get; set; }
        public string? TargetUserId { get; set; }
        public string? TargetUserDisplayName { get; set; }
        public string? TargetUserEmail { get; set; }
        public AccountStatus? TargetUserAccountStatus { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ReportStatus Status { get; set; }
    }
}
