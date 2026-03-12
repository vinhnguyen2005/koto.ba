using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Kotoba.Domain.Entities;
using Kotoba.Domain.Interfaces;
using Kotoba.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Infrastructure.Repositories
{
    public class ConversationParticipantRepository : IConversationParticipantRepository
    {
        private readonly ApplicationDbContext _context;

        public ConversationParticipantRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ConversationParticipant>> GetAllAsync()
        {
            return await _context.ConversationParticipants
                .ToListAsync();
        }

        public async Task<bool> IsParticipant(Guid conversationId, string senderId)
        {
            return await _context.ConversationParticipants
            .AnyAsync(p => p.ConversationId == conversationId
                        && p.UserId == senderId
                        && p.IsActive);
        }
    }
}
