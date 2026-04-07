using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Business.DTOs;
using OzelDers.Business.Interfaces;

namespace OzelDers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TokensController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public TokensController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpGet("balance")]
    public async Task<ActionResult<int>> GetBalance()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _tokenService.GetBalanceAsync(userId));
    }

    [HttpGet("packages")]
    [AllowAnonymous]
    public async Task<ActionResult<List<TokenPackageDto>>> GetPackages()
        => Ok(await _tokenService.GetPackagesAsync());

    [HttpGet("history")]
    public async Task<ActionResult<List<TokenTransactionDto>>> GetHistory()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _tokenService.GetTransactionHistoryAsync(userId));
    }

    public class PurchaseRequestDto 
    {
        public int PackageId { get; set; }
    }

    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase([FromBody] PurchaseRequestDto req, [FromServices] IPaymentService paymentService)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var packages = await _tokenService.GetPackagesAsync();
        var pkg = packages.FirstOrDefault(p => p.Id == req.PackageId);
        
        if (pkg == null) return BadRequest("Geçersiz paket");

        var description = $"{pkg.TokenCount} Jeton Paketi Alımı";
        var paymentUrl = await paymentService.InitiatePaymentAsync(pkg.Price, description, userId);
        
        // Demo amaçlı: Paket ID'sini URL query'sine ekleyerek callback'te bilmemizi sağlıyoruz
        // Gerçek bir senaryoda bu iyzico'nun custom field'ları (ConversationId vs.) üzerinden gider.
        return Ok(new { paymentUrl = $"{paymentUrl}&packageId={pkg.Id}" });
    }

    [HttpPost("payment-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback(
        [FromForm] string token, 
        [FromQuery] int packageId,
        [FromQuery] Guid userId,
        [FromServices] IPaymentService paymentService,
        [FromServices] MassTransit.IPublishEndpoint publishEndpoint)
    {
        var isValid = await paymentService.VerifyPaymentCallbackAsync(token);
        if (!isValid) return BadRequest("Payment verification failed");

        // Idempotency: Aynı token ile tekrar gelen callback'i işleme
        var history = await _tokenService.GetTransactionHistoryAsync(userId);
        if (history.Any(t => t.Description.Contains(token)))
            return Ok(new { message = "Bu ödeme zaten işlendi." });

        var packages = await _tokenService.GetPackagesAsync();
        var pkg = packages.FirstOrDefault(p => p.Id == packageId);
        if (pkg == null) return BadRequest("Invalid package in callback");

        await _tokenService.AddTokenAsync(userId, pkg.TokenCount, $"Kredi Kartı ile Alım (Ref: {token})");

        await publishEndpoint.Publish(new OzelDers.Business.Events.TokenPurchasedEvent
        {
            UserId = userId,
            Amount = pkg.TokenCount
        });

        return Redirect("/panel/jetonlarim?status=success");
    }
}
