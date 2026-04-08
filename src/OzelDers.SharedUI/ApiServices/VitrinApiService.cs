using System.Net.Http.Json;
using OzelDers.Business.DTOs;
using OzelDers.Business.Interfaces;

namespace OzelDers.SharedUI.ApiServices;

public class VitrinApiService : IVitrinService
{
    private readonly HttpClient _http;

    public VitrinApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<VitrinPackageDto>> GetPackagesAsync()
    {
        return await _http.GetFromJsonAsync<List<VitrinPackageDto>>("api/vitrin/packages") ?? new List<VitrinPackageDto>();
    }

    public async Task PurchaseVitrinAsync(Guid listingId, int packageId, Guid userId)
    {
        var response = await _http.PostAsJsonAsync("api/vitrin/purchase", new { listingId, packageId });
        response.EnsureSuccessStatusCode();
    }
}
