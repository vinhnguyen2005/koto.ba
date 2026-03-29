using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs;

public sealed class AdminSecurityAlertDto
{
    public string AlertKey { get; init; } = string.Empty;
    public AdminSecurityAlertType Type { get; init; }
    public AdminSecurityAlertSeverity Severity { get; init; } = AdminSecurityAlertSeverity.Low;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
    public int EvidenceCount { get; init; }
    public string? RelatedUserId { get; init; }
    public string? RelatedIp { get; init; }
    public string AuditTrailUrl { get; init; } = "/admin/system/audits";
    public string? UserUrl { get; init; }
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedByAdminId { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
}
