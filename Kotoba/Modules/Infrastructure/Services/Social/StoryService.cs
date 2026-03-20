using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Services.Social
{
    public class StoryService : IStoryService
    {
        private readonly KotobaDbContext _db;
        private static readonly TimeSpan StoryLifetime = TimeSpan.FromHours(24);

        public StoryService(KotobaDbContext db)
        {
            _db = db;
        }

        public async Task<StoryDto?> CreateStoryAsync(CreateStoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.Content))
                return null;

            var story = new Story
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Content = request.Content,
                MediaUrl = request.MediaUrl,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(StoryLifetime)
            };

            _db.Stories.Add(story);
            await _db.SaveChangesAsync();

            return MapToDto(story);
        }

        public async Task<List<StoryDto>> GetActiveStoriesAsync()
        {
            var now = DateTime.UtcNow;

            return await _db.Stories
                .AsNoTracking()
                .Where(s => s.ExpiresAt > now)         
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => MapToDto(s))
                .ToListAsync();
        }

        // ── helpers ──────────────────────────────────────────────────────────
        private static StoryDto MapToDto(Story s) => new()
        {
            StoryId = s.Id,
            UserId = s.UserId,
            Content = s.Content,
            MediaUrl = s.MediaUrl,
            ExpiresAt = s.ExpiresAt
        };
    }
}
