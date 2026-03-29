using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Repositories
{
    public class ReportRepository
    {
        private readonly IDbContextFactory<KotobaDbContext> _factory;

    public ReportRepository(IDbContextFactory<KotobaDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<ReportCategoryDto>> GetActiveCategoriesAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.ReportCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ReportCategoryDto
            {
                Id          = c.Id,
                Name        = c.Name,
                Description = c.Description
            })
            .ToListAsync();
    }

    public async Task AddAsync(Report report)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.Reports.Add(report);
        await ctx.SaveChangesAsync();
    }

    public async Task<List<AdminReportListItemDto>> GetReportsForReviewAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();

        var reports = await ctx.Reports
            .AsNoTracking()
            .Include(r => r.Reporter)
            .Include(r => r.Category)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminReportListItemDto
            {
                ReportId = r.Id,
                CreatedAt = r.CreatedAt,
                ReporterId = r.ReporterId,
                ReporterDisplayName = r.Reporter.DisplayName,
                ReporterEmail = r.Reporter.Email ?? string.Empty,
                TargetType = r.TargetType,
                TargetId = r.TargetId,
                TargetPreview = string.Empty,
                TargetExists = false,
                CategoryId = r.CategoryId,
                CategoryName = r.Category.Name,
                Description = r.Description,
                Status = r.Status,
            })
            .ToListAsync();

        var messageTargets = reports
            .Where(r => r.TargetType == ReportTargetType.Message && Guid.TryParse(r.TargetId, out _))
            .Select(r => Guid.Parse(r.TargetId))
            .Distinct()
            .ToList();

        var storyTargets = reports
            .Where(r => r.TargetType == ReportTargetType.Story && Guid.TryParse(r.TargetId, out _))
            .Select(r => Guid.Parse(r.TargetId))
            .Distinct()
            .ToList();

        var thoughtTargets = reports
            .Where(r => r.TargetType == ReportTargetType.Thought && Guid.TryParse(r.TargetId, out _))
            .Select(r => Guid.Parse(r.TargetId))
            .Distinct()
            .ToList();

        var userTargets = reports
            .Where(r => r.TargetType == ReportTargetType.User && !string.IsNullOrWhiteSpace(r.TargetId))
            .Select(r => r.TargetId)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var messageLookup = await ctx.Messages
            .AsNoTracking()
            .Where(m => messageTargets.Contains(m.Id))
            .Select(m => new
            {
                m.Id,
                m.ConversationId,
                m.Content,
                m.IsDeleted,
                m.CreatedAt,
                m.SenderId,
                SenderDisplayName = m.Sender.DisplayName,
                SenderEmail = m.Sender.Email,
                SenderStatus = m.Sender.AccountStatus,
            })
            .ToDictionaryAsync(m => m.Id);

        var messageConversationIds = messageLookup
            .Select(m => m.Value.ConversationId)
            .Distinct()
            .ToList();

        var conversationLookup = await ctx.Conversations
            .AsNoTracking()
            .Where(c => messageConversationIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.Type,
                c.GroupName,
            })
            .ToDictionaryAsync(c => c.Id);

        var participantLookup = await ctx.ConversationParticipants
            .AsNoTracking()
            .Where(cp => messageConversationIds.Contains(cp.ConversationId))
            .Select(cp => new
            {
                cp.ConversationId,
                cp.UserId,
                UserDisplayName = cp.User.DisplayName,
            })
            .GroupBy(cp => cp.ConversationId)
            .ToDictionaryAsync(
                group => group.Key,
                group => group
                    .Select(cp => cp.UserDisplayName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name)
                    .ToList());

        var conversationMessagesLookup = await ctx.Messages
            .AsNoTracking()
            .Where(m => messageConversationIds.Contains(m.ConversationId) && !m.IsSystemMessage)
            .Select(m => new
            {
                m.Id,
                m.ConversationId,
                m.CreatedAt,
                m.Content,
                m.IsDeleted,
                SenderDisplayName = m.Sender.DisplayName,
            })
            .GroupBy(m => m.ConversationId)
            .ToDictionaryAsync(
                group => group.Key,
                group => group
                    .OrderBy(m => m.CreatedAt)
                    .ThenBy(m => m.Id)
                    .ToList());

        var storyLookup = await ctx.Stories
            .AsNoTracking()
            .Where(s => storyTargets.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                s.Content,
                s.UserId,
                UserDisplayName = s.User.DisplayName,
                UserEmail = s.User.Email,
                UserStatus = s.User.AccountStatus,
            })
            .ToDictionaryAsync(s => s.Id);

        var thoughtLookup = await ctx.CurrentThoughts
            .AsNoTracking()
            .Where(t => thoughtTargets.Contains(t.Id))
            .Select(t => new
            {
                t.Id,
                t.Content,
                t.UserId,
                UserDisplayName = t.User.DisplayName,
                UserEmail = t.User.Email,
                UserStatus = t.User.AccountStatus,
            })
            .ToDictionaryAsync(t => t.Id);

        var userLookup = await ctx.Users
            .AsNoTracking()
            .Where(u => userTargets.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.DisplayName,
                u.Email,
                u.AccountStatus,
            })
            .ToDictionaryAsync(u => u.Id);

        foreach (var report in reports)
        {
            switch (report.TargetType)
            {
                case ReportTargetType.Message:
                {
                    if (!Guid.TryParse(report.TargetId, out var messageId) || !messageLookup.TryGetValue(messageId, out var message))
                    {
                        report.TargetPreview = "Message not found.";
                        break;
                    }

                    report.TargetExists = true;
                    report.TargetUserId = message.SenderId;
                    report.TargetUserDisplayName = message.SenderDisplayName;
                    report.TargetUserEmail = message.SenderEmail;
                    report.TargetUserAccountStatus = message.SenderStatus;
                    report.TargetCreatedAtUtc = message.CreatedAt;
                    report.TargetPreview = message.IsDeleted
                        ? "Message has been deleted."
                        : TrimForPreview(message.Content, 180);

                    if (conversationLookup.TryGetValue(message.ConversationId, out var conversation))
                    {
                        if (conversation.Type == ConversationType.Group)
                        {
                            var groupName = string.IsNullOrWhiteSpace(conversation.GroupName)
                                ? "Unnamed Group"
                                : conversation.GroupName;
                            report.TargetConversationLabel = $"Group: {groupName}";
                        }
                        else
                        {
                            if (participantLookup.TryGetValue(message.ConversationId, out var participantNames)
                                && participantNames.Count > 0)
                            {
                                report.TargetConversationLabel = $"Direct: {string.Join(", ", participantNames)}";
                            }
                            else
                            {
                                report.TargetConversationLabel = "Direct conversation";
                            }
                        }
                    }

                    if (conversationMessagesLookup.TryGetValue(message.ConversationId, out var conversationMessages)
                        && conversationMessages.Count > 0)
                    {
                        var index = conversationMessages.FindIndex(m => m.Id == messageId);
                        if (index > 1)
                        {
                            var previousSecondary = conversationMessages[index - 2];
                            report.TargetPreviousPreviewSecondary = BuildContextPreview(previousSecondary.SenderDisplayName, previousSecondary.Content, previousSecondary.IsDeleted);
                        }

                        if (index > 0)
                        {
                            var previous = conversationMessages[index - 1];
                            report.TargetPreviousPreview = BuildContextPreview(previous.SenderDisplayName, previous.Content, previous.IsDeleted);
                        }

                        if (index >= 0 && index < conversationMessages.Count - 1)
                        {
                            var next = conversationMessages[index + 1];
                            report.TargetNextPreview = BuildContextPreview(next.SenderDisplayName, next.Content, next.IsDeleted);
                        }

                        if (index >= 0 && index < conversationMessages.Count - 2)
                        {
                            var nextSecondary = conversationMessages[index + 2];
                            report.TargetNextPreviewSecondary = BuildContextPreview(nextSecondary.SenderDisplayName, nextSecondary.Content, nextSecondary.IsDeleted);
                        }
                    }
                    break;
                }

                case ReportTargetType.Story:
                {
                    if (!Guid.TryParse(report.TargetId, out var storyId) || !storyLookup.TryGetValue(storyId, out var story))
                    {
                        report.TargetPreview = "Story not found.";
                        break;
                    }

                    report.TargetExists = true;
                    report.TargetUserId = story.UserId;
                    report.TargetUserDisplayName = story.UserDisplayName;
                    report.TargetUserEmail = story.UserEmail;
                    report.TargetUserAccountStatus = story.UserStatus;
                    report.TargetPreview = TrimForPreview(story.Content, 180);
                    break;
                }

                case ReportTargetType.Thought:
                {
                    if (!Guid.TryParse(report.TargetId, out var thoughtId) || !thoughtLookup.TryGetValue(thoughtId, out var thought))
                    {
                        report.TargetPreview = "Thought not found.";
                        break;
                    }

                    report.TargetExists = true;
                    report.TargetUserId = thought.UserId;
                    report.TargetUserDisplayName = thought.UserDisplayName;
                    report.TargetUserEmail = thought.UserEmail;
                    report.TargetUserAccountStatus = thought.UserStatus;
                    report.TargetPreview = TrimForPreview(thought.Content, 180);
                    break;
                }

                case ReportTargetType.User:
                {
                    if (!userLookup.TryGetValue(report.TargetId, out var user))
                    {
                        report.TargetPreview = "User not found.";
                        break;
                    }

                    report.TargetExists = true;
                    report.TargetUserId = user.Id;
                    report.TargetUserDisplayName = user.DisplayName;
                    report.TargetUserEmail = user.Email;
                    report.TargetUserAccountStatus = user.AccountStatus;
                    report.TargetPreview = $"Reported user profile: {user.DisplayName}";
                    break;
                }

                default:
                    report.TargetPreview = "Unsupported report target.";
                    break;
            }
        }

        return reports;
    }

    public async Task<(bool success, string error)> UpdateStatusAsync(Guid reportId, ReportStatus status, string reviewerId)
    {
        if (string.IsNullOrWhiteSpace(reviewerId))
        {
            return (false, "Unable to resolve reviewer account.");
        }

        await using var ctx = await _factory.CreateDbContextAsync();
        var report = await ctx.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
        if (report is null)
        {
            return (false, "Report not found.");
        }

        if (report.Status != ReportStatus.Pending)
        {
            return (false, "Report has already been processed.");
        }

        report.Status = status;
        report.ReviewedAt = DateTime.UtcNow;
        report.ReviewerId = reviewerId.Trim();

        await ctx.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<bool> AlreadyReportedAsync(
        string reporterId, ReportTargetType targetType, string targetId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Reports.AnyAsync(r =>
            r.ReporterId  == reporterId  &&
            r.TargetType  == targetType  &&
            r.TargetId    == targetId    &&
            r.Status      == ReportStatus.Pending);
    }

        public async Task DeleteAsync(Guid reportId)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            var report = await ctx.Reports.FindAsync(reportId);
            if (report != null)
            {
                ctx.Reports.Remove(report);
                await ctx.SaveChangesAsync();
            }
        }

        private static string TrimForPreview(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "(No content)";
            }

            var normalized = value.Trim();
            if (normalized.Length <= maxLength)
            {
                return normalized;
            }

            return normalized[..maxLength] + "...";
        }

        private static string BuildContextPreview(string? senderDisplayName, string? content, bool isDeleted)
        {
            var sender = string.IsNullOrWhiteSpace(senderDisplayName)
                ? "Unknown"
                : senderDisplayName.Trim();

            var message = isDeleted
                ? "(Message deleted)"
                : TrimForPreview(content, 120);

            return $"{sender}: {message}";
        }
    }
}
