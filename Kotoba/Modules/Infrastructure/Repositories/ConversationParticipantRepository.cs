using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Repositories
{
    public class ConversationParticipantRepository : IConversationParticipantRepository
    {
        private readonly IDbContextFactory<KotobaDbContext> _dbFactory;

        public ConversationParticipantRepository(IDbContextFactory<KotobaDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }


        public async Task<IEnumerable<ConversationParticipant>> GetAllAsync()
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.ConversationParticipants.ToListAsync();
        }

        public async Task<bool> IsParticipant(Guid conversationId, string senderId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.ConversationParticipants
            .AnyAsync(p => p.ConversationId == conversationId
                        && p.UserId == senderId
                        && p.IsActive);
        }

        public async Task<List<Guid>> GetAllConversationIdsForUserAsync(string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.ConversationParticipants
                .Where(p => p.UserId == userId && p.IsActive)
                .Select(p => p.ConversationId)
                .ToListAsync(); 
        }
        public async Task AddAsync(ConversationParticipant participant)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            await _context.ConversationParticipants.AddAsync(participant);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ConversationParticipant>> GetAllConversationByGroupNameForUserAsync(string userId, string groupName)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.ConversationParticipants
            .Include(p => p.Conversation)
            .Where(p => p.UserId == userId
                        && p.IsActive
                        && p.Conversation.Type == ConversationType.Group
                        && p.Conversation.GroupName != null
                        && p.Conversation.GroupName.Contains(groupName))
            .ToListAsync();
        }

        public async Task<List<ConversationParticipant>> GetAllConversationByUserAsync(string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.ConversationParticipants
                .Include(p => p.Conversation)
                    .ThenInclude(c => c.Participants)
                        .ThenInclude(p => p.User)
                .Where(p => p.UserId == userId && p.IsActive)
                .ToListAsync();
        }

        public async Task<List<UserProfile>> GetOtherUsersInConversationAsync(string conversationId, string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.ConversationParticipants
                .Where(p => p.ConversationId.ToString() == conversationId
                         && p.UserId != userId
                         && p.IsActive)
                .Select(p => new UserProfile
                {
                    UserId = p.User.Id,
                    DisplayName = p.User.DisplayName,
                    AvatarUrl = p.User.AvatarUrl,
                    IsOnline = p.User.IsOnline,
                    LastSeenAt = p.User.LastSeenAt
                })
                .ToListAsync();
        }
    }
}
