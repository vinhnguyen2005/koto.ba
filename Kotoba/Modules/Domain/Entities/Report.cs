using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.Entities
{
    public class Report
    {
        public Guid Id { get; set; }
        public string ReporterId { get; set; } = string.Empty;
        public ReportTargetType TargetType { get; set; }        // Message, User, Thought, Story
        public string TargetId { get; set; } = string.Empty;   // dùng string để chứa Guid hoặc UserId
        public Guid CategoryId { get; set; }
        public string? Description { get; set; }
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewerId { get; set; }

        // Navigation
        public virtual User Reporter { get; set; } = null!;
        public virtual ReportCategory Category { get; set; } = null!;
    }
}
