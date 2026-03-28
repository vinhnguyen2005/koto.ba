using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Services.Conversations
{
    public class GroupAdminService : IGroupAdminService
    {
        private readonly IDbContextFactory<KotobaDbContext> _dbFactory;
        public GroupAdminService(IDbContextFactory<KotobaDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<GroupRole?> GetUserRoleAsync(Guid conversationId, string userId)
        {
            await using var _context = await _dbFactory.CreateDbContextAsync();
            var participant = await _context.ConversationParticipants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId
                                        && p.UserId == userId
                                        && p.IsActive);
            return participant?.Role;
        }

        public async Task<bool> IsOwnerAsync(Guid conversationId, string userId)
        {
            var role = await GetUserRoleAsync(conversationId, userId);
            return role == GroupRole.Owner;
        }

        public async Task<bool> IsAdminAsync(Guid conversationId, string userId)
        {
            var role = await GetUserRoleAsync(conversationId, userId);
            return role == GroupRole.Admin;
        }

        public async Task<bool> IsOwnerOrAdminAsync(Guid conversationId, string userId)
        {
            var role = await GetUserRoleAsync(conversationId, userId);
            return role == GroupRole.Owner || role == GroupRole.Admin;
        }

        public async Task<bool> PromoteToAdminAsync(Guid conversationId, string targetUserId)
        {
            await using var _context = await _dbFactory.CreateDbContextAsync();
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId
                                        && p.UserId == targetUserId
                                        && p.IsActive
                                        && p.Role == GroupRole.Member);

            if (participant == null)
                return false;

            participant.Role = GroupRole.Admin;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DemoteFromAdminAsync(Guid conversationId, string targetUserId)
        {
            await using var _context = await _dbFactory.CreateDbContextAsync();
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId
                                        && p.UserId == targetUserId
                                        && p.IsActive
                                        && p.Role == GroupRole.Admin);

            if (participant == null)
                return false;

            participant.Role = GroupRole.Member;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TransferOwnershipAsync(Guid conversationId, string currentOwnerId, string newOwnerId)
        {
            await using var _context = await _dbFactory.CreateDbContextAsync();
            var conversation = await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation?.OwnerId != currentOwnerId)
                return false; ;
            var currentOwner = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId
                                        && p.UserId == currentOwnerId
                                        && p.IsActive
                                        && p.Role == GroupRole.Owner);
            var newOwner = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId
                                        && p.UserId == newOwnerId
                                        && p.IsActive);
            if (currentOwner == null || newOwner == null)
                return false;
            currentOwner.Role = GroupRole.Admin;
            newOwner.Role = GroupRole.Owner;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateGroupNameAsync(Guid conversationId, string newName)
        {
            await using var _context = await _dbFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(newName) || newName.Length > 200)
                return false;

            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation == null)
                return false;

            conversation.GroupName = newName;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string?> AutoTransferOwnershipOnLeaveAsync(Guid conversationId)
        {
            await using var _context = await _dbFactory.CreateDbContextAsync();
            var conversation = await _context.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return null;

            var leavingOwnerId = conversation.OwnerId;
            var seniorAdmin = await _context.ConversationParticipants
                .Where(p => p.ConversationId == conversationId
                         && p.IsActive
                         && p.UserId != leavingOwnerId
                         && p.Role == GroupRole.Admin)
                .OrderBy(p => p.JoinedAt)
                .FirstOrDefaultAsync();

            if (seniorAdmin != null)
            {
                seniorAdmin.Role = GroupRole.Owner;
                conversation.OwnerId = seniorAdmin.UserId;
                await _context.SaveChangesAsync();
                return seniorAdmin.UserId;
            }
            var oldestMember = await _context.ConversationParticipants
                .Where(p => p.ConversationId == conversationId
                         && p.IsActive
                         && p.UserId != leavingOwnerId
                         && p.Role == GroupRole.Member)
                .OrderBy(p => p.JoinedAt)
                .FirstOrDefaultAsync();

            if (oldestMember != null)
            {
                oldestMember.Role = GroupRole.Owner;
                conversation.OwnerId = oldestMember.UserId;
                await _context.SaveChangesAsync();
                return oldestMember.UserId;
            }
            conversation.OwnerId = null;
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<List<string>> GetAdminsAsync(Guid conversationId)
        {
            await using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.ConversationParticipants
                .Where(p => p.ConversationId == conversationId
                         && p.IsActive
                         && (p.Role == GroupRole.Admin || p.Role == GroupRole.Owner))
                .Select(p => p.UserId)
                .ToListAsync();
        }

        public async Task<bool> AddMemberAsync(Guid conversationId, string userId)
        {
            await using var _context = await _dbFactory.CreateDbContextAsync();
            var existing = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId
                                        && p.UserId == userId);

            if (existing != null)
            {
                if (existing.IsActive)
                    return false; 

                existing.IsActive = true;
                existing.LeftAt = null;
                existing.Role = GroupRole.Member;
                existing.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                await _context.ConversationParticipants.AddAsync(new ConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    UserId = userId,
                    Role = GroupRole.Member,  
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveMemberAsync(Guid conversationId, string userId)
        {
            await using var _context = await _dbFactory.CreateDbContextAsync();
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId
                                        && p.UserId == userId
                                        && p.IsActive);
            if (participant == null)
                return false;

            participant.IsActive = false;
            participant.LeftAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LeaveConversationAsync(Guid conversationId, string userId)
        {
            await using var _context = await _dbFactory.CreateDbContextAsync();
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId
                                        && p.UserId == userId
                                        && p.IsActive);
            if (participant == null)
                return false;

            participant.IsActive = false;
            participant.LeftAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
