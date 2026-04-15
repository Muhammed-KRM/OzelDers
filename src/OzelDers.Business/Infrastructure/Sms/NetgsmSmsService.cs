using Microsoft.Extensions.Configuration;
using OzelDers.Business.Interfaces;

namespace OzelDers.Business.Infrastructure.Sms;
public class NetgsmSmsService : ISmsService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public NetgsmSmsService(IConfiguration config, IHttpClientFactory httpFactory)
    {
        _config = config;
        _http = httpFactory.CreateClient("Netgsm");
    }

    public async Task SendAsync(string phoneNumber, string message)
    {
        var user = _config["Netgsm:Username"] ?? "";
        var pass = _config["Netgsm:Password"] ?? "";
        var header = _config["Netgsm:Header"] ?? "OZELDERS";

        // Netgsm HTTP API
        var url = $"https://api.netgsm.com.tr/sms/send/get?" +
                  $"usercode={user}&password={pass}&gsmno={phoneNumber}" +
                  $"&message={Uri.EscapeDataString(message)}&msgheader={header}";

        await _http.GetAsync(url);
        // Hata yönetimi: response body "00" ise başarılı
    }
}
