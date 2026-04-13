using OzelDers.Business.DTOs;
using OzelDers.Business.Exceptions;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class ReviewManager : IReviewService
{
    // ═══════════════════════════════════════════════
    // HATA KODLARI — ReviewManager (Prefix: RM)
    // ═══════════════════════════════════════════════
    private const string EC_GETBYLISTING = "RM-001"; // GetByListingAsync
    private const string EC_CREATE       = "RM-002"; // CreateAsync
    private const string EC_APPROVE      = "RM-003"; // ApproveReviewAsync
    // ═══════════════════════════════════════════════

    private readonly IRepository<Review> _reviewRepo;
    private readonly IListingRepository _listingRepo;
    private readonly ILogService _logService;

    public ReviewManager(IRepository<Review> reviewRepo, IListingRepository listingRepo, ILogService logService)
    {
        _reviewRepo = reviewRepo;
        _listingRepo = listingRepo;
        _logService = logService;
    }

    public async Task<List<ReviewDto>> GetByListingAsync(Guid listingId)
    {
        try
        {
            var reviews = await _reviewRepo.FindAsync(r => r.ListingId == listingId && r.IsApproved);
            return reviews.OrderByDescending(r => r.CreatedAt).Select(MapToDto).ToList();
        }
        catch (Exception ex) { await _logService.LogFunctionErrorAsync(EC_GETBYLISTING, ex, listingId); throw; }
    }

    public async Task<ReviewDto> CreateAsync(ReviewCreateDto dto, Guid reviewerId)
    {
        try
        {
        var listing = await _listingRepo.GetByIdAsync(dto.ListingId) ?? throw new NotFoundException("İlan", dto.ListingId);
        if (listing.OwnerId == reviewerId) throw new BusinessException("Kendi ilanınıza yorum yapamazsınız.");

        var review = new Review
        {
            ReviewerId = reviewerId, ReviewedId = listing.OwnerId, ListingId = dto.ListingId,
            ProfessionalismRating = dto.ProfessionalismRating, CommunicationRating = dto.CommunicationRating,
            ValueRating = dto.ValueRating, Content = dto.Content, IsApproved = false
        };

        await _reviewRepo.AddAsync(review);
        await _reviewRepo.SaveChangesAsync();
        return MapToDto(review);
        }
        catch (BusinessException) { throw; }
        catch (Exception ex) { await _logService.LogFunctionErrorAsync(EC_CREATE, ex, dto, reviewerId); throw; }
    }

    public async Task ApproveReviewAsync(Guid reviewId)
    {
        try
        {
        var review = await _reviewRepo.GetByIdAsync(reviewId) ?? throw new NotFoundException("Yorum", reviewId);
        review.IsApproved = true;
        _reviewRepo.Update(review);
        await _reviewRepo.SaveChangesAsync();
        }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { await _logService.LogFunctionErrorAsync(EC_APPROVE, ex, reviewId); throw; }
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
