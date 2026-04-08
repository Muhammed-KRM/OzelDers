using System.Net.Http.Json;
using OzelDers.Business.DTOs;
using OzelDers.Business.Interfaces;

namespace OzelDers.SharedUI.ApiServices;

public class TokenApiService : ITokenService
{
    private readonly HttpClient _http;

    public TokenApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<int> GetBalanceAsync(Guid userId)
    {
        return await _http.GetFromJsonAsync<int>("api/tokens/balance");
    }

    public async Task<List<TokenPackageDto>> GetPackagesAsync()
    {
        return await _http.GetFromJsonAsync<List<TokenPackageDto>>("api/tokens/packages") ?? new List<TokenPackageDto>();
    }

    public async Task<List<TokenTransactionDto>> GetTransactionHistoryAsync(Guid userId)
    {
        return await _http.GetFromJsonAsync<List<TokenTransactionDto>>("api/tokens/history") ?? new List<TokenTransactionDto>();
    }

    public Task SpendTokenAsync(Guid userId, int amount, string reason)
    {
        throw new NotImplementedException("Jeton harcama işlemi sadece API arkaplanında yapılır.");
    }

    public Task AddTokenAsync(Guid userId, int amount, string reason)
    {
        throw new NotImplementedException("Jeton ekleme işlemi sadece API arkaplanında (callback) yapılır.");
    }
}
