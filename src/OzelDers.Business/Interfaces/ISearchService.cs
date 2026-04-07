using OzelDers.Business.DTOs;

namespace OzelDers.Business.Interfaces;

public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(SearchFilterDto filters);
    Task IndexListingAsync(ListingDto listing);
    Task DeleteListingIndexAsync(Guid listingId);
}
