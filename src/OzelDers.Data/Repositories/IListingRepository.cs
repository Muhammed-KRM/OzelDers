using OzelDers.Data.Entities;

namespace OzelDers.Data.Repositories;

public interface IListingRepository : IRepository<Listing>
{
    Task<Listing?> GetBySlugWithDetailsAsync(string slug);
    Task<List<Listing>> GetActiveListingsByOwnerAsync(Guid ownerId);
    Task<List<Listing>> GetAllListingsByOwnerAsync(Guid ownerId);
}
