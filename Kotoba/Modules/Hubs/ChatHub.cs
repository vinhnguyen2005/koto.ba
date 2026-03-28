using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Kotoba.Modules.Infrastructure.Services.Messages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static Kotoba.Modules.Domain.Interfaces.IMessageService;

namespace Kotoba.Modules.Hubs
{
    public class ChatHub : Hub
    {
        private readonly KotobaDbContext _context;
        private readonly IReactionService _reactionService;
        private readonly ICurrentThoughtService _thoughtService;
        private readonly IHubContext<NotificationHub> _notifHub;
        private readonly IGroupAdminService _adminService;
        private readonly IMessageService _messageService;
        private readonly INotificationService _notificationService;
        public ChatHub(KotobaDbContext context,
            IReactionService reactionService,
            ICurrentThoughtService thoughtService,
            IGroupAdminService adminService,
            IHubContext<NotificationHub> notifHub,
            IMessageService messageService,
            INotificationService notificationService)
        {
            _context = context;
            _reactionService = reactionService;
            _thoughtService = thoughtService;
            _notifHub = notifHub;
            _adminService = adminService;
            _messageService = messageService;
            _notificationService = notificationService;
        }

        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task SendMessage(string tempId, string conversationId, string senderId, string content, List<AttachmentDto> uploadedFiles)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("User not authenticated.");

            await AssertUserCanWriteAsync(userId);

            var convId = Guid.Parse(conversationId);
            var conversation = await _context.Conversations
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == convId);

            if (conversation == null)
                throw new HubException("Conversation not found.");

            var isParticipant = conversation.Participants
                .Any(p => p.UserId == userId && p.IsActive);

            if (!isParticipant)
                throw new HubException("Access denied.");

            AssertDirectRecipientCanReceive(conversation, userId);

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = convId,
                SenderId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Messages.AddAsync(message);

            var finalAttachmentDtos = new List<AttachmentDto>();
            if (uploadedFiles != null && uploadedFiles.Any())
            {
                foreach (var file in uploadedFiles)
                {
                    var attachment = new Attachment
                    {
                        Id = Guid.NewGuid(),
                        MessageId = message.Id,
                        FileName = file.FileName,
                        SavedName = Path.GetFileName(file.Url),
                        ContentType = file.ContentType,
                        Url = file.Url,
                        Size = file.Size
                    };
                    _context.Attachments.Add(attachment);
                    finalAttachmentDtos.Add(new AttachmentDto
                    {
                        Id = attachment.Id,
                        FileName = attachment.FileName,
                        ContentType = attachment.ContentType,
                        Url = attachment.Url,
                        Size = attachment.Size
                    });
                }
            }

            await _context.SaveChangesAsync();

            var dto = new MessageDto
            {
                TempId = tempId,
                MessageId = message.Id,
                SenderId = userId,
                Content = content,
                ConversationId = convId,
                CreatedAt = message.CreatedAt,
                Status = MessageStatus.Sent,
                Attachments = finalAttachmentDtos
            };

            await Clients.Group(conversationId).SendAsync("MessageConfirmed", dto, tempId);

            var participants = await _context.ConversationParticipants
                .Where(p => p.ConversationId.ToString() == conversationId && p.IsActive)
                .Select(p => p.UserId)
                .ToListAsync();

            await Clients.Users(participants).SendAsync("ConversationListChanged");

