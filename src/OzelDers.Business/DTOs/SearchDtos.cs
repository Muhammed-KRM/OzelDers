namespace OzelDers.Business.DTOs;

// Arama filtreleri
public class SearchFilterDto
{
    public string? Query { get; set; }
    public string? Branch { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public string? LessonType { get; set; }
    public string? ListingType { get; set; }
    public string? SortBy { get; set; } // "price_asc", "price_desc", "rating", "newest"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

// Arama sonucu
public class SearchResultDto
{
    public List<ListingDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
