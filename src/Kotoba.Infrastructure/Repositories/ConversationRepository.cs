using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kotoba.Domain.Entities;
using Kotoba.Domain.Interfaces;
using Kotoba.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Infrastructure.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly ApplicationDbContext _context;

        public ConversationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Conversation>> GetAllAsync()
        {
            return await _context.Conversations
                .ToListAsync();
        }

        public async Task<Conversation?> GetAsync(Guid conversationId)
        {
            return await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }
    }
}
