namespace Kotoba.Modules.Domain.Entities
{
    public class Follow
    {
        public Guid Id { get; set; }
        public string FollowerId { get; set; } = string.Empty;   
        public string FollowingId { get; set; } = string.Empty;   
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User Follower { get; set; } = null!;
        public virtual User Following { get; set; } = null!;
    }
}

