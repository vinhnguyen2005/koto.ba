using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.DTOs;

public class UpdateMessageStatusRequest
{
    public Guid MessageId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public MessageStatus Status { get; set; }
}
