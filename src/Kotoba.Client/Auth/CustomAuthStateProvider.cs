using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Kotoba.Client.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _http;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public CustomAuthStateProvider(HttpClient http)
    {
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/auth/me");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UserAuthInfo>();

                if (result?.UserId != null)
                {
                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, result.UserId),
                        new Claim(ClaimTypes.Name, result.UserId)
                    }, "apiauth");

                    var user = new ClaimsPrincipal(identity);
                    var state = new AuthenticationState(user);

                    NotifyAuthenticationStateChanged(Task.FromResult(state));
                    return state;
                }
            }
        }
        catch
        {
            // If auth check fails, user is not authenticated
        }

        return new AuthenticationState(_anonymous);
    }

    public void MarkUserAsLoggedOut()
    {
        var anonymousState = new AuthenticationState(_anonymous);
        NotifyAuthenticationStateChanged(Task.FromResult(anonymousState));
    }

    private class UserAuthInfo
    {
        public string? UserId { get; set; }
    }
}
