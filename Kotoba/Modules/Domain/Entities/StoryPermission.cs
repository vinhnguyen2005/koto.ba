namespace Kotoba.Modules.Domain.Entities
{
    public class StoryPermission
    {
        public Guid Id { get; set; }

        public Guid StoryId { get; set; }
        public string UserId { get; set; } = null!;

        public Story Story { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
