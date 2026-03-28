using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs;

public class AdminAuditViewDto
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string PerformedByAdminId { get; set; } = string.Empty;
    public string? PerformedByAdminDisplayName { get; set; }
    public string? PerformedByAdminEmail { get; set; }
    public AdminActionType ActionType { get; set; } = AdminActionType.Unknown;
    public bool IsSuccess { get; set; }
    public string? TargetEntityType { get; set; }
    public string? TargetEntityId { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? TargetEmail { get; set; }
    public string? MetadataJson { get; set; }
    public string? Summary { get; set; }
}
