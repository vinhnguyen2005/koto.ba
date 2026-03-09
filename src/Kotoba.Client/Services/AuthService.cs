using System.Net.Http.Json;
using Kotoba.Client.Auth;
using Kotoba.Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;

namespace Kotoba.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient http, AuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _authStateProvider = authStateProvider;
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", request);

            if (response.IsSuccessStatusCode)
            {
                // Notify auth state changed
                await ((CustomAuthStateProvider)_authStateProvider).GetAuthenticationStateAsync();
                return (true, null);
            }

            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                // Notify auth state changed
                await ((CustomAuthStateProvider)_authStateProvider).GetAuthenticationStateAsync();
                return (true, null);
            }

            return (false, "Invalid email or password");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _http.PostAsync("api/auth/logout", null);
        }
        catch
        {
            // Ignore logout errors
        }
        finally
        {
            ((CustomAuthStateProvider)_authStateProvider).MarkUserAsLoggedOut();
        }
    }
}
