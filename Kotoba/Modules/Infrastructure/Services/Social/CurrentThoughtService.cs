using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Kotoba.Modules.Infrastructure.Services.Social
{
    public class CurrentThoughtService : ICurrentThoughtService
    {
        private readonly ICurrentThoughtRepository _thoughtRepository;
        private readonly IDbContextFactory<KotobaDbContext> _dbFactory;
        private readonly IMapper _mapper;
        private static readonly TimeSpan ThoughtLifetime = TimeSpan.FromHours(24);
        private readonly IFollowService _followService;

        public CurrentThoughtService(
            ICurrentThoughtRepository thoughtRepository,
            IDbContextFactory<KotobaDbContext> dbFactory,
            IMapper mapper,
            IFollowService followService)
        {
            _thoughtRepository = thoughtRepository;
            _dbFactory = dbFactory;
            _mapper = mapper;
            _followService = followService;
        }

        public async Task<string?> GetThoughtAsync(string userId)
        {
            var thought = await _thoughtRepository.GetByUserIdAsync(userId);
            if (thought == null) return null;

            var now = DateTime.UtcNow;
            if (thought.ExpiresAt <= now)
            {
                await _thoughtRepository.DeleteAsync(thought.Id);
                return null;
            }

            return thought.Content;
        }

        public async Task<bool> SetThoughtAsync(string userId, string content, ThoughtPrivacy privacy = ThoughtPrivacy.Public)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(content))
                return false;

            using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.AccountStatus != AccountStatus.Active)
                return false;

            var existing = await db.CurrentThoughts
                .FirstOrDefaultAsync(ct => ct.UserId == userId);

            if (existing is null)
            {
                var newThought = new CurrentThought
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Content = content,
                    Privacy = privacy,  
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(ThoughtLifetime)
                };
                db.CurrentThoughts.Add(newThought);
            }
            else
            {
                existing.Content = content;
                existing.Privacy = privacy;  
                existing.CreatedAt = DateTime.UtcNow;
                existing.ExpiresAt = DateTime.UtcNow.Add(ThoughtLifetime);
                db.CurrentThoughts.Update(existing);
            }

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<CurrentThoughtDto?> GetByIdAsync(Guid id)
        {
            var thought = await _thoughtRepository.GetByIdAsync(id);
            return _mapper.Map<CurrentThoughtDto>(thought);
        }

        public async Task<List<CurrentThoughtDto>> GetActiveAsync(string? currentUserId = null)
        {
            var thoughts = await _thoughtRepository.GetActiveAsync(currentUserId);
            return _mapper.Map<List<CurrentThoughtDto>>(thoughts);
        }

        public async Task<List<CurrentThoughtDto>> GetActiveFollowingThoughtsAsync(string userId)
        {
            var thoughts = await _thoughtRepository.GetActiveFollowingThoughtsAsync(userId);
            return _mapper.Map<List<CurrentThoughtDto>>(thoughts);
        }

        public async Task<bool> DeleteAsync(Guid id, string userId)
        {
            var thought = await _thoughtRepository.GetByIdAsync(id);
            if (thought == null || thought.UserId != userId)
                return false;

            return await _thoughtRepository.DeleteAsync(id);
        }
        public async Task<string?> GetThoughtForViewerAsync(string ownerId, string viewerId)
        {
            var thought = await _thoughtRepository.GetByUserIdAsync(ownerId);
            if (thought == null) return null;

            if (thought.ExpiresAt <= DateTime.UtcNow)
            {
                await _thoughtRepository.DeleteAsync(thought.Id);
                return null;
            }

            if (thought.Privacy == ThoughtPrivacy.Public)
                return thought.Content;

            var isFollowing = await _followService.IsFollowingAsync(viewerId, ownerId);
            return isFollowing ? thought.Content : null;
        }
    }
}
