using Kotoba.Modules.Domain.DTOs;

namespace Kotoba.Modules.Domain.Interfaces;

/// <summary>
/// Service for managing time-limited stories.
/// </summary>
public interface IStoryService
{
    Task<StoryDto?> CreateStoryAsync(CreateStoryRequest request);
    Task<List<StoryDto>> GetActiveStoriesAsync();

    Task<List<StoryDto>> GetActiveStoriesByUserIdAsync(string userId);
}
