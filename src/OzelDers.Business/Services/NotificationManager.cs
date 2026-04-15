using OzelDers.Business.DTOs;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class NotificationManager : INotificationService
{
    // ═══════════════════════════════════════════════
    // HATA KODLARI — NotificationManager (Prefix: NM)
    // ═══════════════════════════════════════════════
    private const string EC_CREATE   = "NM-001";
    private const string EC_GETCOUNT = "NM-002";
    private const string EC_GETLIST  = "NM-003";
    private const string EC_READ     = "NM-004";
    private const string EC_READALL  = "NM-005";
    // ═══════════════════════════════════════════════

    private readonly IRepository<Notification> _repo;
    private readonly ILogService _logService;

    public NotificationManager(IRepository<Notification> repo, ILogService logService)
    {
        _repo = repo;
        _logService = logService;
    }

    public async Task<Notification> CreateAsync(Guid userId, string type, string title,
        string message, string? actionUrl = null, string? idempotencyKey = null)
    {
        try
        {
            // Duplicate önleme: aynı idempotency key varsa tekrar yazma
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var existing = await _repo.FindAsync(n =>
                    n.UserId == userId &&
                    n.Type == type &&
                    n.IdempotencyKey == idempotencyKey);
                if (existing.Any()) return existing.First();
            }

            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                ActionUrl = actionUrl,
                IdempotencyKey = idempotencyKey,
                ExpiresAt = DateTime.UtcNow.AddDays(90)
            };
            await _repo.AddAsync(notification);
            await _repo.SaveChangesAsync();
            return notification;
        }
        catch (Exception ex)
        {
            await _logService.LogFunctionErrorAsync(EC_CREATE, ex, new { userId, type });
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        try
        {
            var all = await _repo.FindAsync(n => n.UserId == userId && !n.IsRead);
            return all.Count();
        }
        catch (Exception ex)
        {
            await _logService.LogFunctionErrorAsync(EC_GETCOUNT, ex, userId);
            throw;
        }
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId,
        int page = 1, int pageSize = 20)
    {
        try
        {
            var all = await _repo.FindAsync(n => n.UserId == userId);
            return all
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    Title = n.Title,
                    Message = n.Message,
                    ActionUrl = n.ActionUrl,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToList();
        }
        catch (Exception ex)
        {
            await _logService.LogFunctionErrorAsync(EC_GETLIST, ex, userId);
            throw;
        }
    }

    public async Task MarkAsReadAsync(int notificationId, Guid userId)
    {
        try
        {
            var matches = await _repo.FindAsync(n => n.Id == notificationId && n.UserId == userId);
            var n = matches.FirstOrDefault();
            if (n == null) return;
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
            _repo.Update(n);
            await _repo.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            await _logService.LogFunctionErrorAsync(EC_READ, ex, new { notificationId, userId });
            throw;
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        try
        {
            var unread = await _repo.FindAsync(n => n.UserId == userId && !n.IsRead);
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
                _repo.Update(n);
            }
            await _repo.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            await _logService.LogFunctionErrorAsync(EC_READALL, ex, userId);
            throw;
        }
    }
}