            await _notifHub.Clients.Groups(participants).SendAsync("NotifyMessage", dto);
        }

        public async Task ReactToMessage(Guid conversationId, Guid messageId, ReactionType reactionType)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            await AssertParticipantAsync(conversationId, userId);

            var reaction = await _reactionService.AddOrUpdateReactionAsync(userId, messageId, reactionType);
            if (reaction == null)
                throw new HubException("Message not found.");

            await Clients
                .Group(conversationId.ToString())
                .SendAsync("ReactionUpdated", reaction);
        }

        public async Task RemoveReaction(Guid conversationId, Guid messageId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            await AssertParticipantAsync(conversationId, userId);

            var removed = await _reactionService.RemoveReactionAsync(userId, messageId);
            if (!removed)
                throw new HubException("Message not found or no reaction to remove.");

            await Clients
                .Group(conversationId.ToString())
                .SendAsync("ReactionRemoved", new { messageId, userId });
        }

        public async Task SendTyping(Guid conversationId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            await Clients
                .OthersInGroup(conversationId.ToString())
                .SendAsync("UserTyping", new TypingStatusDto
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    IsTyping = true
                });
        }

        public async Task StopTyping(Guid conversationId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            await Clients
                .OthersInGroup(conversationId.ToString())
                .SendAsync("UserTyping", new TypingStatusDto
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    IsTyping = false
                });
        }
        public async Task UpdateThought(string content)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            await _thoughtService.SetThoughtAsync(userId, content);
            await Clients.All.SendAsync("ThoughtUpdated", new { userId, content });
        }

        private async Task AssertParticipantAsync(Guid conversationId, string userId)
        {
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(p => p.ConversationId == conversationId
                            && p.UserId == userId
                            && p.IsActive);

            if (!isParticipant)
                throw new HubException("Access denied.");
        }

        private async Task AssertUserCanWriteAsync(string userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new HubException("User not found.");

            if (user.AccountStatus == AccountStatus.Deleted)
                throw new HubException("Account is deleted.");

            if (user.AccountStatus == AccountStatus.Deactivated)
                throw new HubException("Account is deactivated.");
        }
        public async Task NotifyGroupCreated(List<string> participantIds)
        {
            foreach (var userId in participantIds)
            {
                await Clients.User(userId).SendAsync("ConversationListChanged");
            }
        }
        public async Task KickMember(string conversationId, string targetUserId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            var convId = Guid.Parse(conversationId);

            var conversation = await _context.Conversations.FindAsync(convId);

            if (conversation?.Type != ConversationType.Group)
                throw new HubException("Can only kick members from groups.");
            var isOwnerOrAdmin = await _adminService.IsOwnerOrAdminAsync(convId, userId);
            if (!isOwnerOrAdmin)
                throw new HubException("Only owner/admin can kick members.");

            var targetRole = await _adminService.GetUserRoleAsync(convId, targetUserId);
            if (targetRole == GroupRole.Owner)
                throw new HubException("Cannot kick owner.");

            if (userId == targetUserId)
                throw new HubException("Cannot kick yourself.");

            var targetUser = await _context.Users.FindAsync(targetUserId);
            var displayName = targetUser?.DisplayName ?? "User";

            var systemMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = convId,
                SenderId = userId,
                Content = $"{displayName} has been removed",
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.MemberRemoved,
                SystemMessageData = System.Text.Json.JsonSerializer.Serialize(
                    new SystemMessageDataDto { UserId = targetUserId, DisplayName = displayName }
                )
            };
            await _context.Messages.AddAsync(systemMsg);
            await _adminService.RemoveMemberAsync(convId, targetUserId);

            await _context.SaveChangesAsync();

            await Clients.User(targetUserId).SendAsync("RemovedFromGroup", conversationId);
            await Clients.Group(conversationId).SendAsync("MessageConfirmed", MapSystemMessageToDto(systemMsg), systemMsg.Id.ToString());
            await Clients.Group(conversationId).SendAsync("ConversationListChanged");
            await Clients.Group(conversationId).SendAsync("MembersUpdated");
        }

        public async Task LeaveGroup(string conversationId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            var convId = Guid.Parse(conversationId);

            var userRole = await _adminService.GetUserRoleAsync(convId, userId);
            var user = await _context.Users.FindAsync(userId);
            var displayName = user?.DisplayName ?? "User";

            var systemMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = convId,
                SenderId = userId,
                Content = userRole == GroupRole.Owner
                    ? $"Owner {displayName} left the group"
                    : $"{displayName} left the group",
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = true,
                SystemMessageType = userRole == GroupRole.Owner ? SystemMessageType.OwnerLeft : SystemMessageType.UserLeft,
                SystemMessageData = System.Text.Json.JsonSerializer.Serialize(
                    new SystemMessageDataDto { UserId = userId, DisplayName = displayName }
                )
            };
            await _context.Messages.AddAsync(systemMsg);
            await _adminService.LeaveConversationAsync(convId, userId);
            string? newOwnerId = null;
            if (userRole == GroupRole.Owner)
            {
                newOwnerId = await _adminService.AutoTransferOwnershipOnLeaveAsync(convId);
                if (newOwnerId != null)
                {
                    var newOwner = await _context.Users.FindAsync(newOwnerId);
                    systemMsg.Content = $"Ownership transferred to {newOwner?.DisplayName}";
                }
            }

            await _context.SaveChangesAsync();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);

            await Clients.OthersInGroup(conversationId).SendAsync("MessageConfirmed", MapSystemMessageToDto(systemMsg), systemMsg.Id.ToString());

            if (newOwnerId != null)
            {
                await Clients.Group(conversationId).SendAsync("OwnershipTransferred", newOwnerId);
            }

            await Clients.Group(conversationId).SendAsync("ConversationListChanged");
            await Clients.OthersInGroup(conversationId).SendAsync("MembersUpdated");
        }

        public async Task NotifyMemberAdded(string conversationId, string addedUserId, string addedUserName)
        {
            var convId = Guid.Parse(conversationId);

            var systemMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = convId,
                SenderId = addedUserId,
                Content = $"{addedUserName} is added to the group",
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.MemberAdded,

                SystemMessageData = System.Text.Json.JsonSerializer.Serialize(
                    new SystemMessageDataDto { UserId = addedUserId, DisplayName = addedUserName }
                )
            };
            await _context.Messages.AddAsync(systemMsg);
            await _context.SaveChangesAsync();

            var sysMsgDto = new MessageDto
            {
                MessageId = systemMsg.Id,
                SenderId = addedUserId,
                Content = systemMsg.Content,
                ConversationId = convId,
                CreatedAt = systemMsg.CreatedAt,
                Status = MessageStatus.Sent,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.MemberAdded,
                SystemMessageData = new SystemMessageDataDto { UserId = addedUserId, DisplayName = addedUserName }
            };

            await Clients.Group(conversationId).SendAsync("MessageConfirmed", sysMsgDto, systemMsg.Id.ToString());
            await Clients.User(addedUserId).SendAsync("ConversationListChanged");
            await Clients.Group(conversationId).SendAsync("ConversationListChanged");
            await Clients.Group(conversationId).SendAsync("MembersUpdated");
        }

        public async Task PromoteToAdmin(Guid conversationId, string targetUserId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            var isOwner = await _adminService.IsOwnerAsync(conversationId, userId);
            if (!isOwner)
                throw new HubException("Only owner can promote members.");

            var success = await _adminService.PromoteToAdminAsync(conversationId, targetUserId);
            if (!success)
                throw new HubException("Promote failed - user not found or already admin.");

            var targetUser = await _context.Users.FindAsync(targetUserId);
            var systemMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = userId,
                Content = $"{targetUser?.DisplayName} is now an admin",
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.MemberPromoted,
                SystemMessageData = System.Text.Json.JsonSerializer.Serialize(
                    new SystemMessageDataDto { UserId = targetUserId, DisplayName = targetUser?.DisplayName }
                )
            };
            await _context.Messages.AddAsync(systemMsg);
            await _context.SaveChangesAsync();

            await Clients.Group(conversationId.ToString())
                .SendAsync("MessageConfirmed", MapSystemMessageToDto(systemMsg), systemMsg.Id.ToString());
            await Clients.Group(conversationId.ToString())
                .SendAsync("MembersUpdated");
        }
        public async Task DemoteFromAdmin(Guid conversationId, string targetUserId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            var isOwner = await _adminService.IsOwnerAsync(conversationId, userId);
            if (!isOwner)
                throw new HubException("Only owner can demote admins.");

            var success = await _adminService.DemoteFromAdminAsync(conversationId, targetUserId);
            if (!success)
                throw new HubException("Demote failed - user not found or not admin.");

            var targetUser = await _context.Users.FindAsync(targetUserId);
            var systemMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = userId,
                Content = $"{targetUser?.DisplayName} is no longer an admin",
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.MemberDemoted,
                SystemMessageData = System.Text.Json.JsonSerializer.Serialize(
                    new SystemMessageDataDto { UserId = targetUserId, DisplayName = targetUser?.DisplayName }
                )
            };
            await _context.Messages.AddAsync(systemMsg);
            await _context.SaveChangesAsync();

            await Clients.Group(conversationId.ToString())
                .SendAsync("MessageConfirmed", MapSystemMessageToDto(systemMsg), systemMsg.Id.ToString());
            await Clients.Group(conversationId.ToString())
                .SendAsync("MembersUpdated");
        }

        public async Task TransferOwnership(Guid conversationId, string newOwnerId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);

            var isOwner = await _adminService.IsOwnerAsync(conversationId, userId);
            if (!isOwner)
                throw new HubException("Only owner can transfer ownership.");

            var success = await _adminService.TransferOwnershipAsync(conversationId, userId, newOwnerId);
            if (!success)
                throw new HubException("Transfer failed.");

            var newOwnerUser = await _context.Users.FindAsync(newOwnerId);
            var systemMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = userId,
                Content = $"Ownership transferred to {newOwnerUser?.DisplayName}",
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.OwnershipTransferred,
                SystemMessageData = System.Text.Json.JsonSerializer.Serialize(
                    new SystemMessageDataDto { UserId = newOwnerId, DisplayName = newOwnerUser?.DisplayName }
                )
            };
            await _context.Messages.AddAsync(systemMsg);
            await _context.SaveChangesAsync();

            await Clients.Group(conversationId.ToString())
                .SendAsync("MessageConfirmed", MapSystemMessageToDto(systemMsg), systemMsg.Id.ToString());
            await Clients.Group(conversationId.ToString())
                .SendAsync("MembersUpdated");
        }

        public async Task EditGroupName(Guid conversationId, string newName)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);
            var isOwnerOrAdmin = await _adminService.IsOwnerOrAdminAsync(conversationId, userId);
            if (!isOwnerOrAdmin)
                throw new HubException("Only owner/admin can edit group name.");

            var success = await _adminService.UpdateGroupNameAsync(conversationId, newName);
            if (!success)
                throw new HubException("Update failed.");
            var updatedConv = await _context.Conversations.FindAsync(conversationId);

            var systemMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = userId,
                Content = $"Group name changed to '{newName}'",
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.GroupNameChanged,
                SystemMessageData = System.Text.Json.JsonSerializer.Serialize(
                    new { GroupName = newName }
                )
            };
            await _context.Messages.AddAsync(systemMsg);
            await _context.SaveChangesAsync();

            await Clients.Group(conversationId.ToString())
                .SendAsync("MessageConfirmed", MapSystemMessageToDto(systemMsg), systemMsg.Id.ToString());
            await Clients.Group(conversationId.ToString())
                .SendAsync("GroupNameUpdated", newName);
            var participantIds = await _context.ConversationParticipants
    .Where(p => p.ConversationId == conversationId && p.IsActive)
    .Select(p => p.UserId)
    .ToListAsync();

            await Clients.Users(participantIds).SendAsync("ConversationListChanged");
        }

        public async Task AddMember(Guid conversationId, string targetUserId)
        {
            var userId = Context.UserIdentifier!;
            await AssertUserCanWriteAsync(userId);

            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation?.Type != ConversationType.Group)
                throw new HubException("Can only add members to groups.");
            var requestorRole = await _adminService.GetUserRoleAsync(conversationId, userId);
            if (requestorRole != GroupRole.Owner && requestorRole != GroupRole.Admin)
                throw new HubException("Only owner/admin can add members.");
            var targetUser = await _context.Users.FindAsync(targetUserId);
            if (targetUser == null || targetUser.AccountStatus != AccountStatus.Active)
                throw new HubException("User not found or inactive.");

            var existing = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == targetUserId);

            if (existing?.IsActive == true)
                throw new HubException("User already in group.");
            var success = await _adminService.AddMemberAsync(conversationId, targetUserId);
            if (!success)
                throw new HubException("Add member failed.");

            var displayName = targetUser.DisplayName ?? "User";
            var systemMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = userId,
                Content = $"{displayName} is added to the group",
                CreatedAt = DateTime.UtcNow,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.MemberAdded,
                SystemMessageData = System.Text.Json.JsonSerializer.Serialize(
                    new SystemMessageDataDto { UserId = targetUserId, DisplayName = displayName }
                )
            };
            await _context.Messages.AddAsync(systemMsg);
            await _context.SaveChangesAsync();

            var sysMsgDto = new MessageDto
            {
                MessageId = systemMsg.Id,
                SenderId = userId,
                Content = systemMsg.Content,
                ConversationId = conversationId,
                CreatedAt = systemMsg.CreatedAt,
                Status = MessageStatus.Sent,
                IsSystemMessage = true,
                SystemMessageType = SystemMessageType.MemberAdded,
                SystemMessageData = new SystemMessageDataDto { UserId = targetUserId, DisplayName = displayName }
            };

            await Clients.Group(conversationId.ToString())
                .SendAsync("MessageConfirmed", sysMsgDto, systemMsg.Id.ToString());

            await Clients.User(targetUserId).SendAsync("ConversationListChanged");
            await Clients.Group(conversationId.ToString())
                .SendAsync("MembersUpdated");
        }



        // Revoke message
        public async Task RevokeMessage(Guid conversationId, Guid messageId)
        {
            var userId = Context.UserIdentifier;
            var message = await _messageService.GetMessageByIdAsync(messageId);

            if (message == null || message.SenderId != userId) return;

            await _messageService.RevokeMessageAsync(messageId);

            await Clients.Group(conversationId.ToString())
                .SendAsync("MessageRevoked", messageId);
        }

        // Reply to message
        public async Task SendReplyMessage(string tempId, string conversationId,
            string senderId, string content, Guid replyToMessageId,
            List<AttachmentDto> attachments)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("User not authenticated.");

            await AssertUserCanWriteAsync(userId);

            var convId = Guid.Parse(conversationId);
            var conversation = await _context.Conversations
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == convId);

            if (conversation == null)
                throw new HubException("Conversation not found.");

            var isParticipant = conversation.Participants
                .Any(p => p.UserId == userId && p.IsActive);

            if (!isParticipant)
                throw new HubException("Access denied.");

            AssertDirectRecipientCanReceive(conversation, userId);

            var dto = await _messageService.SendReplyAsync(new SendReplyRequest
            {
                TempId = tempId,
                ConversationId = convId,
                SenderId = userId,
                Content = content,
                ReplyToMessageId = replyToMessageId,
                Attachments = attachments
            });

            await Clients.Group(conversationId)
                .SendAsync("MessageConfirmed", dto, tempId);

            await Clients.Group(conversationId)
                .SendAsync("ConversationListChanged");
        }

        private static void AssertDirectRecipientCanReceive(Conversation conversation, string senderUserId)
        {
            if (conversation.Type != ConversationType.Direct)
                return;

            var other = conversation.Participants
                .FirstOrDefault(p => p.UserId != senderUserId && p.IsActive);

            if (other?.User == null)
                throw new HubException("Recipient not found.");

            if (other.User.AccountStatus == AccountStatus.Deleted)
                throw new HubException("Cannot send messages to a deleted account.");

            if (other.User.AccountStatus == AccountStatus.Deactivated)
                throw new HubException("Cannot send messages to a deactivated account.");
        }

        private MessageDto MapSystemMessageToDto(Message systemMsg)
        {
            return new MessageDto
            {
                MessageId = systemMsg.Id,
                SenderId = systemMsg.SenderId,
                Content = systemMsg.Content,
                ConversationId = systemMsg.ConversationId,
                CreatedAt = systemMsg.CreatedAt,
                Status = MessageStatus.Sent,
                IsSystemMessage = true,
                SystemMessageType = systemMsg.SystemMessageType,
                SystemMessageData = string.IsNullOrEmpty(systemMsg.SystemMessageData)
                    ? null
                    : System.Text.Json.JsonSerializer.Deserialize<SystemMessageDataDto>(systemMsg.SystemMessageData)
            };
        }

        // Gọi từ bất kỳ service nào cần push notification
        public async Task PushNotification(string recipientId, NotificationDto dto)
        {
            await Clients.User(recipientId).SendAsync("ReceiveNotification", dto);
        }

        // Helper dùng nội bộ trong các Hub method khác
        private async Task NotifyAsync(CreateNotificationRequest request)
        {
            var dto = await _notificationService.CreateAsync(request);
            await Clients.User(request.RecipientId)
                .SendAsync("ReceiveNotification", dto);
        }
    }
}
