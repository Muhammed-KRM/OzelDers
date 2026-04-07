using OzelDers.Business.DTOs;
using OzelDers.Business.Exceptions;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class ReviewManager : IReviewService
{
    private readonly IRepository<Review> _reviewRepo;
    private readonly IListingRepository _listingRepo;

    public ReviewManager(IRepository<Review> reviewRepo, IListingRepository listingRepo)
    {
        _reviewRepo = reviewRepo;
        _listingRepo = listingRepo;
    }

    public async Task<List<ReviewDto>> GetByListingAsync(Guid listingId)
    {
        var reviews = await _reviewRepo.FindAsync(r => r.ListingId == listingId && r.IsApproved);
        return reviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ReviewDto> CreateAsync(ReviewCreateDto dto, Guid reviewerId)
    {
        var listing = await _listingRepo.GetByIdAsync(dto.ListingId)
            ?? throw new NotFoundException("İlan", dto.ListingId);

        if (listing.OwnerId == reviewerId)
            throw new BusinessException("Kendi ilanınıza yorum yapamazsınız.");

        var review = new Review
        {
            ReviewerId = reviewerId,
            ReviewedId = listing.OwnerId,
            ListingId = dto.ListingId,
            ProfessionalismRating = dto.ProfessionalismRating,
            CommunicationRating = dto.CommunicationRating,
            ValueRating = dto.ValueRating,
            Content = dto.Content,
            IsApproved = false // Admin onayı bekleniyor
        };

        await _reviewRepo.AddAsync(review);
        await _reviewRepo.SaveChangesAsync();

        return MapToDto(review);
    }

    private static ReviewDto MapToDto(Review r) => new()
    {
        Id = r.Id,
        ReviewerId = r.ReviewerId,
        ReviewerName = r.Reviewer?.FullName ?? "",
        ReviewerImageUrl = r.Reviewer?.ProfileImageUrl,
        ProfessionalismRating = r.ProfessionalismRating,
        CommunicationRating = r.CommunicationRating,
        ValueRating = r.ValueRating,
        AverageRating = r.AverageRating,
        Content = r.Content,
        CreatedAt = r.CreatedAt
    };
}
