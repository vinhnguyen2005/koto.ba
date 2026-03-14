using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Kotoba.Modules.Infrastructure.Services.Identity
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public UserService(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public Task<UserProfile?> GetUserProfileAsync(string userId)
        {
            return GetUserProfileInternalAsync(userId);
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

            var result = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: false);

            return result.Succeeded;
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

            var user = new User
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                DisplayName = request.DisplayName.Trim(),
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

        public async Task<bool> UpdateUserProfileAsync(string userId, UpdateProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return false;
            }

            user.DisplayName = request.DisplayName.Trim();
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            return updateResult.Succeeded;
        }

        private async Task<UserProfile?> GetUserProfileInternalAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return null;
            }

            return new UserProfile
            {
                UserId = user.Id,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                IsOnline = user.IsOnline,
                LastSeenAt = user.LastSeenAt
            };
        }
    }
}
