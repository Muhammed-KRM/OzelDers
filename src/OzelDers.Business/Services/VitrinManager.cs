using OzelDers.Business.DTOs;
using OzelDers.Business.Exceptions;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class VitrinManager : IVitrinService
{
    private readonly IRepository<VitrinPackage> _packageRepo;
    private readonly IListingRepository _listingRepo;
    private readonly ISearchService _searchService;

    private readonly IListingService _listingService;

    public VitrinManager(
        IRepository<VitrinPackage> packageRepo, 
        IListingRepository listingRepo,
        ISearchService searchService,
        IListingService listingService)
    {
        _packageRepo = packageRepo;
        _listingRepo = listingRepo;
        _searchService = searchService;
        _listingService = listingService;
    }

    public async Task<List<VitrinPackageDto>> GetPackagesAsync()
    {
        var packages = await _packageRepo.GetAllAsync();
        return packages.Select(p => new VitrinPackageDto
        {
            Id = p.Id,
            Name = p.Name,
            DurationInDays = p.DurationInDays,
            Price = p.Price,
            IncludesAmberGlow = p.IncludesAmberGlow,
            IncludesTopRanking = p.IncludesTopRanking,
            IncludesHomeCarousel = p.IncludesHomeCarousel
        }).ToList();
    }

    public async Task PurchaseVitrinAsync(Guid listingId, int packageId, Guid userId)
    {
        // İlanın kullanıcıya ait olduğunu doğrula
        var listing = await _listingRepo.GetByIdAsync(listingId)
            ?? throw new NotFoundException("İlan", listingId);

        if (listing.OwnerId != userId)
            throw new UnauthorizedException("Bu ilana vitrin paketi alma yetkiniz yok.");

        var package = (await _packageRepo.FindAsync(p => p.Id == packageId)).FirstOrDefault()
            ?? throw new NotFoundException("Vitrin Paketi", packageId);

        // Vitrin süresini ekle (eğer zaten vitrindeyse, sürenin üzerine ekle)
        var now = DateTime.UtcNow;
        if (listing.IsVitrin && listing.VitrinExpiresAt.HasValue && listing.VitrinExpiresAt.Value > now)
        {
            listing.VitrinExpiresAt = listing.VitrinExpiresAt.Value.AddDays(package.DurationInDays);
        }
        else
        {
            listing.IsVitrin = true;
            listing.VitrinExpiresAt = now.AddDays(package.DurationInDays);
        }

        _listingRepo.Update(listing);
        await _listingRepo.SaveChangesAsync();

        // Index'i güncelle
        var listingDto = await _listingService.GetByIdAsync(listingId);
        if (listingDto != null)
        {
            await _searchService.IndexListingAsync(listingDto);
        }
    }
}
