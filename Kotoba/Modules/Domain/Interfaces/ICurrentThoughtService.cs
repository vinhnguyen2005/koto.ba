using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.Interfaces
{
    public interface ICurrentThoughtService
    {
        Task<bool> SetThoughtAsync(string userId, string content, ThoughtPrivacy privacy = ThoughtPrivacy.Public);
        Task<string?> GetThoughtAsync(string userId);
        Task<CurrentThoughtDto?> GetByIdAsync(Guid id);
        Task<List<CurrentThoughtDto>> GetActiveAsync(string? currentUserId = null);
        Task<List<CurrentThoughtDto>> GetActiveFollowingThoughtsAsync(string userId);
        Task<bool> DeleteAsync(Guid id, string userId);
        Task<string?> GetThoughtForViewerAsync(string ownerId, string viewerId);
    }
}
