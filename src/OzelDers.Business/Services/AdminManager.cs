using Microsoft.EntityFrameworkCore;
using OzelDers.Business.DTOs;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Context;
using OzelDers.Data.Enums;

namespace OzelDers.Business.Services;

public class AdminManager : IAdminService
{
    private readonly AppDbContext _context;

    public AdminManager(AppDbContext context)
    {
        _context = context;
    }

    // ─── Dashboard ───────────────────────────────────────────
    public async Task<AdminDashboardDto> GetDashboardStatsAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalTeachers = await _context.Users.CountAsync(u => u.IsTeacherProfileComplete);
        var totalListings = await _context.Listings.CountAsync();
        var activeListings = await _context.Listings.CountAsync(l => l.Status == ListingStatus.Active);
        var pendingListings = await _context.Listings.CountAsync(l => l.Status == ListingStatus.Pending);
        var totalMessages = await _context.Messages.CountAsync();
        var totalRevenue = await _context.TokenTransactions
            .Where(t => t.Type == TransactionType.Purchase)
            .SumAsync(t => (decimal)t.Amount);

        return new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            TotalTeachers = totalTeachers,
            TotalStudents = totalUsers - totalTeachers,
            TotalListings = totalListings,
            ActiveListings = activeListings,
            PendingListings = pendingListings,
            TotalMessages = totalMessages,
            TotalRevenue = totalRevenue,
            RecentActivities = new List<AdminActivityDto>() // Gerçek aktivite sistemi kurulduğunda doldurulacak
        };
    }

    // ─── Kullanıcı Yönetimi ──────────────────────────────────
    public async Task<List<AdminUserDto>> GetAllUsersAsync(string? search = null, string? role = null, string? status = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                u.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            if (role == "Teacher")
                query = query.Where(u => u.IsTeacherProfileComplete);
            else if (role == "Admin")
                query = query.Where(u => u.Role == UserRole.Admin);
            else if (role == "Student")
                query = query.Where(u => !u.IsTeacherProfileComplete && u.Role != UserRole.Admin);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status == "Active")
                query = query.Where(u => u.IsActive);
            else if (status == "Suspended")
                query = query.Where(u => !u.IsActive);
        }

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.PhoneEncrypted ?? "—",
                Role = u.Role == UserRole.Admin ? "Admin" : (u.IsTeacherProfileComplete ? "Teacher" : "Student"),
                Status = u.IsActive ? "Active" : "Suspended",
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task SuspendUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ActivateUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    // ─── İlan Yönetimi ───────────────────────────────────────
    public async Task<List<AdminListingDto>> GetAllListingsAsync(string? search = null, string? status = null, string? type = null)
    {
        var query = _context.Listings
            .Include(l => l.Owner)
            .Include(l => l.Branch)
            .Include(l => l.District)
                .ThenInclude(d => d.City)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l =>
                l.Title.Contains(search) ||
                l.Owner.FullName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<ListingStatus>(status, out var parsed))
                query = query.Where(l => l.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (type == "Teacher")
                query = query.Where(l => l.Type == ListingType.TeacherOffering);
            else if (type == "Student")
                query = query.Where(l => l.Type == ListingType.StudentLooking);
        }

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new AdminListingDto
            {
                Id = l.Id,
                Title = l.Title,
                TeacherName = l.Owner.FullName,
                Branch = l.Branch.Name,
                City = l.District.City.Name,
                HourlyPrice = l.HourlyPrice,
                Status = l.Status.ToString(),
                IsVitrin = l.IsVitrin,
                ViewCount = l.ReviewCount, // Mevcut modelde ViewCount yok, ReviewCount kullanıldı
                MessageCount = 0, // Mesaj sayısı ayrı sorguyla hesaplanabilir
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();
    }

    public async Task ApproveListingAsync(Guid listingId)
    {
        var listing = await _context.Listings.FindAsync(listingId);
        if (listing != null)
        {
            listing.Status = ListingStatus.Active;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RejectListingAsync(Guid listingId)
    {
        var listing = await _context.Listings.FindAsync(listingId);
        if (listing != null)
        {
            listing.Status = ListingStatus.Suspended; // Reddedilen ilan askıya alınmış sayılır
            await _context.SaveChangesAsync();
        }
    }

    public async Task SuspendListingAsync(Guid listingId)
    {
        var listing = await _context.Listings.FindAsync(listingId);
        if (listing != null)
        {
            listing.Status = ListingStatus.Suspended;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteListingAsync(Guid listingId)
    {
        var listing = await _context.Listings.FindAsync(listingId);
        if (listing != null)
        {
            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync();
        }
    }
}
