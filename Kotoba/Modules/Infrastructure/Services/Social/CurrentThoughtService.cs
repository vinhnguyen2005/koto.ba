using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Services.Social
{
    public class CurrentThoughtService : ICurrentThoughtService
    {
        private readonly KotobaDbContext _db;
        private static readonly TimeSpan ThoughtLifetime = TimeSpan.FromHours(24);

        public CurrentThoughtService(KotobaDbContext db)
        {
            _db = db;
        }
  
        public async Task<bool> SetThoughtAsync(string userId, string content)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(content))
                return false;

            var existing = await _db.CurrentThoughts
                .FirstOrDefaultAsync(ct => ct.UserId == userId);

            if (existing is null)
            {
                _db.CurrentThoughts.Add(new CurrentThought
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Content = content,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(ThoughtLifetime)
                });
            }
            else
            {
                existing.Content = content;
                existing.CreatedAt = DateTime.UtcNow;
                existing.ExpiresAt = DateTime.UtcNow.Add(ThoughtLifetime);
            }

            await _db.SaveChangesAsync();
            return true;
        }


        public async Task<string?> GetThoughtAsync(string userId)
        {
            var now = DateTime.UtcNow;

            var thought = await _db.CurrentThoughts
                .AsNoTracking()
                .FirstOrDefaultAsync(ct => ct.UserId == userId && ct.ExpiresAt > now);

            return thought?.Content;
        }
    }
}
