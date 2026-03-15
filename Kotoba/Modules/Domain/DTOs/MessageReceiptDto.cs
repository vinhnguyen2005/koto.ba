using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs;

public class MessageReceiptDto
{
    public string UserId { get; set; } = string.Empty;
    public MessageStatus Status { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
