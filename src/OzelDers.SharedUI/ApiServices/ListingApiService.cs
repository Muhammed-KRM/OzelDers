using System.Net.Http.Json;
using System.Text.Json;
using OzelDers.Business.DTOs;
using OzelDers.Business.Interfaces;

namespace OzelDers.SharedUI.ApiServices;

public class ListingApiService : IListingService
{
    private readonly HttpClient _http;

    public ListingApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ListingDto> CreateAsync(ListingCreateDto dto, Guid userId)
    {
        var response = await _http.PostAsJsonAsync("api/listings", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ListingDto>() ?? new ListingDto();
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var response = await _http.DeleteAsync($"api/listings/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<ListingDto?> GetByIdAsync(Guid id)
    {
        // Not: Controller tarafında {slug} alıyorduk, 
        // id için varlık eksikse fallback veya slug üstünden çağrılabilir. 
        // Ya da API tarafına GetById eklenebilir. Şimdilik mock veya slug-based ID:
        throw new NotImplementedException();
    }

    public async Task<ListingDto?> GetBySlugAsync(string slug)
    {
        return await _http.GetFromJsonAsync<ListingDto>($"api/listings/{slug}");
    }

    public async Task<List<ListingDto>> GetMyListingsAsync(Guid userId)
    {
        return await _http.GetFromJsonAsync<List<ListingDto>>("api/listings/my") ?? new List<ListingDto>();
    }

    public async Task<List<ListingDto>> GetVitrinListingsAsync()
    {
        return await _http.GetFromJsonAsync<List<ListingDto>>("api/listings/vitrin") ?? new List<ListingDto>();
    }

    public async Task<SearchResultDto> SearchAsync(SearchFilterDto filters)
    {
        // Query param oluşturma
        var queryStr = $"?page={filters.Page}&pageSize={filters.PageSize}";
        if (!string.IsNullOrEmpty(filters.Query)) queryStr += $"&query={Uri.EscapeDataString(filters.Query)}";
        if (!string.IsNullOrEmpty(filters.Branch)) queryStr += $"&branch={Uri.EscapeDataString(filters.Branch)}";
        if (!string.IsNullOrEmpty(filters.City)) queryStr += $"&city={Uri.EscapeDataString(filters.City)}";

        return await _http.GetFromJsonAsync<SearchResultDto>($"api/listings/search{queryStr}") ?? new SearchResultDto();
    }

    public async Task<ListingDto> UpdateAsync(Guid id, ListingUpdateDto dto, Guid userId)
    {
        var response = await _http.PutAsJsonAsync($"api/listings/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ListingDto>() ?? new ListingDto();
    }
}
