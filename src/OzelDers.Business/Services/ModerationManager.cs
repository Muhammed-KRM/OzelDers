using System.Text.RegularExpressions;
using OzelDers.Business.Infrastructure.Moderation;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class ModerationManager : IModerationService
{
    // ═══════════════════════════════════════════════
    // HATA KODLARI — ModerationManager (Prefix: MM)
    // ═══════════════════════════════════════════════
    private const string EC_CHECK  = "MM-001"; // CheckContent
    private const string EC_STRIKE = "MM-002"; // AddStrikeAsync
    // ═══════════════════════════════════════════════

    private readonly IRepository<User> _userRepo;
    private readonly IRepository<ViolationLog> _violationRepo;
    private readonly ILogService _logService;

    // Compiled regex'ler — static, bir kez derlenir
    private static readonly Regex PhoneRegex = new(
        @"(\+90|0090|0)[\s\-\.\(\)\*\[\]\/\\]?[5][0-9][\s\-\.\(\)\*\[\]\/\\]?\d{2}[\s\-\.\(\)\*\[\]\/\\]?\d{3}[\s\-\.\(\)\*\[\]\/\\]?\d{2}[\s\-\.\(\)\*\[\]\/\\]?\d{2}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneSimpleRegex = new(
        @"\b0[5][0-9]\d{8}\b",
        RegexOptions.Compiled);

    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LinkRegex = new(
        @"(https?://[^\s]+|\bwww\.[a-zA-Z0-9\-]+\.[a-zA-Z]{2,})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ModerationManager(
        IRepository<User> userRepo,
        IRepository<ViolationLog> violationRepo,
        ILogService logService)
    {
        _userRepo = userRepo;
        _violationRepo = violationRepo;
        _logService = logService;
    }

    public ModerationResult CheckContent(string title, string description)
    {
        // Normalize et (homoglyph + Türkçe rakam)
        var raw = $"{title} {description}";
        var normalized = TurkishTextNormalizer.Normalize(raw);

        if (PhoneRegex.IsMatch(normalized) || PhoneSimpleRegex.IsMatch(normalized))
            return ModerationResult.Violation("Phone",
                "İlan içeriğinde telefon numarası paylaşılamaz. Platform üzerinden iletişim kurulmalıdır.");

        if (EmailRegex.IsMatch(normalized))
            return ModerationResult.Violation("Email",
                "İlan içeriğinde e-posta adresi paylaşılamaz.");

        if (LinkRegex.IsMatch(normalized))
            return ModerationResult.Violation("Link",
                "İlan içeriğinde harici link paylaşılamaz.");

        return ModerationResult.Clean();
    }

    public async Task AddStrikeAsync(Guid userId, Guid? listingId, string listingTitle,
        string violationType, string detectedContent, string detectedBy)
    {
        try
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return;

            user.ViolationCount++;
            user.LastViolationAt = DateTime.UtcNow;

            // Ban hesapla
            var ban = CalculateBan(user.ViolationCount);
            if (ban.HasValue)
            {
                user.BannedUntil = ban == TimeSpan.MaxValue
                    ? DateTime.MaxValue
                    : DateTime.UtcNow.Add(ban.Value);
                user.BanReason = $"Otomatik: {violationType} ihlali ({user.ViolationCount}. ihlal)";
            }

            _userRepo.Update(user);

            // İhlal kaydı
            await _violationRepo.AddAsync(new ViolationLog
            {
                UserId = userId,
                ListingId = listingId,
                ListingTitle = listingTitle,
                ViolationType = violationType,
                DetectedContent = detectedContent.Length > 200
                    ? detectedContent[..200] + "..." : detectedContent,
                DetectedBy = detectedBy,
                CreatedAt = DateTime.UtcNow
            });

            await _userRepo.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            await _logService.LogFunctionErrorAsync(EC_STRIKE, ex, new { userId, violationType });
            throw;
        }
    }

    private static TimeSpan? CalculateBan(int count) => count switch
    {
        >= 11 => TimeSpan.MaxValue,
        8     => TimeSpan.FromDays(30),
        5     => TimeSpan.FromDays(7),
        _     => null
    };
}
