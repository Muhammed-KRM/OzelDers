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

    [HttpPost("{listingId}/purchase")]
    public async Task<IActionResult> Purchase(Guid listingId, [FromQuery] int packageId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _vitrinService.PurchaseVitrinAsync(listingId, packageId, userId);
        return Ok();
    }
}
