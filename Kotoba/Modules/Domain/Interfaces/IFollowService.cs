namespace Kotoba.Modules.Domain.Interfaces
{
    public interface IFollowService
    {
        Task FollowAsync(string followerId, string followingId);
        Task UnfollowAsync(string followerId, string followingId);
        Task<bool> IsFollowingAsync(string followerId, string followingId);
        Task<int> GetFollowersCount(string userId);
        Task<List<string>> GetFollowingIdsAsync(string userId);
    }
}
