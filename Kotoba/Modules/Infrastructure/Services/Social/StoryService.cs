using AutoMapper;
using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Services.Social
{
    public class StoryService : IStoryService
    {
        private readonly IStoryRepository _storyRepository;
        private readonly IDbContextFactory<KotobaDbContext> _dbFactory;
        private readonly IMapper _mapper;
        private static readonly TimeSpan StoryLifetime = TimeSpan.FromHours(24);

        public StoryService(IStoryRepository storyRepository,
            IDbContextFactory<KotobaDbContext> dbFactory,
            IMapper mapper)
        {
            _storyRepository = storyRepository;
            _dbFactory = dbFactory;
            _mapper = mapper;
        }

        public async Task<StoryDto?> CreateStoryAsync(CreateStoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.Content))
                return null;

            await using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (user == null || user.AccountStatus != AccountStatus.Active)
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

            await _storyRepository.AddAsync(story);

            return _mapper.Map<StoryDto>(story);
        }

        public async Task<List<StoryDto>> GetActiveStoriesAsync()
        {
            var stories = await _storyRepository.GetActiveAsync();
            return _mapper.Map<List<StoryDto>>(stories);
        }

        public async Task<List<StoryDto>> GetActiveStoriesByUserIdAsync(string userId)
        {
            var stories = await _storyRepository.GetActiveByUserIdAsync(userId);
            return _mapper.Map<List<StoryDto>>(stories);
        }
    }
}
