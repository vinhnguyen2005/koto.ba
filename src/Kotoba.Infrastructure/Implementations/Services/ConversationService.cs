using Kotoba.Application.DTOs;
using Kotoba.Application.Interfaces;
using Kotoba.Domain.Entities;
using Kotoba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kotoba.Infrastructure.Implementations.Services
{
    public class ConversationService : IConversationService
    {
        // !MUST BE REMOVED AFTER
        private static readonly List<ConversationDto> _conversations = new();
        private static readonly List<MessageDto> _messages = new();

        public Task<ConversationDto?> CreateDirectConversationAsync(string userAId, string userBId)
        {
            throw new NotImplementedException();
        }
        public Task<ConversationDto?> CreateGroupConversationAsync(CreateGroupRequest request)
        {
            throw new NotImplementedException();
        }
        public Task<(List<ConversationDto> Conversations, List<MessageDto> Messages)> GetUserConversationsAsync(string userId)
        {
            if (_conversations.Count == 0)
            {
                var random = Random.Shared;

                for (int i = 0; i < 5; i++)
                {
                    var conversationId = Guid.NewGuid();
                    var isGroup = random.Next(2) == 0;

                    var participantIds = new List<string>
            {
                userId,
                $"user-{i + 1}",
                $"user-{i + 2}"
            };

                    _conversations.Add(new ConversationDto
                    {
                        ConversationId = conversationId,
                        Type = isGroup ? ConversationType.Group : ConversationType.Direct,
                        GroupName = isGroup ? $"Group {i}" : null,
                        ParticipantIds = participantIds,
                        CreatedAt = DateTime.UtcNow.AddDays(-i)
                    });

                    for (int j = 0; j < 5; j++)
                    {
                        _messages.Add(new MessageDto
                        {
                            MessageId = Guid.NewGuid(),
                            ConversationId = conversationId,
                            SenderId = participantIds[j % participantIds.Count],
                            Content = $"Message {j} in conversation {i}",
                            CreatedAt = DateTime.UtcNow.AddMinutes(-(j * 5))
                        });
                    }
                }
            }

            return Task.FromResult((_conversations, _messages));
        }

        public Task AddMessage(string conversationId, string messageContent)
        {
            if (!Guid.TryParse(conversationId, out var conversationGuid))
                throw new ArgumentException("Invalid conversation id");

            _messages.Add(new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conversationGuid,
                SenderId = "user-1",
                Content = messageContent,
                CreatedAt = DateTime.UtcNow
            });

            return Task.CompletedTask;
        }

        public Task<ConversationDto?> GetConversationDetailAsync(Guid conversationId)
        {
            throw new NotImplementedException();
        }
    }
}
