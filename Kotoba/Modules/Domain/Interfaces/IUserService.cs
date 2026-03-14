using Kotoba.Modules.Domain.DTOs;

namespace Kotoba.Modules.Domain.Interfaces;

/// <summary>
/// Service for managing user accounts and profiles.
/// </summary>
public interface IUserService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequest request);
    Task<bool> LoginAsync(LoginRequest request);
    Task<UserProfile?> GetUserProfileAsync(string userId);
    Task<bool> UpdateUserProfileAsync(string userId, UpdateProfileRequest request);
}
