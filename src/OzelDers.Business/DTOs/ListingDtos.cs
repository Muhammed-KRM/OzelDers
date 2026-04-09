using System.ComponentModel.DataAnnotations;
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

    [Required(ErrorMessage = "İlan başlığı zorunludur.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Başlık 5-100 karakter arasında olmalıdır.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(1000, MinimumLength = 20, ErrorMessage = "Açıklama 20-1000 karakter arasında olmalıdır.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Saatlik ücret zorunludur.")]
    [Range(1, 10000, ErrorMessage = "Saatlik ücret 1-10.000 TL arasında olmalıdır.")]
    public int HourlyPrice { get; set; }

    public LessonType LessonType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir branş seçiniz.")]
    public int BranchId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir ilçe seçiniz.")]
    public int DistrictId { get; set; }
}

// İlan güncelleme formu
public class ListingUpdateDto
{
    public ListingType Type { get; set; }

    [Required(ErrorMessage = "İlan başlığı zorunludur.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Başlık 5-100 karakter arasında olmalıdır.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(1000, MinimumLength = 20, ErrorMessage = "Açıklama 20-1000 karakter arasında olmalıdır.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Saatlik ücret zorunludur.")]
    [Range(1, 10000, ErrorMessage = "Saatlik ücret 1-10.000 TL arasında olmalıdır.")]
    public int HourlyPrice { get; set; }

    public LessonType LessonType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir branş seçiniz.")]
    public int BranchId { get; set; }

    public int CityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir ilçe seçiniz.")]
    public int DistrictId { get; set; }

    public bool IsActive { get; set; }
}
