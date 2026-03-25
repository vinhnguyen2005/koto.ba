using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Repositories
{
    public class CurrentThoughtRepository : ICurrentThoughtRepository
    {
        private readonly KotobaDbContext _db;

        public CurrentThoughtRepository(KotobaDbContext db)
        {
            _db = db;
        }

        public async Task<CurrentThought?> GetByUserIdAsync(string userId)
        {
            return await _db.CurrentThoughts
                .FirstOrDefaultAsync(t => t.UserId == userId);
        }

        public async Task<List<CurrentThought>> GetByUserIdsAsync(List<string> userIds)
        {
            return await _db.CurrentThoughts
                .Where(t => userIds.Contains(t.UserId))
                .ToListAsync();
        }
        public async Task AddAsync(CurrentThought thought)
        {
            await _db.CurrentThoughts.AddAsync(thought);
        }

        public Task UpdateAsync(CurrentThought thought)
        {
            _db.CurrentThoughts.Update(thought);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CurrentThought thought)
        {
            _db.CurrentThoughts.Remove(thought);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
