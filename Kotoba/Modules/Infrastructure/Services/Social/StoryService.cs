using AutoMapper;
using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Hubs;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Services.Social
{
    public class StoryService : IStoryService
    {
        private readonly IStoryRepository _storyRepository;
        private readonly IDbContextFactory<KotobaDbContext> _dbFactory;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IMapper _mapper;
        private static readonly TimeSpan StoryLifetime = TimeSpan.FromHours(24);

        public StoryService(IStoryRepository storyRepository,
            IDbContextFactory<KotobaDbContext> dbFactory,
            IHubContext<NotificationHub> hub,
            IMapper mapper)
        {
            _storyRepository = storyRepository;
            _dbFactory = dbFactory;
            _hub = hub;
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

            var visibility = request.Visibility?.ToLower() ?? "public";

            if (visibility == "specific" &&
                (request.AllowedUserIds == null || !request.AllowedUserIds.Any()))
            {
                return null;
            }

            var story = new Story
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Content = request.Content,
                MediaUrl = request.MediaUrl,
                CreatedAt = DateTime.UtcNow,
                Visibility = visibility,
                ExpiresAt = DateTime.UtcNow.Add(StoryLifetime)
            };

            if (visibility == "specific")
            {
                var validUserIds = await db.Users
                    .Where(u => request.AllowedUserIds!.Contains(u.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                story.Permissions = validUserIds
                    .Distinct()
                    .Select(uid => new StoryPermission
                    {
                        Id = Guid.NewGuid(),
                        StoryId = story.Id,
                        UserId = uid
                    })
                    .ToList();
            }

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

        public async Task<List<StoryDto>> GetActiveFollowingStoriesAsync(string userId)
        {
            var stories = await _storyRepository.GetActiveFollowingStoriesAsync(userId);
            return _mapper.Map<List<StoryDto>>(stories);
        }

        public async Task MarkStoriesAsSeenAsync(string viewerId, List<Guid> storyIds)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var stories = await db.Stories
                .Where(s => storyIds.Contains(s.Id))
                .Select(s => new { s.Id, s.UserId })
                .ToListAsync();

            var existingViews = await db.StoryViews
                .Where(v => v.ViewerId == viewerId && storyIds.Contains(v.StoryId))
                .ToListAsync();

            var newViews = new List<StoryView>();

            foreach (var s in stories)
            {
                if (!existingViews.Any(v => v.StoryId == s.Id))
                {
                    newViews.Add(new StoryView
                    {
                        Id = Guid.NewGuid(),
                        StoryId = s.Id,
                        ViewerId = viewerId
                    });
                }
            }

            if (newViews.Any())
            {
                db.StoryViews.AddRange(newViews);
                await db.SaveChangesAsync();
            }

            var uploaderGroups = stories
                .Where(s => s.UserId != viewerId)
                .GroupBy(s => s.UserId);

            foreach (var group in uploaderGroups)
            {
                var uploaderId = group.Key;


                var alreadyNotified = await db.Notifications
                    .Where(n =>
                        n.RecipientId == uploaderId &&
                        n.ActorId == viewerId &&
                        n.Type == NotificationType.StorySeen &&
                        n.CreatedAt > DateTime.UtcNow.AddSeconds(-10))
                    .AnyAsync();

                if (alreadyNotified) continue;

                var viewer = await db.Users
                    .Where(u => u.Id == viewerId)
                    .Select(u => new { u.DisplayName, u.AvatarUrl })
                    .FirstOrDefaultAsync();

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    RecipientId = uploaderId,
                    ActorId = viewerId,
                    Type = NotificationType.StorySeen,
                    Message = $"{viewer?.DisplayName ?? "Someone"} viewed your story"
                };

                db.Notifications.Add(notification);
                await db.SaveChangesAsync();

                var dto = new NotificationDto
                {
                    Id = notification.Id,
                    Type = NotificationType.StorySeen,

                    ActorId = viewerId,
                    ActorName = viewer?.DisplayName,
                    ActorAvatar = viewer?.AvatarUrl,

                    TargetId = null,
                    TargetType = "Story",

                    Message = $"{viewer?.DisplayName ?? "Someone"} viewed your story",

                    IsRead = false,
                    CreatedAt = notification.CreatedAt
                };

                await _hub.Clients.Group(uploaderId)
                    .SendAsync("ReceiveNotification", dto);
                await _hub.Clients.Group(uploaderId)
                    .SendAsync("NotifyStorySeen", dto);
            }
        }

        public async Task ReactToStoryAsync(string userId, Guid storyId, ReactionType type)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var story = await db.Stories
                .Where(s => s.Id == storyId)
                .Select(s => new { s.Id, s.UserId })
                .FirstOrDefaultAsync();

            if (story == null || story.UserId == userId)
                return;

            var existing = await db.StoryReactions
                .FirstOrDefaultAsync(r => r.StoryId == storyId && r.UserId == userId);

            if (existing != null)
            {
                existing.Type = type;
            }
            else
            {
                db.StoryReactions.Add(new StoryReaction
                {
                    Id = Guid.NewGuid(),
                    StoryId = storyId,
                    UserId = userId,
                    Type = type
                });
            }

            await db.SaveChangesAsync();

            var alreadyNotified = await db.Notifications.AnyAsync(n =>
                n.RecipientId == story.UserId &&
                n.ActorId == userId &&
                n.TargetId == storyId.ToString() &&
                n.Type == NotificationType.StoryReaction &&
                n.CreatedAt > DateTime.UtcNow.AddSeconds(-10));

            if (alreadyNotified) return;

            var viewer = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.DisplayName, u.AvatarUrl })
                .FirstAsync();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                RecipientId = story.UserId,
                ActorId = userId,
                TargetId = storyId.ToString(),
                TargetType = "Story",
                Type = NotificationType.StoryReaction,
                Message = $"{viewer.DisplayName} reacted to your story"
            };

            db.Notifications.Add(notification);
            await db.SaveChangesAsync();

            var dto = new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                ActorId = userId,
                ActorName = viewer.DisplayName,
                ActorAvatar = viewer.AvatarUrl,
                TargetId = storyId.ToString(),
                TargetType = "Story",
                Message = notification.Message,
                CreatedAt = notification.CreatedAt,
                IsRead = false
            };

            await _hub.Clients.Group(story.UserId)
                .SendAsync("ReceiveNotification", dto);

            await _hub.Clients.Group(story.UserId)
                .SendAsync("NotifyStoryReaction", dto);
        }
    }
}
