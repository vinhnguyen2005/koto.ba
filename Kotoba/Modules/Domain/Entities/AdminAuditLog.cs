using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.Entities;

public class AdminAuditLog
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string PerformedByAdminId { get; set; } = string.Empty;
    public AdminActionType ActionType { get; set; } = AdminActionType.Unknown;
    public bool IsSuccess { get; set; } = true;
    public string? TargetEntityType { get; set; }
    public string? TargetEntityId { get; set; }
    public string? Summary { get; set; }
    public string? MetadataJson { get; set; }
    public string? CorrelationId { get; set; }
    public string? SourceIp { get; set; }

    public virtual User? PerformedByAdmin { get; set; }
}
