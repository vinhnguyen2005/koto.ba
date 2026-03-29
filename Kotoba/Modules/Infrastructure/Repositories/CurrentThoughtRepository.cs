using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Repositories
{
    public class CurrentThoughtRepository : ICurrentThoughtRepository
    {
        private readonly IDbContextFactory<KotobaDbContext> _dbFactory;

        public CurrentThoughtRepository(IDbContextFactory<KotobaDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<CurrentThought?> GetByUserIdAsync(string userId)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            return await _db.CurrentThoughts
                .FirstOrDefaultAsync(t => t.UserId == userId);
        }

        public async Task<List<CurrentThought>> GetByUserIdsAsync(List<string> userIds)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            return await _db.CurrentThoughts
                .Where(t => userIds.Contains(t.UserId))
                .ToListAsync();
        }

        public async Task<CurrentThought?> GetByIdAsync(Guid id)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            return await _db.CurrentThoughts
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(CurrentThought thought)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            await _db.CurrentThoughts.AddAsync(thought);
            await _db.SaveChangesAsync(); 
        }

        public async Task UpdateAsync(CurrentThought thought)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            _db.CurrentThoughts.Update(thought);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            using (var context = await _dbFactory.CreateDbContextAsync())
            {
                var thought = await context.CurrentThoughts.FindAsync(id);
                if (thought == null)
                    return false;

                context.CurrentThoughts.Remove(thought);
                await context.SaveChangesAsync();
                return true;
            }
        }

        public async Task SaveChangesAsync()
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            await _db.SaveChangesAsync();
        }

        public async Task<List<CurrentThought>> GetActiveAsync(string? currentUserId = null)
        {
            using (var context = await _dbFactory.CreateDbContextAsync())
            {
                IQueryable<CurrentThought> query = context.CurrentThoughts
                    .AsNoTracking()
                    .Include(t => t.User);

                if (string.IsNullOrEmpty(currentUserId))
                {
                    query = query.Where(t => t.Privacy == ThoughtPrivacy.Public);
                }
                else
                {
                    var followingIds = await context.Follows
                        .Where(f => f.FollowerId == currentUserId)
                        .Select(f => f.FollowingId)
                        .ToListAsync();

                    query = query.Where(t =>
                        t.Privacy == ThoughtPrivacy.Public ||
                        (t.Privacy == ThoughtPrivacy.FollowersOnly && followingIds.Contains(t.UserId)) ||
                        t.UserId == currentUserId
                    );
                }

                return await query
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
        }

        public async Task<List<CurrentThought>> GetActiveFollowingThoughtsAsync(string userId)
        {
            using (var context = await _dbFactory.CreateDbContextAsync())
            {
                var followingIds = await context.Follows
                    .Where(f => f.FollowerId == userId)
                    .Select(f => f.FollowingId)
                    .ToListAsync();

                followingIds.Add(userId);

                return await context.CurrentThoughts
                    .AsNoTracking()
                    .Include(t => t.User)
                    .Where(t => followingIds.Contains(t.UserId))
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
        }
    }
}
