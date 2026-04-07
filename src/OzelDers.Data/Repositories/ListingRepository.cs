using Microsoft.EntityFrameworkCore;
using OzelDers.Data.Context;
using OzelDers.Data.Entities;
using OzelDers.Data.Enums;

namespace OzelDers.Data.Repositories;

public class ListingRepository : GenericRepository<Listing>, IListingRepository
{
    public ListingRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Listing?> GetBySlugWithDetailsAsync(string slug)
    {
        return await _dbSet
            .Include(l => l.Owner)
            .Include(l => l.Branch)
            .Include(l => l.District)
                .ThenInclude(d => d.City)
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.Slug == slug && l.Status == ListingStatus.Active);
    }

    public async Task<List<Listing>> GetActiveListingsByOwnerAsync(Guid ownerId)
    {
        return await _dbSet
            .Include(l => l.Branch)
            .Where(l => l.OwnerId == ownerId && l.Status == ListingStatus.Active)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }
}
