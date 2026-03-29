using Kotoba.Modules.Domain.Entities;

public interface ICurrentThoughtRepository
{
    Task<CurrentThought?> GetByUserIdAsync(string userId);
    Task<List<CurrentThought>> GetByUserIdsAsync(List<string> userIds);
    Task<CurrentThought?> GetByIdAsync(Guid id);
    Task AddAsync(CurrentThought thought);
    Task UpdateAsync(CurrentThought thought);
    Task<bool> DeleteAsync(Guid id);
    Task SaveChangesAsync();
    Task<List<CurrentThought>> GetActiveAsync(string? currentUserId = null);
    Task<List<CurrentThought>> GetActiveFollowingThoughtsAsync(string userId);
}
