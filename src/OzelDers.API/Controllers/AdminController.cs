using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Business.Interfaces;

namespace OzelDers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ISettingService _settingService;

    public AdminController(IAdminService adminService, ISettingService settingService)
    {
        _adminService = adminService;
        _settingService = settingService;
    }

    // ─── Sistem Ayarları ─────────────────────────────────────
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        // Örnek olarak bilinen anahtarları döndürelim (veya tümünü veritabanından çekelim)
        // Şimdilik DB'den çekmek daha doğru:
        var settings = new List<OzelDers.Business.DTOs.GlobalSettingDto>
        {
            new() { Key = "ListingCreationCost", Value = await _settingService.GetSettingAsync("ListingCreationCost", "5"), Description = "İlan oluşturma jeton maliyeti" },
            new() { Key = "MessageUnlockCost", Value = await _settingService.GetSettingAsync("MessageUnlockCost", "1"), Description = "Mesaj kilidi açma jeton maliyeti" },
            new() { Key = "DirectOfferCost", Value = await _settingService.GetSettingAsync("DirectOfferCost", "2"), Description = "Direkt teklif gönderme jeton maliyeti" }
        };
        return Ok(settings);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSetting([FromBody] OzelDers.Business.DTOs.GlobalSettingDto dto)
    {
        await _settingService.SetSettingAsync(dto.Key, dto.Value, dto.Description);
        return Ok(new { message = "Ayar güncellendi." });
    }

    // ─── Dashboard ───────────────────────────────────────────
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        return Ok(stats);
    }

    // ─── Kullanıcı Yönetimi ──────────────────────────────────
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role, [FromQuery] string? status)
    {
        var users = await _adminService.GetAllUsersAsync(search, role, status);
        return Ok(users);
    }

    [HttpPut("users/{userId}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid userId)
    {
        await _adminService.SuspendUserAsync(userId);
        return Ok(new { message = "Kullanıcı askıya alındı." });
    }

    [HttpPut("users/{userId}/activate")]
    public async Task<IActionResult> ActivateUser(Guid userId)
    {
        await _adminService.ActivateUserAsync(userId);
        return Ok(new { message = "Kullanıcı aktifleştirildi." });
    }

    // ─── İlan Yönetimi ───────────────────────────────────────
    [HttpGet("listings")]
    public async Task<IActionResult> GetListings([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? type)
    {
        var listings = await _adminService.GetAllListingsAsync(search, status, type);
        return Ok(listings);
    }

    [HttpPut("listings/{listingId}/approve")]
    public async Task<IActionResult> ApproveListing(Guid listingId)
    {
        await _adminService.ApproveListingAsync(listingId);
        return Ok(new { message = "İlan onaylandı." });
    }

    [HttpPut("listings/{listingId}/reject")]
    public async Task<IActionResult> RejectListing(Guid listingId)
    {
        await _adminService.RejectListingAsync(listingId);
        return Ok(new { message = "İlan reddedildi." });
    }

    [HttpPut("listings/{listingId}/suspend")]
    public async Task<IActionResult> SuspendListing(Guid listingId)
    {
        await _adminService.SuspendListingAsync(listingId);
        return Ok(new { message = "İlan askıya alındı." });
    }

    [HttpDelete("listings/{listingId}")]
    public async Task<IActionResult> DeleteListing(Guid listingId)
    {
        await _adminService.DeleteListingAsync(listingId);
        return Ok(new { message = "İlan silindi." });
    }
}
