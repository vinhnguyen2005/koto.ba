using Kotoba.Application.Interfaces;
using Kotoba.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Kotoba.Server.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IMessageService _messageService;

    public ConversationsController(IConversationService conversationService, IMessageService messageService)
    {
        _conversationService = conversationService;
        _messageService = messageService;
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<ConversationsResponse>> GetUserConversations(string userId)
    {
        var result = await _conversationService.GetUserConversationsAsync(userId);

        var response = new ConversationsResponse
        {
            Conversations = result.Conversations,
            Messages = result.Messages
        };

        return Ok(response);
    }

    [HttpPost("{conversationId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        if (conversationId == Guid.Empty)
        {
            return BadRequest("Conversation id is required.");
        }
        request.ConversationId = conversationId;
        var messageDto = await _messageService.SendMessageAsync(request);
        if (messageDto == null)
        {
            return StatusCode(500, "Failed to send message.");
        }
        return Ok(messageDto);
    }

    [HttpGet("{conversationId:guid}/messages")]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(Guid conversationId)
    {
        if (conversationId == Guid.Empty)
            return BadRequest("ConversationId is required.");

        var messages = await _messageService.GetMessagesAsync(conversationId, new PagingRequest());
        return Ok(messages);
    }

 
}
