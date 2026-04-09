using System.Net.Http.Json;
using OzelDers.Business.DTOs;

using OzelDers.Business.Interfaces;

namespace OzelDers.SharedUI.ApiServices;

public class UserApiService : IUserService
{
    private readonly HttpClient _http;

    public UserApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        return await _http.GetFromJsonAsync<UserProfileDto>("api/users/profile") ?? new UserProfileDto();
    }

    public async Task UpdatePersonalInfoAsync(Guid userId, PersonalInfoDto dto)
    {
        var response = await _http.PutAsJsonAsync("api/users/personal-info", dto);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdatePaymentInfoAsync(Guid userId, PaymentInfoDto dto)
    {
        var response = await _http.PutAsJsonAsync("api/users/payment-info", dto);
        response.EnsureSuccessStatusCode();
    }

    public async Task ChangePasswordAsync(Guid userId, PasswordChangeDto dto)
    {
        var response = await _http.PutAsJsonAsync("api/users/change-password", dto);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new ApplicationException(content);
        }
    }

    public async Task UpdateNotificationSettingsAsync(Guid userId, NotificationSettingsDto dto)
    {
        var response = await _http.PutAsJsonAsync("api/users/notification-settings", dto);
        response.EnsureSuccessStatusCode();
    }
}
