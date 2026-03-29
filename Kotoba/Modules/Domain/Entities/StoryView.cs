namespace Kotoba.Modules.Domain.Entities
{
    public class StoryView
    {
        public Guid Id { get; set; }

        public Guid StoryId { get; set; }
        public string ViewerId { get; set; } = null!;
        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
        public bool NotificationSent { get; set; } = false;
        public Story Story { get; set; } = null!;
        public User Viewer { get; set; } = null!;
    }
}
