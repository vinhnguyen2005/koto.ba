using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace Kotoba.Modules.Infrastructure.Services.Conversations
{
    public class ConversationService : IConversationService
    {
        private KotobaDbContext _context;
        public ConversationService(IDbContextFactory<KotobaDbContext> DbFactory)
        {
            _context = DbFactory.CreateDbContext();
        }
        public Task<ConversationDto?> CreateDirectConversationAsync(string userAId, string userBId)
        {
            throw new NotImplementedException();
        }

        public async Task<ConversationDto?> CreateGroupConversationAsync(CreateGroupRequest request)
        {
            ConversationType type = request.ParticipantIds.Count() < 3 ? ConversationType.Direct : ConversationType.Group;
            Conversation newConversation = new Conversation
            {
                Type = type,
                GroupName = request.GroupName
            };
            _context.Conversations.Add(newConversation);
            await _context.SaveChangesAsync();

            foreach(string participantId in request.ParticipantIds)
            {
                _context.ConversationParticipants.Add(new ConversationParticipant
                {
                    ConversationId = newConversation.Id,
                    UserId = participantId
                });
                await _context.SaveChangesAsync();
            }                                    

            ConversationDto conversationDto = new ConversationDto
            {
                ConversationId = newConversation.Id,
                Type = newConversation.Type,
                GroupName = newConversation.GroupName,
                CreatedAt = newConversation.CreatedAt,
                UpdatedAt = newConversation.UpdatedAt
            };
            return conversationDto;
        }

        public Task<ConversationDto?> GetConversationDetailAsync(Guid conversationId)
        {
            throw new NotImplementedException();
        }

        public Task<List<ConversationDto>> FindGroupConversationsAsync(string userId, string groupName)
        {
            if(!string.IsNullOrEmpty(userId))
            {
                List<ConversationParticipant> conversations = _context.ConversationParticipants.Include(cp => cp.Conversation).Where(cp => cp.UserId != null && cp.UserId.Equals(userId) && cp.Conversation.GroupName != null && cp.Conversation.GroupName.Equals(groupName)).ToList();
                List<ConversationDto> conversationDtos = conversations.Select(cp => new ConversationDto
                {
                    ConversationId = cp.ConversationId,
                    Type = cp.Conversation.Type,
                    GroupName = cp.Conversation.GroupName,
                    CreatedAt = cp.Conversation.CreatedAt,
                    UpdatedAt = cp.Conversation.UpdatedAt
                }).ToList();
                return Task.FromResult(conversationDtos);
            }
            return Task.FromResult(new List<ConversationDto>());
        }

        public Task<List<ConversationDto>> GetUserConversationsAsync(string userId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                List<ConversationParticipant> conversations = _context.ConversationParticipants.Include(cp => cp.Conversation).Where(cp => cp.UserId != null && cp.UserId.Equals(userId)).ToList();
                List<ConversationDto> conversationDtos = conversations.Select(cp => new ConversationDto
                {
                    ConversationId = cp.ConversationId,
                    Type = cp.Conversation.Type,
                    GroupName = cp.Conversation.GroupName,
                    CreatedAt = cp.Conversation.CreatedAt,
                    UpdatedAt = cp.Conversation.UpdatedAt
                }).ToList();
                return Task.FromResult(conversationDtos);
            }
            return Task.FromResult(new List<ConversationDto>());
        }

        public Task<ConversationDto?> FindDirectConversationsAsync(string userAId, string userBId)
        {
            if (string.IsNullOrEmpty(userAId) || string.IsNullOrEmpty(userBId))
                return Task.FromResult<ConversationDto?>(null);

            // Find conversation IDs where userA is a participant
            var userAConvIds = _context.ConversationParticipants
                .Where(cp => cp.UserId == userAId)
                .Select(cp => cp.ConversationId);

            // Find conversation IDs where userB is a participant
            var userBConvIds = _context.ConversationParticipants
                .Where(cp => cp.UserId == userBId)
                .Select(cp => cp.ConversationId);

            // The direct conversation is the one shared by BOTH users
            var sharedConvId = userAConvIds.Intersect(userBConvIds).FirstOrDefault();

            if (sharedConvId == default)
            {
                // Await the task returned by CreateGroupConversationAsync and wrap it in Task.FromResult
                if (!userAId.Equals(userBId))
                {
                    var conversationTask = CreateGroupConversationAsync(new CreateGroupRequest { ParticipantIds = [userAId, userBId] });
                    return conversationTask;
                }
                else
                {
                    var conversationTask = CreateGroupConversationAsync(new CreateGroupRequest { ParticipantIds = [userAId] });
                    return conversationTask;
                }
            }

            var conversation = _context.Conversations
                .Where(c => c.Id == sharedConvId && c.Type == Domain.Enums.ConversationType.Direct)
                .Select(c => new ConversationDto
                {
                    ConversationId = c.Id,
                    Type = c.Type,
                    GroupName = c.GroupName,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefault();

            return Task.FromResult(conversation);
        }
    }
}
