using System.Text.RegularExpressions;
using FluentValidation;
using Ganss.Xss;
using MassTransit;
using OzelDers.Business.DTOs;
using OzelDers.Business.Events;
using OzelDers.Business.Exceptions;
using OzelDers.Business.Helpers;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Enums;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class ListingManager : IListingService
{
    private readonly IListingRepository _listingRepo;
    private readonly IRepository<Branch> _branchRepo;
    private readonly IRepository<District> _districtRepo;
    private readonly IValidator<ListingCreateDto> _createValidator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ITokenService _tokenService;
    private readonly ISettingService _settingService;

    public ListingManager(
        IListingRepository listingRepo,
        IRepository<Branch> branchRepo,
        IRepository<District> districtRepo,
        IValidator<ListingCreateDto> createValidator,
        IPublishEndpoint publishEndpoint,
        ITokenService tokenService,
        ISettingService settingService)
    {
        _listingRepo = listingRepo;
        _branchRepo = branchRepo;
        _districtRepo = districtRepo;
        _createValidator = createValidator;
        _publishEndpoint = publishEndpoint;
        _tokenService = tokenService;
        _settingService = settingService;
    }

    public async Task<ListingDto> CreateAsync(ListingCreateDto dto, Guid userId)
    {
        // 1. FluentValidation ile doğrula
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new BusinessException(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        // 2. Slug oluştur
        var slug = SlugHelper.GenerateSlug(dto.Title);

        // 3. XSS Koruması ve Entity'ye map et
        var sanitizer = new HtmlSanitizer();
        string sanitizedDescription = sanitizer.Sanitize(dto.Description);

        var listing = new Listing
        {
            OwnerId = userId,
            Type = dto.Type,
            Title = dto.Title,
            Slug = slug,
            Description = sanitizedDescription,
            HourlyPrice = dto.HourlyPrice,
            LessonType = dto.LessonType,
            BranchId = dto.BranchId,
            DistrictId = dto.DistrictId,
            Status = PerformAutoModeration(dto.Title, sanitizedDescription)
        };

        // 4. Jeton Harcaması
        var cost = await _settingService.GetIntSettingAsync("ListingCreationCost", 5);
        await _tokenService.SpendTokenAsync(userId, cost, "Yeni ilan oluşturuldu");

        // 5. Repository ile kaydet
        await _listingRepo.AddAsync(listing);
        await _listingRepo.SaveChangesAsync();

        // 5. Event fırlat (Consumer ES'e indexleyecek + cache'i temizleyecek)
        await _publishEndpoint.Publish(new ListingCreatedEvent { ListingId = listing.Id });

        // 6. DTO olarak döndür
        return MapToDto(listing);
    }

    public async Task<ListingDto?> GetByIdAsync(Guid id)
    {
        var listing = await _listingRepo.GetByIdAsync(id);
        
        // Ensure entity navigations like Branch, District, Owner are loaded when manually mapping
        // In a real scenario we use eager loading (e.g. Include).
        return listing is null ? null : MapToDto(listing);
    }

    public async Task<ListingDto?> GetBySlugAsync(string slug)
    {
        var listing = await _listingRepo.GetBySlugWithDetailsAsync(slug);
        return listing is null ? null : MapToDto(listing);
    }

    public async Task<List<ListingDto>> GetVitrinListingsAsync()
    {
        var listings = await _listingRepo.FindAsync(l => l.IsVitrin && l.Status == ListingStatus.Active);
        return listings.Select(MapToDto).ToList();
    }

    public async Task<List<ListingDto>> GetMyListingsAsync(Guid userId)
    {
        var listings = await _listingRepo.GetAllListingsByOwnerAsync(userId);
        return listings.Select(MapToDto).ToList();
    }

    public async Task<ListingDto> UpdateAsync(Guid id, ListingUpdateDto dto, Guid userId)
    {
        var listing = await _listingRepo.GetByIdAsync(id)
            ?? throw new NotFoundException("İlan", id);

        if (listing.OwnerId != userId)
            throw new UnauthorizedException("Bu ilanı düzenleme yetkiniz yok.");

        var sanitizer = new HtmlSanitizer();
        string sanitizedDescription = sanitizer.Sanitize(dto.Description);

        listing.Type = dto.Type;
        listing.Title = dto.Title;
        listing.Slug = SlugHelper.GenerateSlug(dto.Title);
        listing.Description = sanitizedDescription;
        listing.HourlyPrice = dto.HourlyPrice;
        listing.LessonType = dto.LessonType;
        listing.BranchId = dto.BranchId;
        listing.DistrictId = dto.DistrictId;
        
        // Check auto-moderation for content violation
        var modStatus = PerformAutoModeration(dto.Title, sanitizedDescription);
        
        // If it passed moderation, honor the user's toggle (IsActive)
        if (modStatus == ListingStatus.Active)
        {
            listing.Status = dto.IsActive ? ListingStatus.Active : ListingStatus.Suspended;
        }
        else
        {
            // If violation found, force pending
            listing.Status = ListingStatus.Pending;
        }

        _listingRepo.Update(listing);
        await _listingRepo.SaveChangesAsync();

        // Event fırlat
        await _publishEndpoint.Publish(new ListingUpdatedEvent { ListingId = listing.Id });

        return MapToDto(listing);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var listing = await _listingRepo.GetByIdAsync(id)
            ?? throw new NotFoundException("İlan", id);

        if (listing.OwnerId != userId)
            throw new UnauthorizedException("Bu ilanı silme yetkiniz yok.");

        listing.Status = ListingStatus.Closed;
        _listingRepo.Update(listing);
        await _listingRepo.SaveChangesAsync();

        // Event fırlat
        await _publishEndpoint.Publish(new ListingDeletedEvent { ListingId = listing.Id });
    }

    public async Task<SearchResultDto> SearchAsync(SearchFilterDto filters)
    {
        // Bu metod ileride SearchManager (ES) üzerinden çağrılacak.
        // Şimdilik basit PostgreSQL sorgusu:
        var all = await _listingRepo.FindAsync(l => l.Status == ListingStatus.Active);
        var items = all.Select(MapToDto).ToList();
        return new SearchResultDto
        {
            Items = items,
            TotalCount = items.Count,
            Page = filters.Page,
            PageSize = filters.PageSize
        };
    }

    /// <summary>
    /// İçerikte telefon numarası, e-posta veya yasaklı kelimeler olup olmadığını kontrol eder.
    /// Kurallara uyuyorsa otomatik olarak Active (Yayında) statüsü döndürür.
    /// Uymuyorsa manuel admin kontrolü için Pending (Onay Bekliyor) döndürür.
    /// </summary>
    private ListingStatus PerformAutoModeration(string title, string description)
    {
        string fullText = $"{title} {description}";

        // Basit Telefon Numarası Taraması (Örn: 0555 555 5555, 05555555555)
        var phoneRegex = new Regex(@"0?\s*5\s*\d\s*\d\s*\d\s*\d\s*\d\s*\d\s*\d\s*\d\s*\d");
        
        // Basit E-posta Taraması
        var emailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");

        // Yasaklı kelimeler vs.
        string[] forbiddenWords = { "escort", "kumar", "bahis" }; // vs...

        if (phoneRegex.IsMatch(fullText) || emailRegex.IsMatch(fullText) || forbiddenWords.Any(w => fullText.Contains(w, StringComparison.OrdinalIgnoreCase)))
        {
            return ListingStatus.Pending;
        }

        return ListingStatus.Active;
    }

    private static ListingDto MapToDto(Listing l) => new()
    {
        Id = l.Id,
        OwnerId = l.OwnerId,
        OwnerName = l.Owner?.FullName ?? "",
        OwnerImageUrl = l.Owner?.ProfileImageUrl,
        Type = l.Type,
        Title = l.Title,
        Slug = l.Slug,
        Description = l.Description,
        HourlyPrice = l.HourlyPrice,
        LessonType = l.LessonType,
        BranchName = l.Branch?.Name ?? "",
        CityName = l.District?.City?.Name ?? "",
        DistrictName = l.District?.Name ?? "",
        IsVitrin = l.IsVitrin,
        AverageRating = l.AverageRating,
        ReviewCount = l.ReviewCount,
        Status = l.Status,
        CreatedAt = l.CreatedAt,
        ImageUrls = l.Images?.Select(i => i.ImageUrl).ToList() ?? new()
    };
}
