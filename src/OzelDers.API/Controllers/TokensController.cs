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
}
