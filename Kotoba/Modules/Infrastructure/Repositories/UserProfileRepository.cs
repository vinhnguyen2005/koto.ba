using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Buffers;

namespace Kotoba.Modules.Infrastructure.Repositories
{
    public class UserProfileRepository
    {
        private readonly IDbContextFactory<KotobaDbContext> _factory;

        public UserProfileRepository(IDbContextFactory<KotobaDbContext> factory)
        {
            _factory = factory;
        }

        public List<UserProfile> GetUsersByDisplayNameAsync(string searchValue)
        {
            using var _context = _factory.CreateDbContext();
            return _context.Users
                .Where(u => u.DisplayName.Contains(searchValue)
                            && u.AccountStatus != Domain.Enums.AccountStatus.Deleted)
                .Select(u => new UserProfile
                {
                    UserId = u.Id,
                    DisplayName = u.DisplayName,
                    AccountStatus = u.AccountStatus
                })
                .ToList(); 
        }

        public async Task<User?> GetByIdAsync(string userId)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            return await ctx.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
