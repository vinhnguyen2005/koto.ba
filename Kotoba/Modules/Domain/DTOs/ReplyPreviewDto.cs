namespace Kotoba.Modules.Domain.DTOs
{
    public class ReplyPreviewDto
    {
        public Guid MessageId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string? Content { get; set; }         // null nếu là attachment
        public string? AttachmentType { get; set; }  // "image" | "file" | null
        public bool IsRevoked { get; set; }
    }
}
