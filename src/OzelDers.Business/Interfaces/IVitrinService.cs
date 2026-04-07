using OzelDers.Business.DTOs;

namespace OzelDers.Business.Interfaces;

public interface IVitrinService
{
    Task<List<VitrinPackageDto>> GetPackagesAsync();
    Task PurchaseVitrinAsync(Guid listingId, int packageId, Guid userId);
}
