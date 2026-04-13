using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using OzelDers.Business.Interfaces;

namespace OzelDers.App.States;

public class MauiAuthenticationStateProvider : AuthenticationStateProvider, ICustomAuthStateProvider
{
    private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userSessionJson = Preferences.Default.Get<string>("UserSession", string.Empty);
            if (string.IsNullOrEmpty(userSessionJson))
                return Task.FromResult(new AuthenticationState(_anonymous));

            var userSession = JsonSerializer.Deserialize<MauiUserSession>(userSessionJson);
            if (userSession == null)
                return Task.FromResult(new AuthenticationState(_anonymous));

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, userSession.FullName),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.Role, userSession.Role),
                new Claim(ClaimTypes.NameIdentifier, userSession.Id.ToString())
            }, "CustomAuth"));

            return Task.FromResult(new AuthenticationState(claimsPrincipal));
        }
        catch (Exception)
        {
            return Task.FromResult(new AuthenticationState(_anonymous));
        }
    }

    public Task UpdateAuthenticationState(MauiUserSession? userSession)
    {
        ClaimsPrincipal claimsPrincipal;

        if (userSession != null)
        {
            Preferences.Default.Set("UserSession", JsonSerializer.Serialize(userSession));
            claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, userSession.FullName),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.Role, userSession.Role),
                new Claim(ClaimTypes.NameIdentifier, userSession.Id.ToString())
            }, "CustomAuth"));
        }
        else
        {
            Preferences.Default.Remove("UserSession");
            claimsPrincipal = _anonymous;
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        return Task.CompletedTask;
    }

    public async Task LogInAsync(Guid id, string fullName, string email, string role, string token = "")
    {
        var session = new MauiUserSession
        {
            Id = id,
            FullName = fullName,
            Email = email,
            Role = role,
            Token = token
        };
        await UpdateAuthenticationState(session);
    }

    public async Task LogOutAsync()
    {
        await UpdateAuthenticationState(null);
    }
}

public class MauiUserSession
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
