using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

namespace OzelDers.Web.States;

/// <summary>
/// Her HTTP isteğine otomatik olarak Authorization: Bearer header'ı ekleyen handler.
/// LocalStorage'dan JWT Token'ı alır ve request header'larına koyar.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly ProtectedLocalStorage _localStorage;
    // JS Runtime kaldırıldı çünkü prerendering sırasında circuit çökmesine neden oluyor

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
        catch (Exception)
        {
            // Prerender sırasında LocalStorage erişilemiyorsa sessizce geç
        }

        var response = await base.SendAsync(request, cancellationToken);
        
        // 401/403 hatalarını yutma — çağıran servisin ele alması için response döndür
        // Sadece gerçek API hataları (400, 500 vb.) için exception fırlat
        if (!response.IsSuccessStatusCode 
            && response.StatusCode != System.Net.HttpStatusCode.Unauthorized 
            && response.StatusCode != System.Net.HttpStatusCode.Forbidden)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API Hatası ({response.StatusCode}): {errorContent}");
        }

        return response;
    }
}
