using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;

namespace Kotoba.Modules.Domain.Interfaces
{
    public interface IStoryRepository
    {
        Task<Story?> AddAsync(Story story);

        Task<List<Story>> GetActiveAsync();
    }
}
