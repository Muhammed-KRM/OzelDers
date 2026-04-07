using OzelDers.Business.DTOs;

namespace OzelDers.Business.Interfaces;

public interface IReviewService
{
    Task<List<ReviewDto>> GetByListingAsync(Guid listingId);
    Task<ReviewDto> CreateAsync(ReviewCreateDto dto, Guid reviewerId);
}
