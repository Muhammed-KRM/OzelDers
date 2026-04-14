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
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API Hatası ({response.StatusCode}): {errorContent}");
        }

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

    public async Task<SearchResultDto> SearchAsync(SearchFilterDto filters, CancellationToken cancellationToken = default)
    {
        var q = $"?page={filters.Page}&pageSize={filters.PageSize}";
        if (!string.IsNullOrEmpty(filters.Query))          q += $"&query={Uri.EscapeDataString(filters.Query)}";
        if (!string.IsNullOrEmpty(filters.Branch))         q += $"&branch={Uri.EscapeDataString(filters.Branch)}";
        if (!string.IsNullOrEmpty(filters.City))           q += $"&city={Uri.EscapeDataString(filters.City)}";
        if (!string.IsNullOrEmpty(filters.District))       q += $"&district={Uri.EscapeDataString(filters.District)}";
        if (!string.IsNullOrEmpty(filters.LessonType))     q += $"&lessonType={filters.LessonType}";
        if (!string.IsNullOrEmpty(filters.ListingType))    q += $"&listingType={filters.ListingType}";
        if (!string.IsNullOrEmpty(filters.SortBy))         q += $"&sortBy={filters.SortBy}";
        if (!string.IsNullOrEmpty(filters.EducationLevel)) q += $"&educationLevel={Uri.EscapeDataString(filters.EducationLevel)}";
        if (!string.IsNullOrEmpty(filters.CategorySlug))   q += $"&categorySlug={filters.CategorySlug}";
        if (filters.MinPrice.HasValue)                     q += $"&minPrice={filters.MinPrice}";
        if (filters.MaxPrice.HasValue)                     q += $"&maxPrice={filters.MaxPrice}";
        if (filters.HasTrialLesson == true)                q += "&hasTrialLesson=true";
        if (filters.IsGroupLesson == true)                 q += "&isGroupLesson=true";
        if (filters.MinExperienceYears.HasValue)           q += $"&minExperienceYears={filters.MinExperienceYears}";
        if (filters.GradeLevel.HasValue)                   q += $"&gradeLevel={filters.GradeLevel}";

        var url = $"api/listings/search{q}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await _http.GetFromJsonAsync<SearchResultDto>(url, cancellationToken) ?? new SearchResultDto();
            sw.Stop();
            Console.WriteLine($"[ListingApiService] SearchAsync tamamlandı — {sw.ElapsedMilliseconds}ms, totalCount={result.TotalCount}");
            return result;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            Console.WriteLine($"[ListingApiService] SearchAsync iptal edildi — {sw.ElapsedMilliseconds}ms");
            throw;
        }
    }

    public async Task<ListingDto> UpdateAsync(Guid id, ListingUpdateDto dto, Guid userId)
    {
        var response = await _http.PutAsJsonAsync($"api/listings/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ListingDto>() ?? new ListingDto();
    }
}
