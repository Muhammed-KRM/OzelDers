using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Context;
using Microsoft.EntityFrameworkCore;
using OzelDers.Data.Enums;
using System.Text.Json;

namespace OzelDers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// KVK Kapsamında kullanıcının tüm kişisel verilerini JSON formatında dışa aktarır (Data Export).
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportData()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var user = await _context.Users
            .Include(u => u.Listings)
            .Include(u => u.SentMessages)
            .Include(u => u.ReceivedMessages)
            .Include(u => u.TokenTransactions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        var exportData = new
        {
            PersonalInfo = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.CreatedAt,
                user.TokenBalance
            },
            Listings = user.Listings.Select(l => new { l.Id, l.Title, l.Status, l.CreatedAt }),
            Messages = new
            {
                Sent = user.SentMessages.Select(m => new { m.Id, m.CreatedAt, m.Status }),
                Received = user.ReceivedMessages.Select(m => new { m.Id, m.CreatedAt, m.Status })
            },
            TokenHistory = user.TokenTransactions.Select(t => new { t.Id, t.Amount, t.Type, t.CreatedAt })
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(exportData, options);

        return File(jsonBytes, "application/json", $"ozelders_export_{DateTime.UtcNow:yyyyMMdd}.json");
    }

    /// <summary>
    /// Mobil uygulama için Firebase Cloud Messaging token'ını kaydeder.
    /// </summary>
    [HttpPost("fcm-token")]
    public async Task<IActionResult> SaveFcmToken([FromBody] FcmTokenDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.FcmToken = dto.Token;
        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// KVK Kapsamında hesabı kalıcı silmez, yasal saklama süreleri nedeniyle "Soft Delete" (Askıya Alma) uygular.
    /// 6 ay sonra anonimleştirilecek.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var user = await _context.Users
            .Include(u => u.Listings)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        // 1. Hesap Pasifleştirilir
        user.IsActive = false;

        // 2. Tüm açık ilanları kapatılır
        foreach (var listing in user.Listings)
        {
            listing.Status = ListingStatus.Closed;
        }

        // 3. İleride sistemde kalıcı silme CronJob'u tarafından işaretlenmesi için tarih konulabilir
        // user.DeletedAt = DateTime.UtcNow; // (Soft-Delete implementation details vary, we simply use IsActive=false)

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Hesabınız başarıyla dondurulmuştur. KVK politikamız gereği verileriniz yasal süre bitiminde tamemen anonimleştirilecektir." });
    }

    /// <summary>
    /// Mevcut kullanıcının temel bilgilerini döndürür (ban durumu, ihlal sayısı vb.).
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.ViolationCount,
            user.BannedUntil,
            user.BanReason,
            user.FcmToken
        });
    }
}

public record FcmTokenDto(string Token);
