using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Business.Interfaces;

namespace OzelDers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IReviewService _reviewService;

    public AdminController(IAuthService authService, IReviewService reviewService)
    {
        _authService = authService;
        _reviewService = reviewService;
    }

    [HttpPatch("users/{userId}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid userId)
    {
        await _authService.SetUserStatusAsync(userId, false);
        return Ok(new { message = "Kullanıcı askıya alındı." });
    }

    [HttpPatch("users/{userId}/activate")]
    public async Task<IActionResult> ActivateUser(Guid userId)
    {
        await _authService.SetUserStatusAsync(userId, true);
        return Ok(new { message = "Kullanıcı aktifleştirildi." });
    }

    [HttpPatch("reviews/{reviewId}/approve")]
    public async Task<IActionResult> ApproveReview(Guid reviewId)
    {
        await _reviewService.ApproveReviewAsync(reviewId);
        return Ok(new { message = "Yorum onaylandı." });
    }
}
