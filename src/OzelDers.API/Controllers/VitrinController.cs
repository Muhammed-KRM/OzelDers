using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Business.DTOs;
using OzelDers.Business.Interfaces;

namespace OzelDers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VitrinController : ControllerBase
{
    private readonly IVitrinService _vitrinService;

    public VitrinController(IVitrinService vitrinService)
    {
        _vitrinService = vitrinService;
    }

    [HttpGet("packages")]
    [AllowAnonymous]
    public async Task<ActionResult<List<VitrinPackageDto>>> GetPackages()
        => Ok(await _vitrinService.GetPackagesAsync());

    public class VitrinPurchaseRequestDto
    {
        public int PackageId { get; set; }
    }

    [HttpPost("{listingId}/purchase")]
    public async Task<IActionResult> Purchase(Guid listingId, [FromBody] VitrinPurchaseRequestDto req, [FromServices] IPaymentService paymentService)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var packages = await _vitrinService.GetPackagesAsync();
        var pkg = packages.FirstOrDefault(p => p.Id == req.PackageId);
        
        if (pkg == null) return BadRequest("Geçersiz paket");

        var description = $"Vitrin Paketi: {pkg.Name} - İlan ID: {listingId}";
        var paymentUrl = await paymentService.InitiatePaymentAsync(pkg.Price, description, userId);
        
        // Demo amaçlı: ID'leri query'sine ekleyerek callback'te bilmemizi sağlıyoruz
        return Ok(new { paymentUrl = $"{paymentUrl}&packageId={pkg.Id}&listingId={listingId}" });
    }

    [HttpPost("payment-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback(
        [FromForm] string token, 
        [FromQuery] int packageId,
        [FromQuery] Guid listingId,
        [FromQuery] Guid userId,
        [FromServices] IPaymentService paymentService)
    {
        var isValid = await paymentService.VerifyPaymentCallbackAsync(token);
        if (!isValid) return BadRequest("Payment verification failed");

        await _vitrinService.PurchaseVitrinAsync(listingId, packageId, userId);

        return Redirect("/panel/ilanlarim?status=vitrin_success");
    }
}
