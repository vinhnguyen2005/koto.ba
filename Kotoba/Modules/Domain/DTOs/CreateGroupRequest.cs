namespace Kotoba.Modules.Domain.DTOs;

public class CreateGroupRequest
{
    public string GroupName { get; set; } = string.Empty;
    public List<string> ParticipantIds { get; set; } = new();
    public string CreatorId { get; set; } = string.Empty;
    public List<string>? AdminIds { get; set; }
}
