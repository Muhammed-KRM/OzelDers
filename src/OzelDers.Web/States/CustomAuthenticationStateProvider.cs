using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

using OzelDers.Business.Interfaces;

namespace OzelDers.Web.States;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider, ICustomAuthStateProvider
{
    private readonly ProtectedLocalStorage _localStorage;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(ProtectedLocalStorage localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Prerendering sırasında JS çağrısı yapılamaz. 
            // Bu hata ProtectedLocalStorage tarafından fırlatılır.
            var result = await _localStorage.GetAsync<UserSession>("UserSession");
            var userSession = result.Success ? result.Value : null;
            
            if (userSession == null)
                return new AuthenticationState(_anonymous);

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, userSession.FullName),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.Role, userSession.Role),
                new Claim(ClaimTypes.NameIdentifier, userSession.Id.ToString())
            }, "CustomAuth"));

            return new AuthenticationState(claimsPrincipal);
        }
        catch (Exception)
        {
            // Prerendering sırasında veya hata durumunda anonim kullanıcı döner.
            // Blazor bağlantı kurulduğunda bu metodu tekrar çağıracaktır.
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task UpdateAuthenticationState(UserSession? userSession)
    {
        ClaimsPrincipal claimsPrincipal;

        if (userSession != null)
        {
            await _localStorage.SetAsync("UserSession", userSession);
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
            await _localStorage.DeleteAsync("UserSession");
            claimsPrincipal = _anonymous;
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            var result = await _localStorage.GetAsync<UserSession>("UserSession");
            return result.Success ? result.Value?.Token : null;
        }
        catch { return null; }
    }

    public async Task LogInAsync(Guid id, string fullName, string email, string role, string token = "")
    {
        var session = new UserSession
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

public class UserSession
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
