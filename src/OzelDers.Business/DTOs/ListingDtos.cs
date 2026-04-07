using OzelDers.Data.Enums;

namespace OzelDers.Business.DTOs;

// İlan detay görüntüleme
public class ListingDto
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerImageUrl { get; set; }
    public ListingType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int HourlyPrice { get; set; }
    public LessonType LessonType { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
    public bool IsVitrin { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public ListingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

// İlan oluşturma formu
public class ListingCreateDto
{
    public ListingType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int HourlyPrice { get; set; }
    public LessonType LessonType { get; set; }
    public int BranchId { get; set; }
    public int DistrictId { get; set; }
}

// İlan güncelleme formu
public class ListingUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int HourlyPrice { get; set; }
    public LessonType LessonType { get; set; }
    public int BranchId { get; set; }
    public int DistrictId { get; set; }
}
