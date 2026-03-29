using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Domain.Constants;
using Kotoba.Modules.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace Kotoba.Modules.Infrastructure.Services.Identity
{
    public class UserService : IUserService
    {
        private const string ProfileTokenProvider = "Kotoba.Profile";
        private const string ProfileBioTokenName = "Bio";
        private const string BannedDisplayTag = " [Banned]";

        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAdminAuditService _adminAuditService;
        private readonly KotobaDbContext _dbContext;
        private readonly UserProfileRepository _userProfileRepository;
        private readonly SemaphoreSlim _userProfileLock = new(1, 1);

        public UserService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager,
            IAdminAuditService adminAuditService,
            KotobaDbContext dbContext,
            UserProfileRepository userProfileRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _adminAuditService = adminAuditService;
            _dbContext = dbContext;
            _userProfileRepository = userProfileRepository;
        }

        public Task<UserProfile?> GetUserProfileAsync(string userId)
        {
            return GetUserProfileInternalAsync(userId);
        }

        public List<UserProfile> GetUsersByDisplayNameAsync(string searchValue)
        {
            return _userProfileRepository.GetUsersByDisplayNameAsync(searchValue);
        }

        public async Task<bool> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return false;
            }

            var normalizedEmail = request.Email.Trim();
            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            if (user is null)
            {
                return false;
            }

            if (user.AccountStatus == AccountStatus.Deleted)
            {
                // Deleted accounts cannot log in again.
                return false;
            }

            if (user.AccountStatus == AccountStatus.Banned)
            {
                return false;
            }

            // Admin accounts must authenticate through the admin portal only.
            if (await IsAnyAdminAsync(user))
            {
                return false;
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: false);

            return result.Succeeded;
        }

        public async Task<string?> LoginAdminAsync(
            LoginRequest request,
            string? sourceIp = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return null;
            }

            var normalizedEmail = request.Email.Trim();
            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            if (user is null)
            {
                return null;
            }

            if (user.AccountStatus == AccountStatus.Deleted
                || user.AccountStatus == AccountStatus.Deactivated
                || user.AccountStatus == AccountStatus.Banned)
            {
                await TraceAdminLoginAsync(
                    user.Id,
                    isSuccess: false,
                    summary: "Failed admin login",
                    metadata: $"Reason: Account status is {user.AccountStatus}",
                    sourceIp: sourceIp,
                    correlationId: correlationId,
                    cancellationToken: cancellationToken);
                return null;
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                await TraceAdminLoginAsync(
                    user.Id,
                    isSuccess: false,
                    summary: "Failed admin login",
                    metadata: "Reason: Invalid credentials",
                    sourceIp: sourceIp,
                    correlationId: correlationId,
                    cancellationToken: cancellationToken);
                return null;
            }

            var isSystemAdmin = await _userManager.IsInRoleAsync(user, AdminRoles.SystemAdmin);
            var isBusinessAdmin = await _userManager.IsInRoleAsync(user, AdminRoles.BusinessAdmin);
            if (isSystemAdmin)
            {
                await TraceAdminLoginAsync(
                    user.Id,
                    isSuccess: true,
                    summary: "Admin login succeeded (system admin)",
                    metadata: "Role: SystemAdmin",
                    sourceIp: sourceIp,
                    correlationId: correlationId,
                    cancellationToken: cancellationToken);
                return "/admin/system/dashboard";
            }

            if (isBusinessAdmin)
            {
                await TraceAdminLoginAsync(
                    user.Id,
                    isSuccess: true,
                    summary: "Admin login succeeded (business admin)",
                    metadata: "Role: BusinessAdmin",
                    sourceIp: sourceIp,
                    correlationId: correlationId,
                    cancellationToken: cancellationToken);
                return "/admin/business/dashboard";
            }

            await _signInManager.SignOutAsync();

            await TraceAdminLoginAsync(
                user.Id,
                isSuccess: false,
                summary: "Failed admin login",
                metadata: "Reason: Account has no admin role",
                sourceIp: sourceIp,
                correlationId: correlationId,
                cancellationToken: cancellationToken);

            return null;
        }

        public async Task<string?> GetLatestBanReasonByEmailAsync(string? email, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var normalizedEmail = email.Trim();
            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (user is null)
            {
                return null;
            }

            var metadata = await _dbContext.AdminAuditLogs
                .AsNoTracking()
                .Where(a => a.TargetEntityId == user.Id
                    && a.ActionType == AdminActionType.UserBanned
                    && a.IsSuccess)
                .OrderByDescending(a => a.TimestampUtc)
                .Select(a => a.MetadataJson)
                .FirstOrDefaultAsync(cancellationToken);

            return ExtractReasonFromMetadata(metadata);
        }

        public async Task<AccountStatus?> GetAccountStatusByEmailAsync(string? email, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var normalizedEmail = email.Trim();
            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            return user?.AccountStatus;
        }

        public async Task<AdminCreationResult> CreateBusinessAdminAsync(
            CreateBusinessAdminRequest request,
            string performedByAdminId,
            string? sourceIp = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(performedByAdminId))
            {
                return AdminCreationResult.Failure(new[] { "Unable to resolve acting admin account." });
            }

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                errors.Add("Display name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                errors.Add("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                errors.Add("Password is required.");
            }

            if (errors.Count > 0)
            {
                var validationFailure = AdminCreationResult.Failure(errors);
                await TraceAdminCreateAsync(
                    performedByAdminId,
                    request.Email,
                    validationFailure,
                    sourceIp,
                    correlationId,
                    cancellationToken);
                return validationFailure;
            }

            var normalizedEmail = request.Email.Trim();
            var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
            if (existingUser is not null)
            {
                var duplicateFailure = AdminCreationResult.Failure(new[] { "Email is already in use." });
                await TraceAdminCreateAsync(
                    performedByAdminId,
                    normalizedEmail,
                    duplicateFailure,
                    sourceIp,
                    correlationId,
                    cancellationToken);
                return duplicateFailure;
            }

            try
            {
                var user = new User
                {
                    UserName = normalizedEmail,
                    Email = normalizedEmail,
                    DisplayName = request.DisplayName.Trim(),
                    AccountStatus = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    IsOnline = false,
                };

                var createResult = await _userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    var createFailure = AdminCreationResult.Failure(createResult.Errors.Select(error => error.Description));
                    await TraceAdminCreateAsync(
                        performedByAdminId,
                        normalizedEmail,
                        createFailure,
                        sourceIp,
                        correlationId,
                        cancellationToken);
                    return createFailure;
                }

                if (!await _roleManager.RoleExistsAsync(AdminRoles.BusinessAdmin))
                {
                    var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(AdminRoles.BusinessAdmin));
                    if (!createRoleResult.Succeeded)
                    {
                        await _userManager.DeleteAsync(user);
                        var roleFailure = AdminCreationResult.Failure(createRoleResult.Errors.Select(error => error.Description));
                        await TraceAdminCreateAsync(
                            performedByAdminId,
                            normalizedEmail,
                            roleFailure,
                            sourceIp,
                            correlationId,
                            cancellationToken);
                        return roleFailure;
                    }
                }

                var addRoleResult = await _userManager.AddToRoleAsync(user, AdminRoles.BusinessAdmin);
                if (!addRoleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    var addRoleFailure = AdminCreationResult.Failure(addRoleResult.Errors.Select(error => error.Description));
                    await TraceAdminCreateAsync(
                        performedByAdminId,
                        normalizedEmail,
                        addRoleFailure,
                        sourceIp,
                        correlationId,
                        cancellationToken);
                    return addRoleFailure;
                }

                var success = AdminCreationResult.Success(user.Id);
                await TraceAdminCreateAsync(
                    performedByAdminId,
                    normalizedEmail,
                    success,
                    sourceIp,
                    correlationId,
                    cancellationToken);
                return success;
            }
            catch (Exception ex)
            {
                var exceptionFailure = AdminCreationResult.Failure(new[] { "Unexpected error while creating admin account." });
                await TraceAdminCreateAsync(
                    performedByAdminId,
                    normalizedEmail,
                    exceptionFailure,
                    sourceIp,
                    correlationId,
                    cancellationToken,
                    ex.Message);
                return exceptionFailure;
            }
        }

        public async Task<IReadOnlyList<BusinessAdminListItem>> GetBusinessAdminsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var users = await _userManager.Users
                .AsNoTracking()
                .Where(user => user.AccountStatus != AccountStatus.Deleted)
                .OrderByDescending(user => user.CreatedAt)
                .ToListAsync(cancellationToken);

            var list = new List<BusinessAdminListItem>();
            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, AdminRoles.BusinessAdmin))
                {
                    list.Add(new BusinessAdminListItem
                    {
                        UserId = user.Id,
                        DisplayName = user.DisplayName,
                        Email = user.Email ?? string.Empty,
                        AccountStatus = user.AccountStatus,
                        CreatedAt = user.CreatedAt,
                    });
                }
            }

            return list;
        }

        public async Task<IReadOnlyList<NormalUserListItem>> GetNormalUsersAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var users = await _userManager.Users
                .AsNoTracking()
                .Where(user => user.AccountStatus != AccountStatus.Deleted)
                .OrderByDescending(user => user.CreatedAt)
                .ToListAsync(cancellationToken);

            var systemAdmins = await _userManager.GetUsersInRoleAsync(AdminRoles.SystemAdmin);
            var businessAdmins = await _userManager.GetUsersInRoleAsync(AdminRoles.BusinessAdmin);

            var adminIds = systemAdmins
                .Concat(businessAdmins)
                .Select(admin => admin.Id)
                .ToHashSet(StringComparer.Ordinal);

            var list = users
                .Where(user => !adminIds.Contains(user.Id))
                .Select(user => new NormalUserListItem
                {
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    Email = user.Email ?? string.Empty,
                    AccountStatus = user.AccountStatus,
                    IsOnline = user.IsOnline,
                    CreatedAt = user.CreatedAt,
                    LastSeenAt = user.LastSeenAt,
                })
                .ToList();

            return list;
        }

        public async Task<AccountOperationResult> DeactivateUserByAdminAsync(
            string userId,
            string performedByAdminId,
            string? reason = null,
            string? sourceIp = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedReason = NormalizeModerationReason(reason);

            if (string.IsNullOrWhiteSpace(performedByAdminId))
            {
                var actorFailure = AccountOperationResult.Failure(new[] { "Unable to resolve acting admin account." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserDeactivated, actorFailure, sourceIp, correlationId, cancellationToken);
                return actorFailure;
            }

            if (string.IsNullOrWhiteSpace(normalizedReason))
            {
                var reasonFailure = AccountOperationResult.Failure(new[] { "A reason is required to deactivate a user." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserDeactivated, reasonFailure, sourceIp, correlationId, cancellationToken);
                return reasonFailure;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                var idFailure = AccountOperationResult.Failure(new[] { "User ID is required." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserDeactivated, idFailure, sourceIp, correlationId, cancellationToken);
                return idFailure;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                var notFound = AccountOperationResult.Failure(new[] { "User profile not found." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserDeactivated, notFound, sourceIp, correlationId, cancellationToken);
                return notFound;
            }

            if (await IsAnyAdminAsync(user))
            {
                var adminTargetFailure = AccountOperationResult.Failure(new[] { "This action is only available for normal users." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserDeactivated, adminTargetFailure, sourceIp, correlationId, cancellationToken);
                return adminTargetFailure;
            }

            if (user.AccountStatus == AccountStatus.Banned)
            {
                var bannedFailure = AccountOperationResult.Failure(new[] { "Banned users cannot be deactivated." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserDeactivated, bannedFailure, sourceIp, correlationId, cancellationToken);
                return bannedFailure;
            }

            var result = await DeactivateAccountAsync(userId);
            await TraceUserAccountActionAsync(
                performedByAdminId,
                userId,
                AdminActionType.UserDeactivated,
                result,
                sourceIp,
                correlationId,
                cancellationToken,
                $"Reason: {normalizedReason}");
            return result;
        }

        public async Task<AccountOperationResult> ReactivateUserByAdminAsync(
            string userId,
            string performedByAdminId,
            string? reason = null,
            string? sourceIp = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedReason = NormalizeModerationReason(reason);

            if (string.IsNullOrWhiteSpace(performedByAdminId))
            {
                var actorFailure = AccountOperationResult.Failure(new[] { "Unable to resolve acting admin account." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserReactivated, actorFailure, sourceIp, correlationId, cancellationToken);
                return actorFailure;
            }

            if (string.IsNullOrWhiteSpace(normalizedReason))
            {
                var reasonFailure = AccountOperationResult.Failure(new[] { "A reason is required to reactivate a user." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserReactivated, reasonFailure, sourceIp, correlationId, cancellationToken);
                return reasonFailure;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                var idFailure = AccountOperationResult.Failure(new[] { "User ID is required." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserReactivated, idFailure, sourceIp, correlationId, cancellationToken);
                return idFailure;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                var notFound = AccountOperationResult.Failure(new[] { "User profile not found." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserReactivated, notFound, sourceIp, correlationId, cancellationToken);
                return notFound;
            }

            if (await IsAnyAdminAsync(user))
            {
                var adminTargetFailure = AccountOperationResult.Failure(new[] { "This action is only available for normal users." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserReactivated, adminTargetFailure, sourceIp, correlationId, cancellationToken);
                return adminTargetFailure;
            }

            if (user.AccountStatus == AccountStatus.Banned)
            {
                var bannedFailure = AccountOperationResult.Failure(new[] { "Banned users must be unbanned before reactivation." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserReactivated, bannedFailure, sourceIp, correlationId, cancellationToken);
                return bannedFailure;
            }

            var result = await ReactivateAccountAsync(userId);
            await TraceUserAccountActionAsync(
                performedByAdminId,
                userId,
                AdminActionType.UserReactivated,
                result,
                sourceIp,
                correlationId,
                cancellationToken,
                $"Reason: {normalizedReason}");
            return result;
        }

        public async Task<AccountOperationResult> BanUserByAdminAsync(
            string userId,
            string performedByAdminId,
            string? reason = null,
            string? sourceIp = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedReason = NormalizeModerationReason(reason);

            if (string.IsNullOrWhiteSpace(performedByAdminId))
            {
                var actorFailure = AccountOperationResult.Failure(new[] { "Unable to resolve acting admin account." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserBanned, actorFailure, sourceIp, correlationId, cancellationToken);
                return actorFailure;
            }

            if (string.IsNullOrWhiteSpace(normalizedReason))
            {
                var reasonFailure = AccountOperationResult.Failure(new[] { "A reason is required to ban a user." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserBanned, reasonFailure, sourceIp, correlationId, cancellationToken);
                return reasonFailure;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                var idFailure = AccountOperationResult.Failure(new[] { "User ID is required." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserBanned, idFailure, sourceIp, correlationId, cancellationToken);
                return idFailure;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                var notFound = AccountOperationResult.Failure(new[] { "User profile not found." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserBanned, notFound, sourceIp, correlationId, cancellationToken);
                return notFound;
            }

            if (await IsAnyAdminAsync(user))
            {
                var adminTargetFailure = AccountOperationResult.Failure(new[] { "This action is only available for normal users." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserBanned, adminTargetFailure, sourceIp, correlationId, cancellationToken);
                return adminTargetFailure;
            }

            if (user.AccountStatus == AccountStatus.Deleted)
            {
                var deletedFailure = AccountOperationResult.Failure(new[] { "Deleted accounts cannot be banned." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserBanned, deletedFailure, sourceIp, correlationId, cancellationToken);
                return deletedFailure;
            }

            if (user.AccountStatus == AccountStatus.Banned)
            {
                var alreadyFailure = AccountOperationResult.Failure(new[] { "User is already banned." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserBanned, alreadyFailure, sourceIp, correlationId, cancellationToken);
                return alreadyFailure;
            }

            user.IsOnline = false;
            user.LastSeenAt = DateTime.UtcNow;
            user.AccountStatus = AccountStatus.Banned;
            user.DeactivatedAt = DateTime.UtcNow;
            user.DisplayName = EnsureBannedTag(user.DisplayName);

            var updateResult = await _userManager.UpdateAsync(user);
            var result = updateResult.Succeeded
                ? AccountOperationResult.Success()
                : FromIdentityResult(updateResult);

            await TraceUserAccountActionAsync(
                performedByAdminId,
                userId,
                AdminActionType.UserBanned,
                result,
                sourceIp,
                correlationId,
                cancellationToken,
                $"Reason: {normalizedReason}");
            return result;
        }

        public async Task<AccountOperationResult> UnbanUserByAdminAsync(
            string userId,
            string performedByAdminId,
            string? reason = null,
            string? sourceIp = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedReason = NormalizeModerationReason(reason);

            if (string.IsNullOrWhiteSpace(performedByAdminId))
            {
                var actorFailure = AccountOperationResult.Failure(new[] { "Unable to resolve acting admin account." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserUnbanned, actorFailure, sourceIp, correlationId, cancellationToken);
                return actorFailure;
            }

            if (string.IsNullOrWhiteSpace(normalizedReason))
            {
                var reasonFailure = AccountOperationResult.Failure(new[] { "A reason is required to unban a user." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserUnbanned, reasonFailure, sourceIp, correlationId, cancellationToken);
                return reasonFailure;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                var idFailure = AccountOperationResult.Failure(new[] { "User ID is required." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserUnbanned, idFailure, sourceIp, correlationId, cancellationToken);
                return idFailure;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                var notFound = AccountOperationResult.Failure(new[] { "User profile not found." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserUnbanned, notFound, sourceIp, correlationId, cancellationToken);
                return notFound;
            }

            if (await IsAnyAdminAsync(user))
            {
                var adminTargetFailure = AccountOperationResult.Failure(new[] { "This action is only available for normal users." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserUnbanned, adminTargetFailure, sourceIp, correlationId, cancellationToken);
                return adminTargetFailure;
            }

            if (user.AccountStatus != AccountStatus.Banned)
            {
                var notBannedFailure = AccountOperationResult.Failure(new[] { "User is not banned." });
                await TraceUserAccountActionAsync(performedByAdminId, userId, AdminActionType.UserUnbanned, notBannedFailure, sourceIp, correlationId, cancellationToken);
                return notBannedFailure;
            }

            user.AccountStatus = AccountStatus.Active;
            user.DeactivatedAt = null;
            user.DisplayName = RemoveBannedTag(user.DisplayName);

            var updateResult = await _userManager.UpdateAsync(user);
            var result = updateResult.Succeeded
                ? AccountOperationResult.Success()
                : FromIdentityResult(updateResult);

            await TraceUserAccountActionAsync(
                performedByAdminId,
                userId,
                AdminActionType.UserUnbanned,
                result,
                sourceIp,
                correlationId,
                cancellationToken,
                $"Reason: {normalizedReason}");
            return result;
        }

        public async Task<AccountOperationResult> DisableAdminAsync(
            string adminIdToDisable,
            string performedByAdminId,
            string? sourceIp = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(performedByAdminId))
            {
                var noActorFailure = AccountOperationResult.Failure(new[] { "Unable to resolve acting admin account." });
                await TraceAdminDisableAsync(
                    performedByAdminId,
                    adminIdToDisable,
                    noActorFailure,
                    sourceIp,
                    correlationId,
                    cancellationToken);
                return noActorFailure;
            }

            if (string.IsNullOrWhiteSpace(adminIdToDisable))
            {
                return AccountOperationResult.Failure(new[] { "Admin ID is required." });
            }

            if (performedByAdminId == adminIdToDisable)
            {
                var selfDisableFailure = AccountOperationResult.Failure(new[] { "Cannot disable your own account." });
                await TraceAdminDisableAsync(
                    performedByAdminId,
                    adminIdToDisable,
                    selfDisableFailure,
                    sourceIp,
                    correlationId,
                    cancellationToken);
                return selfDisableFailure;
            }

            var user = await _userManager.FindByIdAsync(adminIdToDisable);
            if (user is null)
            {
                var notFoundFailure = AccountOperationResult.Failure(new[] { "Admin account not found." });
                await TraceAdminDisableAsync(
                    performedByAdminId,
                    adminIdToDisable,
                    notFoundFailure,
                    sourceIp,
                    correlationId,
                    cancellationToken);
                return notFoundFailure;
            }

            if (user.AccountStatus == AccountStatus.Deactivated)
            {
                var alreadyDisabledFailure = AccountOperationResult.Failure(new[] { "Admin account is already disabled." });
                await TraceAdminDisableAsync(
                    performedByAdminId,
                    adminIdToDisable,
                    alreadyDisabledFailure,
                    sourceIp,
                    correlationId,
                    cancellationToken);
                return alreadyDisabledFailure;
            }

            try
            {
                user.AccountStatus = AccountStatus.Deactivated;
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    var updateFailure = AccountOperationResult.Failure(updateResult.Errors.Select(error => error.Description));
                    await TraceAdminDisableAsync(
                        performedByAdminId,
                        adminIdToDisable,
                        updateFailure,
                        sourceIp,
                        correlationId,
                        cancellationToken);
                    return updateFailure;
                }

                var success = AccountOperationResult.Success();
                await TraceAdminDisableAsync(
                    performedByAdminId,
                    adminIdToDisable,
                    success,
                    sourceIp,
                    correlationId,
                    cancellationToken);
                return success;
            }
            catch (Exception ex)
            {
                var exceptionFailure = AccountOperationResult.Failure(new[] { "Unexpected error while disabling admin account." });
                await TraceAdminDisableAsync(
                    performedByAdminId,
                    adminIdToDisable,
                    exceptionFailure,
                    sourceIp,
                    correlationId,
                    cancellationToken,
                    ex.Message);
                return exceptionFailure;
            }
        }

        private async Task TraceAdminCreateAsync(
            string performedByAdminId,
            string? targetEmail,
            AdminCreationResult result,
            string? sourceIp,
            string? correlationId,
            CancellationToken cancellationToken,
            string? extraMetadata = null)
        {
            var metadata = result.Succeeded
                ? null
                : string.Join(" | ", result.Errors);

            if (!string.IsNullOrWhiteSpace(extraMetadata))
            {
                metadata = string.IsNullOrWhiteSpace(metadata)
                    ? extraMetadata
                    : $"{metadata} | {extraMetadata}";
            }

            await _adminAuditService.TraceAsync(new AdminAuditEntryRequest
            {
                PerformedByAdminId = performedByAdminId,
                ActionType = AdminActionType.AdminCreated,
                IsSuccess = result.Succeeded,
                TargetEntityType = "User",
                TargetEntityId = result.Succeeded ? result.CreatedAdminId : targetEmail,
                Summary = result.Succeeded
                    ? $"Created business admin {targetEmail}"
                    : $"Failed to create business admin {targetEmail}",
                MetadataJson = metadata,
                CorrelationId = correlationId,
                SourceIp = sourceIp,
            }, cancellationToken);
        }

        private async Task TraceAdminDisableAsync(
            string performedByAdminId,
            string targetAdminId,
            AccountOperationResult result,
            string? sourceIp,
            string? correlationId,
            CancellationToken cancellationToken,
            string? extraMetadata = null)
        {
            var metadata = result.Succeeded
                ? null
                : string.Join(" | ", result.Errors);

            if (!string.IsNullOrWhiteSpace(extraMetadata))
            {
                metadata = string.IsNullOrWhiteSpace(metadata)
                    ? extraMetadata
                    : $"{metadata} | {extraMetadata}";
            }

            await _adminAuditService.TraceAsync(new AdminAuditEntryRequest
            {
                PerformedByAdminId = performedByAdminId,
                ActionType = AdminActionType.AdminDisabled,
                IsSuccess = result.Succeeded,
                TargetEntityType = "User",
                TargetEntityId = targetAdminId,
                Summary = result.Succeeded
                    ? $"Disabled admin account {targetAdminId}"
                    : $"Failed to disable admin account {targetAdminId}",
                MetadataJson = metadata,
                CorrelationId = correlationId,
                SourceIp = sourceIp,
            }, cancellationToken);
        }

        private async Task TraceUserAccountActionAsync(
            string performedByAdminId,
            string targetUserId,
            AdminActionType actionType,
            AccountOperationResult result,
            string? sourceIp,
            string? correlationId,
            CancellationToken cancellationToken,
            string? extraMetadata = null)
        {
            var metadata = result.Succeeded
                ? null
                : string.Join(" | ", result.Errors);

            if (!string.IsNullOrWhiteSpace(extraMetadata))
            {
                metadata = string.IsNullOrWhiteSpace(metadata)
                    ? extraMetadata
                    : $"{metadata} | {extraMetadata}";
            }

            await _adminAuditService.TraceAsync(new AdminAuditEntryRequest
            {
                PerformedByAdminId = performedByAdminId,
                ActionType = actionType,
                IsSuccess = result.Succeeded,
                TargetEntityType = "User",
                TargetEntityId = targetUserId,
                Summary = result.Succeeded
                    ? $"{actionType} applied to user {targetUserId}"
                    : $"{actionType} failed for user {targetUserId}",
                MetadataJson = metadata,
                CorrelationId = correlationId,
                SourceIp = sourceIp,
            }, cancellationToken);
        }

        private Task TraceAdminLoginAsync(
            string adminId,
            bool isSuccess,
            string summary,
            string? metadata,
            string? sourceIp,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            return _adminAuditService.TraceAsync(new AdminAuditEntryRequest
            {
                PerformedByAdminId = adminId,
                ActionType = isSuccess ? AdminActionType.AdminLoginSucceeded : AdminActionType.AdminLoginFailed,
                IsSuccess = isSuccess,
                TargetEntityType = "User",
                TargetEntityId = adminId,
                Summary = summary,
                MetadataJson = metadata,
                CorrelationId = correlationId,
                SourceIp = sourceIp,
            }, cancellationToken);
        }

        private async Task<bool> IsAnyAdminAsync(User user)
        {
            return await _userManager.IsInRoleAsync(user, AdminRoles.SystemAdmin)
                || await _userManager.IsInRoleAsync(user, AdminRoles.BusinessAdmin);
        }

        private static string? NormalizeModerationReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return null;
            }

            var normalized = reason.Trim();
            return normalized.Length <= 500
                ? normalized
                : normalized[..500];
        }

        private static string EnsureBannedTag(string? displayName)
        {
            var baseName = string.IsNullOrWhiteSpace(displayName)
                ? "User"
                : RemoveBannedTag(displayName).Trim();

            return baseName.EndsWith(BannedDisplayTag, StringComparison.Ordinal)
                ? baseName
                : baseName + BannedDisplayTag;
        }

        private static string? ExtractReasonFromMetadata(string? metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata))
            {
                return null;
            }

            const string prefix = "Reason:";
            var index = metadata.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return null;
            }

            var reason = metadata[(index + prefix.Length)..].Trim();
            return string.IsNullOrWhiteSpace(reason) ? null : reason;
        }

        private static string RemoveBannedTag(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return "User";
            }

            var current = displayName.Trim();

            while (current.EndsWith(BannedDisplayTag, StringComparison.Ordinal)
                || current.EndsWith("[Banned]", StringComparison.Ordinal))
            {
                if (current.EndsWith(BannedDisplayTag, StringComparison.Ordinal))
                {
                    current = current[..^BannedDisplayTag.Length].TrimEnd();
                    continue;
                }

                current = current[..^"[Banned]".Length].TrimEnd();
            }

            return string.IsNullOrWhiteSpace(current)
                ? "User"
                : current;
        }

        public async Task<RegistrationResult> RegisterAsync(RegisterRequest request)
        {
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                validationErrors.Add("Display name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                validationErrors.Add("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                validationErrors.Add("Password is required.");
            }

            if (validationErrors.Count > 0)
            {
                return RegistrationResult.Failure(validationErrors);
            }

            var normalizedEmail = request.Email.Trim();
            var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
            if (existingUser is not null)
            {
                return RegistrationResult.Failure(new[] { "Email is already in use." });
            }

            var formattedDisplayName = FormatDisplayName(request.DisplayName);
            var normalizedHandle = NormalizeUserHandle(formattedDisplayName);

            var user = new User
            {
                UserName = normalizedHandle,
                Email = normalizedEmail,
                DisplayName = formattedDisplayName,
                IsOnline = false,
                LastSeenAt = null,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var identityErrors = createResult.Errors
                    .Select(error => error.Description)
                    .Where(description => !string.IsNullOrWhiteSpace(description))
                    .ToList();

                return RegistrationResult.Failure(identityErrors);
            }

            await _signInManager.SignInAsync(user, isPersistent: true);
            return RegistrationResult.Success();
        }

        public async Task<AccountOperationResult> UpdateUserProfileAsync(string userId, UpdateProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return AccountOperationResult.Failure(new[] { "Display name is required." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return AccountOperationResult.Failure(new[] { "User profile not found." });
            }

            user.DisplayName = FormatDisplayName(request.DisplayName);
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();

            var requestedUserName = string.IsNullOrWhiteSpace(request.UserName)
                ? user.UserName ?? user.Email ?? user.Id
                : request.UserName.Trim().TrimStart('@');

            var requestedEmail = string.IsNullOrWhiteSpace(request.Email)
                ? user.Email ?? string.Empty
                : request.Email.Trim();

            if (!string.Equals(user.UserName, requestedUserName, StringComparison.Ordinal))
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, requestedUserName);
                if (!setUserNameResult.Succeeded)
                {
                    return FromIdentityResult(setUserNameResult);
                }
            }

            if (!string.Equals(user.Email, requestedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, requestedEmail);
                if (!setEmailResult.Succeeded)
                {
                    return FromIdentityResult(setEmailResult);
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return FromIdentityResult(updateResult);
            }

            if (string.IsNullOrWhiteSpace(request.Bio))
            {
                await _userManager.RemoveAuthenticationTokenAsync(user, ProfileTokenProvider, ProfileBioTokenName);
            }
            else
            {
                await _userManager.SetAuthenticationTokenAsync(user, ProfileTokenProvider, ProfileBioTokenName, request.Bio.Trim());
            }

            return AccountOperationResult.Success();
        }

        public async Task<AccountOperationResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return AccountOperationResult.Failure(new[] { "Unable to resolve current user." });
            }

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return AccountOperationResult.Failure(new[] { "Current and new password are required." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return AccountOperationResult.Failure(new[] { "User profile not found." });
            }

            var changeResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!changeResult.Succeeded)
            {
                return FromIdentityResult(changeResult);
            }

            return AccountOperationResult.Success();
        }

        public async Task<AccountOperationResult> DeactivateAccountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return AccountOperationResult.Failure(new[] { "Unable to resolve current user." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return AccountOperationResult.Failure(new[] { "User profile not found." });
            }

            if (user.AccountStatus == AccountStatus.Deleted)
            {
                return AccountOperationResult.Failure(new[] { "Deleted accounts cannot be deactivated." });
            }

            user.IsOnline = false;
            user.LastSeenAt = DateTime.UtcNow;
            user.AccountStatus = AccountStatus.Deactivated;
            user.DeactivatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return FromIdentityResult(updateResult);
            }
            return AccountOperationResult.Success();
        }

        public async Task<AccountOperationResult> ReactivateAccountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return AccountOperationResult.Failure(new[] { "Unable to resolve current user." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return AccountOperationResult.Failure(new[] { "User profile not found." });
            }

            if (user.AccountStatus != AccountStatus.Deactivated)
            {
                return AccountOperationResult.Failure(new[] { "Account is not deactivated." });
            }

            user.AccountStatus = AccountStatus.Active;
            user.DeactivatedAt = null;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return FromIdentityResult(updateResult);
            }

            return AccountOperationResult.Success();
        }

        public async Task<AccountOperationResult> DeleteAccountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return AccountOperationResult.Failure(new[] { "Unable to resolve current user." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return AccountOperationResult.Failure(new[] { "User profile not found." });
            }

            // Soft delete: anonymize profile and mark account as deleted while
            // keeping content and unique identifiers like email.
            user.DisplayName = "Deleted User";
            user.AvatarUrl = null;
            user.UserName = user.UserName; // keep username value, but it's no longer shown.
            user.IsOnline = false;
            user.LastSeenAt = DateTime.UtcNow;
            user.AccountStatus = AccountStatus.Deleted;
            user.DeletedAt = DateTime.UtcNow;

            // Clear bio token if present.
            await _userManager.RemoveAuthenticationTokenAsync(user, ProfileTokenProvider, ProfileBioTokenName);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return FromIdentityResult(updateResult);
            }
            return AccountOperationResult.Success();
        }

        private async Task<UserProfile?> GetUserProfileInternalAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            await _userProfileLock.WaitAsync();
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user is null)
                {
                    return null;
                }

                var bio = await _userManager.GetAuthenticationTokenAsync(user, ProfileTokenProvider, ProfileBioTokenName);

                return new UserProfile
                {
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    AvatarUrl = user.AvatarUrl,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Bio = bio ?? string.Empty,
                    IsOnline = user.IsOnline,
                    LastSeenAt = user.LastSeenAt,
                    AccountStatus = user.AccountStatus
                };
            }
            catch (ObjectDisposedException)
            {
                // The underlying UserManager or its store has been disposed,
                // typically because the circuit or scope is shutting down.
                // Treat this as "no profile available" for the caller.
                return null;
            }
            finally
            {
                _userProfileLock.Release();
            }
        }


        private static string FormatDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return displayName;

            // Trim leading and trailing whitespace
            displayName = displayName.Trim();

            // Replace multiple spaces with single space
            displayName = System.Text.RegularExpressions.Regex.Replace(displayName, @"\s+", " ");

            // Apply title case: capitalize first letter of each word, lowercase the rest
            var words = displayName.Split(' ');
            var titleCaseWords = words.Select(word =>
                char.ToUpper(word[0]) + (word.Length > 1 ? word.Substring(1).ToLower() : string.Empty)
            ).ToArray();

            return string.Join(" ", titleCaseWords);
        }

        private static string NormalizeUserHandle(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return string.Empty;

            // Convert to lowercase and replace spaces with dashes
            var normalized = displayName.Trim().ToLower();
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", "-");

            return normalized;
        }

        private static AccountOperationResult FromIdentityResult(IdentityResult result)
        {
            if (result.Succeeded)
            {
                return AccountOperationResult.Success();
            }

            var errors = result.Errors
                .Select(error => error.Description)
                .Where(description => !string.IsNullOrWhiteSpace(description))
                .ToList();

            if (errors.Count == 0)
            {
                errors.Add("Operation failed.");
            }

            return AccountOperationResult.Failure(errors);
        }
    }
}
