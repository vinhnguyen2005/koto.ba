using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Enums;

namespace Kotoba.Modules.Domain.Interfaces;

/// <summary>
/// Service for managing user accounts and profiles.
/// </summary>
public interface IUserService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequest request);
    Task<bool> LoginAsync(LoginRequest request);
    Task<string?> LoginAdminAsync(LoginRequest request);
    Task<AccountStatus?> GetAccountStatusByEmailAsync(string? email, CancellationToken cancellationToken = default);
    Task<AdminCreationResult> CreateBusinessAdminAsync(
        CreateBusinessAdminRequest request,
        string performedByAdminId,
        string? sourceIp = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
    Task<AccountOperationResult> DisableAdminAsync(
        string adminIdToDisable,
        string performedByAdminId,
        string? sourceIp = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessAdminListItem>> GetBusinessAdminsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NormalUserListItem>> GetNormalUsersAsync(CancellationToken cancellationToken = default);
    Task<AccountOperationResult> DeactivateUserByAdminAsync(
        string userId,
        string performedByAdminId,
        string? sourceIp = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
    Task<AccountOperationResult> ReactivateUserByAdminAsync(
        string userId,
        string performedByAdminId,
        string? sourceIp = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
    Task<AccountOperationResult> BanUserByAdminAsync(
        string userId,
        string performedByAdminId,
        string? sourceIp = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
    Task<AccountOperationResult> UnbanUserByAdminAsync(
        string userId,
        string performedByAdminId,
        string? sourceIp = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
    Task<UserProfile?> GetUserProfileAsync(string userId);
    List<UserProfile> GetUsersByDisplayNameAsync(string searchValue);
    Task<AccountOperationResult> UpdateUserProfileAsync(string userId, UpdateProfileRequest request);
    Task<AccountOperationResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<AccountOperationResult> DeactivateAccountAsync(string userId);
    Task<AccountOperationResult> ReactivateAccountAsync(string userId);
    Task<AccountOperationResult> DeleteAccountAsync(string userId);
}
