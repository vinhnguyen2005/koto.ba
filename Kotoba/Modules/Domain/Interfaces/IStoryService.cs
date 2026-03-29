using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.Interfaces;

/// <summary>
/// Service for managing time-limited stories.
/// </summary>
public interface IStoryService
{
    Task<StoryDto?> CreateStoryAsync(CreateStoryRequest request);
    Task<List<StoryDto>> GetActiveStoriesAsync();
    Task<List<StoryDto>> GetActiveStoriesByUserIdAsync(string userId);
    Task MarkStoriesAsSeenAsync(string viewerId, List<Guid> storyIds);
    Task ReactToStoryAsync(string userId, Guid storyId, ReactionType type);
}
