using FluentValidation;
using OzelDers.Business.DTOs;
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

    public ListingManager(
        IListingRepository listingRepo,
        IRepository<Branch> branchRepo,
        IRepository<District> districtRepo,
        IValidator<ListingCreateDto> createValidator)
    {
        _listingRepo = listingRepo;
        _branchRepo = branchRepo;
        _districtRepo = districtRepo;
        _createValidator = createValidator;
    }

    public async Task<ListingDto> CreateAsync(ListingCreateDto dto, Guid userId)
    {
        // 1. FluentValidation ile doğrula
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new BusinessException(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        // 2. Slug oluştur
        var slug = SlugHelper.GenerateSlug(dto.Title);

        // 3. Entity'ye map et
        var listing = new Listing
        {
            OwnerId = userId,
            Type = dto.Type,
            Title = dto.Title,
            Slug = slug,
            Description = dto.Description,
            HourlyPrice = dto.HourlyPrice,
            LessonType = dto.LessonType,
            BranchId = dto.BranchId,
            DistrictId = dto.DistrictId,
            Status = ListingStatus.Pending
        };

        // 4. Repository ile kaydet
        await _listingRepo.AddAsync(listing);
        await _listingRepo.SaveChangesAsync();

        // 5. DTO olarak döndür
        return MapToDto(listing);
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
        var listings = await _listingRepo.GetActiveListingsByOwnerAsync(userId);
        return listings.Select(MapToDto).ToList();
    }

    public async Task<ListingDto> UpdateAsync(Guid id, ListingUpdateDto dto, Guid userId)
    {
        var listing = await _listingRepo.GetByIdAsync(id)
            ?? throw new NotFoundException("İlan", id);

        if (listing.OwnerId != userId)
            throw new UnauthorizedException("Bu ilanı düzenleme yetkiniz yok.");

        listing.Title = dto.Title;
        listing.Slug = SlugHelper.GenerateSlug(dto.Title);
        listing.Description = dto.Description;
        listing.HourlyPrice = dto.HourlyPrice;
        listing.LessonType = dto.LessonType;
        listing.BranchId = dto.BranchId;
        listing.DistrictId = dto.DistrictId;

        _listingRepo.Update(listing);
        await _listingRepo.SaveChangesAsync();

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
