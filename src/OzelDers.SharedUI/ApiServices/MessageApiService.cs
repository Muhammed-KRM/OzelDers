using System.Net.Http.Json;
using OzelDers.Business.DTOs;
using OzelDers.Business.Interfaces;

namespace OzelDers.SharedUI.ApiServices;

public class MessageApiService : IMessageService
{
    private readonly HttpClient _http;

    public MessageApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<MessageDto>> GetInboxAsync(Guid userId)
    {
        return await _http.GetFromJsonAsync<List<MessageDto>>("api/messages/inbox") ?? new List<MessageDto>();
    }

    public async Task<List<MessageDto>> GetConversationAsync(Guid userId, Guid otherUserId)
    {
        return await _http.GetFromJsonAsync<List<MessageDto>>($"api/messages/conversation/{otherUserId}") ?? new List<MessageDto>();
    }

    public async Task<MessageDto> SendMessageAsync(MessageSendDto dto, Guid senderId)
    {
        var response = await _http.PostAsJsonAsync("api/messages/send", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageDto>() ?? new MessageDto();
    }

    public async Task UnlockMessageAsync(Guid messageId, Guid userId)
    {
        var response = await _http.PostAsync($"api/messages/{messageId}/unlock", null);
        response.EnsureSuccessStatusCode();
    }
}
