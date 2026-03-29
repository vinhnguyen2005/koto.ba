using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Repositories
{
    public class StoryRepository : IStoryRepository
    {
        private readonly IDbContextFactory<KotobaDbContext> _dbFactory;
        private static readonly TimeSpan StoryLifetime = TimeSpan.FromHours(24);
        public StoryRepository(IDbContextFactory<KotobaDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }
        public async Task<Story?> AddAsync(Story story)
        {
            using (var context = await _dbFactory.CreateDbContextAsync())
            {
                context.Stories.Add(story);
                await context.SaveChangesAsync();

                return story;
            }   
        }

        public async Task<List<Story>> GetActiveAsync()
        {
            using (var context = await _dbFactory.CreateDbContextAsync())
            {
                var now = DateTime.UtcNow;

                return await context.Stories
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Include(s => s.Permissions)
                    .Where(s => s.ExpiresAt > now)
                    .OrderByDescending(s => s.CreatedAt)
                    .GroupBy(s => s.UserId)
                    .Select(g => g.First())
                    .ToListAsync();
            } 
        }

        public async Task<List<Story>> GetActiveByUserIdAsync(string userId)
        {
            using (var context = await _dbFactory.CreateDbContextAsync())
            {
                var now = DateTime.UtcNow;

                return await context.Stories
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Include(s => s.Permissions)
                    .Where(s => s.ExpiresAt > now)
                    .Where(s => s.UserId == userId)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();
            }
        }
        public async Task<List<Story>> GetActiveFollowingStoriesAsync(string userId)
        {
            using (var context = await _dbFactory.CreateDbContextAsync())
            {
                var now = DateTime.UtcNow;

                var followingIds = await context.Follows
                    .Where(f => f.FollowerId == userId)
                    .Select(f => f.FollowingId)
                    .ToListAsync();

                return await context.Stories
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Where(s => s.ExpiresAt > now &&
                        (followingIds.Contains(s.UserId) || s.UserId == userId)) 
                    .OrderByDescending(s => s.CreatedAt)
                    .GroupBy(s => s.UserId)
                    .Select(g => g.First())
                    .ToListAsync();
            }
        }
    }
}

