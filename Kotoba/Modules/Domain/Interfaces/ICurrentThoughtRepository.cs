using Kotoba.Modules.Domain.Entities;

public interface ICurrentThoughtRepository
{
    Task<CurrentThought?> GetByUserIdAsync(string userId);
    Task<List<CurrentThought>> GetByUserIdsAsync(List<string> userIds);
    Task AddAsync(CurrentThought thought);
    Task UpdateAsync(CurrentThought thought);
    Task DeleteAsync(CurrentThought thought);
    Task SaveChangesAsync();
}
