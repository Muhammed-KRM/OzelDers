using System.Net.Http.Headers;
using System.Text.Json;

namespace OzelDers.App.States;

public class MauiAuthTokenHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var userSessionJson = Preferences.Default.Get<string>("UserSession", string.Empty);
            if (!string.IsNullOrEmpty(userSessionJson))
            {
                var userSession = JsonSerializer.Deserialize<MauiUserSession>(userSessionJson);
                if (userSession != null && !string.IsNullOrEmpty(userSession.Token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userSession.Token);
                }
            }
        }
        catch (Exception)
        {
            // Ignored if Preferences not available
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
