using System.Net.Http.Json;
using OzelDers.Business.DTOs;
using OzelDers.Business.Interfaces;

namespace OzelDers.SharedUI.ApiServices;

public class AuthApiService : IAuthService
{
    private readonly HttpClient _http;

    public AuthApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AuthResultDto> RegisterAsync(UserRegisterDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", dto);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResultDto>() ?? new AuthResultDto();
        }
        
        var error = await response.Content.ReadAsStringAsync();
        return new AuthResultDto { Success = false, ErrorMessage = error };
    }

    public async Task<AuthResultDto> LoginAsync(UserLoginDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", dto);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResultDto>() ?? new AuthResultDto();
        }
        
        var error = await response.Content.ReadAsStringAsync();
        return new AuthResultDto { Success = false, ErrorMessage = error };
    }

    public async Task<UserDto?> GetCurrentUserAsync(Guid userId)
    {
        return await _http.GetFromJsonAsync<UserDto>("api/auth/me");
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UserProfileUpdateDto dto)
    {
        var response = await _http.PutAsJsonAsync("api/auth/profile", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>() ?? new UserDto();
    }

    public async Task<AuthResultDto> RefreshTokenAsync(string refreshToken)
    {
        var response = await _http.PostAsJsonAsync("api/auth/refresh-token", new RefreshTokenRequestDto { RefreshToken = refreshToken });
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResultDto>() ?? new AuthResultDto();
        }
        return new AuthResultDto { Success = false, ErrorMessage = "Token refresh failed" };
    }

    // Bu metot normalde back-end (Server) tarafında çağrılır, client API üzerinden bunu yapamaz
    public Task UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiryTime)
    {
        throw new NotImplementedException("Bu metod Client API arayüzünden tetiklenemez.");
    }

    public Task SetUserStatusAsync(Guid userId, bool isActive)
    {
        throw new NotImplementedException("Bu metod Client API arayüzünden tetiklenemez.");
    }
}
