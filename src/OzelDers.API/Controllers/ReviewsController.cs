using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Business.DTOs;
using OzelDers.Business.Interfaces;

namespace OzelDers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("listing/{listingId}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ReviewDto>>> GetByListing(Guid listingId)
        => Ok(await _reviewService.GetByListingAsync(listingId));

    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(ReviewCreateDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _reviewService.CreateAsync(dto, userId));
    }
}
