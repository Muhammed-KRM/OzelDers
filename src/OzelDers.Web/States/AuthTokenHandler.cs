using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace OzelDers.Web.States;

/// <summary>
/// Her HTTP isteğine otomatik olarak Authorization: Bearer header'ı ekleyen handler.
/// LocalStorage'dan JWT Token'ı alır ve request header'larına koyar.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly ProtectedLocalStorage _localStorage;

    public AuthTokenHandler(ProtectedLocalStorage localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _localStorage.GetAsync<UserSession>("UserSession");
            if (result.Success && result.Value != null && !string.IsNullOrEmpty(result.Value.Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.Value.Token);
            }
        }
        catch
        {
            // Prerender sırasında LocalStorage erişilemiyorsa sessizce geç
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
