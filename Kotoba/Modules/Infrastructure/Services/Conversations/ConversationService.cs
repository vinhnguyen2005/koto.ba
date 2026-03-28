using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Kotoba.Modules.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;


namespace Kotoba.Modules.Infrastructure.Services.Conversations
{
    public class ConversationService : IConversationService
    {
        private readonly ConversationParticipantRepository _conversationParticipantRepository;
        private readonly ConversationRepository _conversationRepository;
        private readonly MessageRepository _messageRepository;
        private readonly IDbContextFactory<KotobaDbContext> _dbFactory;

        public ConversationService(
            ConversationParticipantRepository conversationParticipantRepository,
            ConversationRepository conversationRepository,
            MessageRepository messageRepository,
            IDbContextFactory<KotobaDbContext> dbFactory)
        {
            _conversationParticipantRepository = conversationParticipantRepository;
            _conversationRepository = conversationRepository;
            _messageRepository = messageRepository;
            _dbFactory = dbFactory;
        }

        public async Task<ConversationDto?> CreateSelfDirectConversationAsync(string userAId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userAId);

            if (user == null || user.AccountStatus != AccountStatus.Active)
                return null;

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
            await using var db = await _dbFactory.CreateDbContextAsync();

            var users = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userAId || u.Id == userBId)
                .Select(u => new { u.Id, u.AccountStatus })
                .ToListAsync();

            var userA = users.FirstOrDefault(u => u.Id == userAId);
            var userB = users.FirstOrDefault(u => u.Id == userBId);

            if (userA == null || userB == null)
                return null;

            if (userA.AccountStatus != AccountStatus.Active)
                return null;

            if (userB.AccountStatus == AccountStatus.Deleted)
                return null;

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
            if (request.ParticipantIds == null || !request.ParticipantIds.Any())
                return null;

            await using var db = await _dbFactory.CreateDbContextAsync();

            var users = await db.Users
                .AsNoTracking()
                .Where(u => request.ParticipantIds.Contains(u.Id))
                .Select(u => new { u.Id, u.AccountStatus })
                .ToListAsync();

            if (users.Count != request.ParticipantIds.Count)
                return null;

            if (users.Any(u => u.AccountStatus != AccountStatus.Active))
                return null;

            var type = !string.IsNullOrEmpty(request.GroupName)
                || request.ParticipantIds.Count >= 3
                ? ConversationType.Group
                : ConversationType.Direct;

            Conversation newConversation = new Conversation
            {
                Type = type,
                GroupName = request.GroupName,
                OwnerId = request.CreatorId
            };
            await _conversationRepository.AddAsync(newConversation);

            var adminIds = request.AdminIds ?? new List<string>();

            foreach (string participantId in request.ParticipantIds)
            {
                GroupRole role;
                if (participantId == request.CreatorId)
                {
                    role = GroupRole.Owner; 
                }
                else if (adminIds.Contains(participantId))
                {
                    role = GroupRole.Admin;  
                }
                else
                {
                    role = GroupRole.Member; 
                }

                await _conversationParticipantRepository.AddAsync(new ConversationParticipant
                {
                    ConversationId = newConversation.Id,
                    UserId = participantId,
                    Role = role
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
                    LastMessage = cp.Conversation.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => new MessageDto
                        {
                            MessageId = m.Id,
                            SenderId = m.SenderId,
                            Content = m.Content ?? ((m.Attachments != null && m.Attachments.Count > 0) ? "📎 Attachment" : null),
                            CreatedAt = m.CreatedAt
                        }).FirstOrDefault(),
                    Participants = cp.Conversation.Participants
                        .Where(p => p.IsActive)
                        .Select(p => new UserProfile
                    {
                        UserId = p.UserId,
                        DisplayName = p.User?.DisplayName ?? "",
                        AvatarUrl = p.User?.AvatarUrl,
                        IsOnline = p.User?.IsOnline ?? false,
                        AccountStatus = p.User?.AccountStatus ?? AccountStatus.Active
                    }).ToList()
                }).ToList();
                return conversationDtos;
            }
            return new List<ConversationDto>();
        }

        public async Task<ConversationDto?> FindDirectConversationsAsync(string userAId, string userBId)
        {
            if (string.IsNullOrEmpty(userAId) || string.IsNullOrEmpty(userBId))
                return null;

            if (userAId.Equals(userBId))
            {
                var selfConv = await _conversationRepository.GetConversationDetailByIdAsync(userAId);
                if (selfConv == null)
                {
                    return await CreateSelfDirectConversationAsync(userAId);
                }
                return selfConv;
            }

            var sharedConversation = await _conversationRepository
                .GetDirectConversationAsync(userAId, userBId);

            if (sharedConversation == null)
            {
                return await CreateDirectConversationAsync(userAId, userBId);
            }

            return sharedConversation;
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

        public Task<List<UserProfile>> GetAllUsersInConversationAsync(string conversationId)
        {
            return _conversationParticipantRepository.GetAllUsersInConversationAsync(conversationId);
        }
    }
}
