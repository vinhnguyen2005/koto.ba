using Kotoba.Core.Interfaces;
using Kotoba.Shared.DTOs;
using Kotoba.Infrastructure.Data;
using Kotoba.Domain.Entities;
using Kotoba.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Kotoba.Domain.Interfaces;

namespace Kotoba.Infrastructure.Services.Messages;

public class MessageService : IMessageService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationParticipantRepository _conversationParticipantRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    public MessageService(
        IConversationRepository conversationRepository,
        IConversationParticipantRepository conversationParticipantRepository,
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork
        )
    {
        _conversationRepository = conversationRepository;
        _conversationParticipantRepository = conversationParticipantRepository;
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MessageDto?> SendMessageAsync(SendMessageRequest request)
    {
        // TODO: Implement send message
        var isParticipant = await _conversationParticipantRepository.IsParticipant(request.ConversationId, request.SenderId);

        if (!isParticipant) return null;
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            SenderId = request.SenderId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        };
        await _messageRepository.AddAsync(message);

        var conversation = await _conversationRepository.GetAsync(request.ConversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
        return new MessageDto
        {
            MessageId = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            Status = MessageStatus.Sent,
            Reactions = new List<ReactionDto>(),
            Attachments = new List<AttachmentDto>()
        };
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid conversationId, PagingRequest paging)
    {
        var messages = await _messageRepository.GetMessagesPageAsync(conversationId, paging.Page, paging.PageSize);

        return messages.Select(m => new MessageDto
        {
            MessageId = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            Status = MessageStatus.Sent,
            Reactions = m.Reactions.Select(r => new ReactionDto
            {
                MessageId = r.MessageId,
                UserId = r.UserId,
                Type = r.Type,
                CreatedAt = r.CreatedAt
            }).ToList(),
            Attachments = m.Attachments.Select(a => new AttachmentDto
            {
                AttachmentId = a.Id,
                MessageId = a.MessageId,
                FileName = a.FileName,
                FileType = a.FileType,
                FileUrl = a.FileUrl
            }).ToList()
        }).ToList();
    }
}
