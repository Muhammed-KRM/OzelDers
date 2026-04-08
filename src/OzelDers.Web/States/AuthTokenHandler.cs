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
    private readonly IJSRuntime _jsRuntime;

    public AuthTokenHandler(ProtectedLocalStorage localStorage, IJSRuntime jsRuntime)
    {
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
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

        // --- MERKEZİ LOGLAMA ---
        string payload = "";
        if (request.Content != null)
        {
            payload = await request.Content.ReadAsStringAsync();
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("console.log", "--------------------------------------------------");
            await _jsRuntime.InvokeVoidAsync("console.log", $"👉 [OzelDers GIDEN ISTEK] {request.Method} {request.RequestUri}");
            await _jsRuntime.InvokeVoidAsync("console.log", $"👉 [HEADER] Auth: {(request.Headers.Authorization != null ? "Eklendi" : "Yok")}");
            if (!string.IsNullOrEmpty(payload))
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"👉 [PAYLOAD]\n{payload}");
            }
        }
        catch { /* Prerendering ignored */ }
        // -----------------------

        var response = await base.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            try 
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"❌ [OzelDers HATA] {response.StatusCode}\nDetay: {errorContent}");
                await _jsRuntime.InvokeVoidAsync("console.log", "--------------------------------------------------");
            } 
            catch { }
            
            throw new HttpRequestException($"API Hatası ({response.StatusCode}): {errorContent}");
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("console.log", $"✅ [OzelDers CEVAP] İşlem başarılı ({response.StatusCode}).");
            await _jsRuntime.InvokeVoidAsync("console.log", "--------------------------------------------------");
        }
        catch { }

        return response;
    }
}
