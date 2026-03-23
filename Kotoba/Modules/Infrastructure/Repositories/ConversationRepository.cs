using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly IDbContextFactory<KotobaDbContext> _dbFactory;

        public ConversationRepository(IDbContextFactory<KotobaDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }


        public async Task<IEnumerable<Conversation>> GetAllAsync()
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.Conversations
                .ToListAsync();
        }

        public async Task<ConversationDto?> GetConversationByIdAsync(Guid conversationId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.Conversations
                .Where(c => c.Id == conversationId)
                .Select(c => new ConversationDto
                {
                    ConversationId = c.Id,
                    Type = c.Type,
                    GroupName = c.GroupName,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }
        public async Task AddAsync(Conversation conversation)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            await _context.Conversations.AddAsync(conversation);
            await _context.SaveChangesAsync();
        }

        public Task<Conversation?> GetAsync(Guid conversationId)
        {
            throw new NotImplementedException();
        }

        public async Task<ConversationDto?> GetConversationDetailByIdAsync(string conversationId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.Conversations
                .Where(c => conversationId != null && c.Id.ToString() == conversationId)
                .Select(c => new ConversationDto
                {
                    ConversationId = c.Id,
                    Type = c.Type,                    
                    GroupName = c.GroupName,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }
    }
}
