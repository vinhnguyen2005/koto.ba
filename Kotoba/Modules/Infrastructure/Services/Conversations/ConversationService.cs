using Azure.Core;
using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Kotoba.Modules.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;


namespace Kotoba.Modules.Infrastructure.Services.Conversations
{
    public class ConversationService : IConversationService
    {        
        ConversationParticipantRepository _conversationParticipantRepository;
        ConversationRepository _conversationRepository;
        MessageRepository _messageRepository;
        public ConversationService(ConversationParticipantRepository conversationParticipantRepository,
            ConversationRepository conversationRepository,
            MessageRepository messageRepository)
        {            
            _conversationParticipantRepository = conversationParticipantRepository;
            _conversationRepository = conversationRepository;
            _messageRepository = messageRepository;
        }

        public async Task<ConversationDto?> CreateSelfDirectConversationAsync(string userAId)
        {
            Conversation newConversation = new Conversation
            {
                Id = Guid.Parse(userAId),
                Type = ConversationType.Direct
            };
            await _conversationRepository.AddAsync(newConversation);
            await _conversationParticipantRepository.AddAsync(new ConversationParticipant
            {
                ConversationId = newConversation.Id,
                UserId = userAId
            });            

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
        public async Task<ConversationDto?> CreateDirectConversationAsync(string userAId, string userBId)
        {
            Conversation newConversation = new Conversation
            {
                Type = ConversationType.Direct                
            };

            await _conversationRepository.AddAsync(newConversation);
            await _conversationParticipantRepository.AddAsync(new ConversationParticipant
            {
                ConversationId = newConversation.Id,
                UserId = userAId
            });
            await _conversationParticipantRepository.AddAsync(new ConversationParticipant
            {
                ConversationId = newConversation.Id,
                UserId = userBId
            });


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

        public async Task<ConversationDto?> CreateGroupConversationAsync(CreateGroupRequest request)
        {
            ConversationType type = request.ParticipantIds.Count() < 3 ? ConversationType.Direct : ConversationType.Group;
            Conversation newConversation = new Conversation
            {
                Type = type,
                GroupName = request.GroupName
            };
            await _conversationRepository.AddAsync(newConversation);            

            foreach(string participantId in request.ParticipantIds)
            {
                await _conversationParticipantRepository.AddAsync(new ConversationParticipant
                {
                    ConversationId = newConversation.Id,
                    UserId = participantId
                });                
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

        public async Task<ConversationDto?> GetConversationDetailAsync(string conversationId)
        {
            return await _conversationRepository.GetConversationDetailByIdAsync(conversationId);
        }

        public async Task<List<ConversationDto>> FindGroupConversationsAsync(string userId, string groupName)
        {
            if(!string.IsNullOrEmpty(userId))
            {
                List<ConversationParticipant> conversations = await _conversationParticipantRepository.GetAllConversationByGroupNameForUserAsync(userId, groupName);
                List<ConversationDto> conversationDtos = conversations.Select(cp => new ConversationDto
                {
                    ConversationId = cp.ConversationId,
                    Type = cp.Conversation.Type,
                    GroupName = cp.Conversation.GroupName,
                    CreatedAt = cp.Conversation.CreatedAt,
                    UpdatedAt = cp.Conversation.UpdatedAt
                }).ToList();
                return await Task.FromResult(conversationDtos);
            }
            return await Task.FromResult(new List<ConversationDto>());
        }

        public async Task<List<ConversationDto>> GetUserConversationsAsync(string userId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                List<ConversationParticipant> conversations = await _conversationParticipantRepository.GetAllConversationByUserAsync(userId);
                List<ConversationDto> conversationDtos = conversations.Select(cp => new ConversationDto
                {
                    ConversationId = cp.ConversationId,
                    Type = cp.Conversation.Type,
                    GroupName = cp.Conversation.GroupName,
                    CreatedAt = cp.Conversation.CreatedAt,
                    UpdatedAt = cp.Conversation.UpdatedAt,
                    Participants = cp.Conversation.Participants.Select(p => new UserProfile
                    {
                        UserId = p.UserId,
                        DisplayName = p.User?.DisplayName ?? "",
                        AvatarUrl = p.User?.AvatarUrl,
                        IsOnline = p.User?.IsOnline ?? false
                    }).ToList()
                }).ToList();
                return conversationDtos;
            }
            return new List<ConversationDto>();
        }

        public async Task<ConversationDto?> FindDirectConversationsAsync(string userAId, string userBId)
        {
            if (string.IsNullOrEmpty(userAId) || string.IsNullOrEmpty(userBId))
                return await Task.FromResult<ConversationDto?>(null);

            if(userAId.Equals(userBId))
            {
                // Check if a direct conversation already exists for the user with themselves
                var selfConv = await _conversationRepository.GetConversationDetailByIdAsync(userAId);                
                if (selfConv == null)
                {
                    var conversationTask = CreateSelfDirectConversationAsync(userAId);
                    return await conversationTask;
                }
                return selfConv;
            }

            var userAConvIds = await _conversationParticipantRepository.GetAllConversationIdsForUserAsync(userAId);
            var userBConvIds = await _conversationParticipantRepository.GetAllConversationIdsForUserAsync(userBId);
            var sharedConvId = userAConvIds.Intersect(userBConvIds).FirstOrDefault();

            if (sharedConvId == default)
            {
                // Await the task returned by CreateGroupConversationAsync and wrap it in Task.FromResult              
                var conversationTask = CreateDirectConversationAsync(userAId, userBId);
                return await conversationTask;                               
            }

            var conversation = await _conversationRepository.GetConversationByIdAsync(sharedConvId);

            return await Task.FromResult(conversation);
        }

        public Task<List<UserProfile>> GetOtherUsersInConversationsAsync(string conversationId, string userId)
        {
            return _conversationParticipantRepository.GetOtherUsersInConversationAsync(conversationId, userId);
        }

        public async Task<List<MessageDto>> GetMessagesAsync(string conversationId)
        {
            var convId = Guid.Parse(conversationId);            

            return await _messageRepository.GetMessagesAsync(convId);
        }
    }
}
