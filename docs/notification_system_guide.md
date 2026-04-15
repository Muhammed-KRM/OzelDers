# OzelDers — Bildirim Sistemi ve Moderasyon UI Uygulama Rehberi

**Versiyon:** 1.0  
**Tarih:** Nisan 2026

---

## BÖLÜM 1 — Amaç ve Kapsam

### Ne Yapmak İstiyoruz?

Bu sistem üç ayrı sorunu çözüyor:

**1. Moderasyon Görünürlüğü**
- Kullanıcı uyarı/ban aldığında bunu görmeli (navbar'da, popup olarak)
- Admin ihlalleri görmeli, ban uygulayabilmeli, kaldırabilmeli
- Şu an ilan "Onay Bekliyor"a düşüyor ama kullanıcıya hiçbir şey söylenmiyor

**2. Site İçi Bildirim Merkezi**
- Navbar'da çan simgesi (🔔), okunmamış sayısı badge olarak
- Tıklayınca dropdown veya /bildirimler sayfası
- Bildirimler DB'de saklanır, okundu/okunmadı takibi

**3. Çok Kanallı Bildirim Gönderimi**
- Site içi (her zaman)
- E-posta (kullanıcı izin verdiyse — `EmailNotifications` alanı zaten var)
- SMS (kullanıcı izin verdiyse — `SmsNotifications` alanı zaten var)
- Mobil push (MAUI uygulaması için — Firebase Cloud Messaging)

### Bildirim Tetikleyicileri

| Olay | Kanal | Öncelik |
|---|---|---|
| İlan oluşturuldu (onay bekleniyor) | Site içi + E-posta | Yüksek |
| İlan onaylandı | Site içi + E-posta | Yüksek |
| İlan reddedildi | Site içi + E-posta + Popup | Yüksek |
| İlana başvuru geldi | Site içi + E-posta | Yüksek |
| Jeton yüklendi | Site içi | Normal |
| İlk kayıt | E-posta (zaten var) | Normal |
| Uyarı alındı | Site içi + E-posta + **Popup** | Kritik |
| Ban yendi | Site içi + E-posta + **Popup** | Kritik |
| Mesaj geldi | Site içi | Normal |
| Vitrin süresi doldu | Site içi + E-posta | Normal |
| Şifre değiştirildi | E-posta | Güvenlik |

### Mobil Uygulama Etkilenir mi?

Bildirim sistemi tamamen sunucu tarafında. MAUI uygulaması:
- Site içi bildirimleri API'den çeker (mevcut HTTP pattern)
- Push bildirim için FCM token'ı API'ye kaydeder (tek seferlik)
- Sonrasında Firebase sunucudan push gönderir, MAUI alır

SharedUI veya MAUI'ye özel platform kodu sadece FCM token kaydı için gerekir.

---

## BÖLÜM 2 — Kullanılacak Teknolojiler

### 2.1 Site İçi Bildirimler — PostgreSQL + SignalR

Bildirimler `Notifications` tablosunda saklanır.
Gerçek zamanlı güncelleme için **ASP.NET Core SignalR** kullanılır.
Blazor Server zaten SignalR kullanıyor, ek kurulum yok.

Web'de: Blazor component `OnAfterRenderAsync`'te hub'a bağlanır.
MAUI'de: Aynı SignalR hub'ına bağlanır (platform farkı yok).

### 2.2 E-posta — Mevcut SmtpEmailService

`IEmailService` ve `SmtpEmailService` zaten projede var.
Yeni template'ler ekleyeceğiz, altyapıya dokunmuyoruz.

### 2.3 SMS — Netgsm veya Twilio

Türkiye için **Netgsm** en yaygın ve ucuz seçenek.
HTTP API ile çalışır, NuGet paketi gerekmez, direkt `HttpClient` ile.

Alternatif: Twilio (uluslararası, daha pahalı).

### 2.4 Mobil Push — Firebase Cloud Messaging (FCM)

**Neden FCM:**
- Android ve iOS için tek API
- MAUI'de `Plugin.Firebase.CloudMessaging` NuGet paketi ile entegre
- Ücretsiz (Google servisi)
- Sunucu tarafında `FirebaseAdmin` NuGet paketi ile gönderim

**Nasıl çalışır:**
1. MAUI uygulaması açılınca FCM'den token alır
2. Token API'ye `POST /api/account/fcm-token` ile kaydedilir
3. Sunucu bildirim göndereceğinde `FirebaseAdmin` ile FCM'e istek atar
4. FCM cihaza push gönderir

### 2.5 NuGet Paketleri

```
# API projesine:
dotnet add package FirebaseAdmin

# Worker projesine (zaten var, kontrol et):
# MailKit zaten var

# MAUI projesine:
dotnet add package Plugin.Firebase.CloudMessaging
```

### 2.6 Mevcut Altyapı (Değişmeyenler)

| Teknoloji | Kullanım |
|---|---|
| RabbitMQ + MassTransit | Bildirim event'leri Worker'a iletmek için |
| IEmailService | E-posta gönderimi |
| ILogService | Hata loglama |
| Redis | Okunmamış bildirim sayısı cache |

---

## BÖLÜM 3 — Proje Mimarisine Göre Nereye Ne Yazılacak

### 3.1 OzelDers.Data

**Yeni entity:**
`src/OzelDers.Data/Entities/Notification.cs`
- Id, UserId, Type (enum), Title, Message, IsRead, CreatedAt, ReadAt

**User.cs'e eklenecek:**
- `FcmToken` string? — mobil push için
- `Notifications` navigation property

**AppDbContext.cs:**
- `DbSet<Notification> Notifications`

**Migration:** `AddNotificationSystem`

### 3.2 OzelDers.Business

**Yeni interface:**
`src/OzelDers.Business/Interfaces/INotificationService.cs`
- `CreateAsync(userId, type, title, message)`
- `GetUnreadCountAsync(userId)`
- `GetUserNotificationsAsync(userId, page)`
- `MarkAsReadAsync(notificationId, userId)`
- `MarkAllAsReadAsync(userId)`

**Yeni servis:**
`src/OzelDers.Business/Services/NotificationManager.cs`
- Hata kodları NM-001..NM-005
- `_logService.LogFunctionErrorAsync` catch bloklarında

**Yeni SMS servisi:**
`src/OzelDers.Business/Infrastructure/Sms/NetgsmSmsService.cs`
- `ISmsService` interface
- HttpClient ile Netgsm API

**Yeni event'ler:**
`src/OzelDers.Business/Events/NotificationEvents.cs`
- `SendNotificationEvent` — Worker'a gönderilecek

**DependencyInjection.cs:**
- `INotificationService → NotificationManager`
- `ISmsService → NetgsmSmsService`

### 3.3 OzelDers.Worker

**Yeni consumer:**
`src/OzelDers.Worker/Consumers/SendNotificationConsumer.cs`
- E-posta, SMS, FCM push gönderimini burada yapar
- Async, kullanıcıyı bekletmez

**Worker/Program.cs:**
- Consumer kaydı

### 3.4 OzelDers.API

**Yeni controller:**
`src/OzelDers.API/Controllers/NotificationsController.cs`
- `GET /api/notifications` — kullanıcının bildirimleri
- `GET /api/notifications/unread-count`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`
- `POST /api/account/fcm-token` — FCM token kaydet

**Yeni SignalR Hub:**
`src/OzelDers.API/Hubs/NotificationHub.cs`
- Authenticated bağlantı
- `SendNotification(userId, notification)` metodu

**AdminController.cs'e eklenecek:**
- `GET /api/admin/violations` — ihlal listesi
- `POST /api/admin/users/{id}/ban` — manuel ban
- `POST /api/admin/users/{id}/unban` — ban kaldır

**Program.cs:**
- SignalR servisi ve hub endpoint'i
- FirebaseAdmin başlatma

### 3.5 OzelDers.SharedUI

**Yeni component:**
`src/OzelDers.SharedUI/Components/NotificationBell.razor`
- Çan simgesi, okunmamış badge
- Dropdown ile son 5 bildirim
- SignalR ile gerçek zamanlı güncelleme

**Yeni sayfa:**
`src/OzelDers.SharedUI/Pages/Notifications.razor` (`/bildirimler`)
- Tüm bildirimler, sayfalı

**Yeni component:**
`src/OzelDers.SharedUI/Components/BanWarningPopup.razor`
- Ban/uyarı durumunda ekrana çıkan popup
- MainLayout'a eklenir, her sayfada kontrol eder

**NavMenu.razor:**
- `NotificationBell` component'i eklenir

**MainLayout.razor:**
- `BanWarningPopup` eklenir

### 3.6 OzelDers.App (MAUI)

**MauiProgram.cs:**
- FCM plugin kaydı
- Uygulama açılınca FCM token alıp API'ye gönder

**Değişmeyenler:** SharedUI sayfaları, tüm Blazor component'leri

---

## BÖLÜM 4 — Projede Kullanılan Kod Standartları

### 4.1 Hata Kodu Sistemi

```csharp
// NotificationManager.cs için:
private const string EC_CREATE   = "NM-001";
private const string EC_GETCOUNT = "NM-002";
private const string EC_GETLIST  = "NM-003";
private const string EC_READ     = "NM-004";
private const string EC_READALL  = "NM-005";
```

### 4.2 Hata Loglama

```csharp
catch (Exception ex)
{
    await _logService.LogFunctionErrorAsync(EC_CREATE, ex, new { userId, type });
    throw;
}
```

### 4.3 Controller Yapısı

AdminController'daki gibi:
- `[ApiController]`, `[Route("api/[controller]")]`
- `[Authorize]` veya `[Authorize(Roles = "Admin")]`
- `return Ok(...)` veya `return NotFound()`

### 4.4 Event Yapısı

```csharp
// Mevcut pattern (ListingEvents.cs'ten):
public class SendNotificationEvent
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public bool SendEmail { get; set; }
    public bool SendSms { get; set; }
    public bool SendPush { get; set; }
}
```

### 4.5 Consumer Yapısı

```csharp
// ListingCreatedConsumer.cs pattern'i:
public class SendNotificationConsumer : IConsumer<SendNotificationEvent>
{
    private readonly ILogger<SendNotificationConsumer> _logger;
    // constructor injection...

    public async Task Consume(ConsumeContext<SendNotificationEvent> context)
    {
        _logger.LogInformation("SendNotificationEvent alındı: {UserId}", context.Message.UserId);
        try { /* işlem */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hata: {UserId}", context.Message.UserId);
            throw; // MassTransit retry
        }
    }
}
```

---

## BÖLÜM 5 — Adım Adım Uygulama Rehberi

---

### ADIM 1 — Veritabanı: Notification Entity

`src/OzelDers.Data/Entities/Notification.cs` oluştur:

```csharp
namespace OzelDers.Data.Entities;

public class Notification
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Type { get; set; } = "";
    // Tipler: ListingPending, ListingApproved, ListingRejected,
    //         NewApplication, TokenLoaded, Warning, Ban,
    //         MessageReceived, VitrinExpired, PasswordChanged, Welcome
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ActionUrl { get; set; }  // Tıklayınca nereye gitsin
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

### ADIM 2 — Veritabanı: User.cs'e FcmToken Ekle

`src/OzelDers.Data/Entities/User.cs` dosyasını aç, moderasyon alanlarının altına ekle:

```csharp
// Push bildirim için Firebase token
public string? FcmToken { get; set; }
```

---

### ADIM 3 — Veritabanı: AppDbContext ve Migration

`src/OzelDers.Data/Context/AppDbContext.cs` dosyasına ekle:

```csharp
public DbSet<Notification> Notifications => Set<Notification>();
```

Terminalde migration oluştur:
```bash
dotnet ef migrations add AddNotificationSystem --project src/OzelDers.Data --startup-project src/OzelDers.API
dotnet ef database update --project src/OzelDers.Data --startup-project src/OzelDers.API
```

---

### ADIM 4 — Business: INotificationService Interface

`src/OzelDers.Business/Interfaces/INotificationService.cs` oluştur:

```csharp
using OzelDers.Business.DTOs;

namespace OzelDers.Business.Interfaces;

public interface INotificationService
{
    Task<Notification> CreateAsync(Guid userId, string type, string title,
        string message, string? actionUrl = null);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20);
    Task MarkAsReadAsync(int notificationId, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
}
```

`src/OzelDers.Business/DTOs/NotificationDto.cs` oluştur:

```csharp
namespace OzelDers.Business.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

### ADIM 5 — Business: NotificationManager

`src/OzelDers.Business/Services/NotificationManager.cs` oluştur:

```csharp
using OzelDers.Business.DTOs;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class NotificationManager : INotificationService
{
    private const string EC_CREATE   = "NM-001";
    private const string EC_GETCOUNT = "NM-002";
    private const string EC_GETLIST  = "NM-003";
    private const string EC_READ     = "NM-004";
    private const string EC_READALL  = "NM-005";

    private readonly IRepository<Notification> _repo;
    private readonly ILogService _logService;

    public NotificationManager(IRepository<Notification> repo, ILogService logService)
    {
        _repo = repo;
        _logService = logService;
    }

    public async Task<Notification> CreateAsync(Guid userId, string type, string title,
        string message, string? actionUrl = null)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId, Type = type, Title = title,
                Message = message, ActionUrl = actionUrl
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
            return all.Count;
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
                    Id = n.Id, Type = n.Type, Title = n.Title,
                    Message = n.Message, ActionUrl = n.ActionUrl,
                    IsRead = n.IsRead, CreatedAt = n.CreatedAt
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
            var n = await _repo.GetByIdAsync(notificationId);
            if (n == null || n.UserId != userId) return;
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
            foreach (var n in unread) { n.IsRead = true; n.ReadAt = DateTime.UtcNow; _repo.Update(n); }
            await _repo.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            await _logService.LogFunctionErrorAsync(EC_READALL, ex, userId);
            throw;
        }
    }
}
```

---

### ADIM 6 — Business: SendNotificationEvent

`src/OzelDers.Business/Events/NotificationEvents.cs` oluştur:

```csharp
namespace OzelDers.Business.Events;

public class SendNotificationEvent
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ActionUrl { get; set; }
    public bool SendEmail { get; set; } = true;
    public bool SendSms { get; set; } = false;
    public bool SendPush { get; set; } = true;
    // E-posta için kullanıcı bilgisi (DB'ye gitmemek için)
    public string? UserEmail { get; set; }
    public string? UserPhone { get; set; }
    public string? FcmToken { get; set; }
}
```

---

### ADIM 7 — Business: ISmsService ve NetgsmSmsService

`src/OzelDers.Business/Interfaces/ISmsService.cs` oluştur:

```csharp
namespace OzelDers.Business.Interfaces;

public interface ISmsService
{
    Task SendAsync(string phoneNumber, string message);
}
```

`src/OzelDers.Business/Infrastructure/Sms/NetgsmSmsService.cs` oluştur:

```csharp
using Microsoft.Extensions.Configuration;
using OzelDers.Business.Interfaces;

namespace OzelDers.Business.Infrastructure.Sms;

public class NetgsmSmsService : ISmsService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public NetgsmSmsService(IConfiguration config, IHttpClientFactory httpFactory)
    {
        _config = config;
        _http = httpFactory.CreateClient("Netgsm");
    }

    public async Task SendAsync(string phoneNumber, string message)
    {
        var user = _config["Netgsm:Username"] ?? "";
        var pass = _config["Netgsm:Password"] ?? "";
        var header = _config["Netgsm:Header"] ?? "OZELDERS";

        // Netgsm HTTP API
        var url = $"https://api.netgsm.com.tr/sms/send/get?" +
                  $"usercode={user}&password={pass}&gsmno={phoneNumber}" +
                  $"&message={Uri.EscapeDataString(message)}&msgheader={header}";

        await _http.GetAsync(url);
        // Hata yönetimi: response body "00" ise başarılı
    }
}
```

---

### ADIM 8 — Business: DependencyInjection.cs Güncelle

`src/OzelDers.Business/DependencyInjection.cs` dosyasına ekle:

```csharp
services.AddScoped<INotificationService, NotificationManager>();
services.AddScoped<ISmsService, OzelDers.Business.Infrastructure.Sms.NetgsmSmsService>();
services.AddHttpClient("Netgsm");
```

---

### ADIM 9 — API: FirebaseAdmin Paketi Ekle

```bash
dotnet add package FirebaseAdmin --project src/OzelDers.API/OzelDers.API.csproj
```

`src/OzelDers.API/Program.cs` dosyasına, `var builder = ...` satırından sonra ekle:

```csharp
// Firebase Admin başlatma (google-services.json dosyası gerekir)
var firebaseCredPath = builder.Configuration["Firebase:CredentialPath"];
if (!string.IsNullOrEmpty(firebaseCredPath) && File.Exists(firebaseCredPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredPath)
    });
}
```

`appsettings.json`'a ekle:
```json
"Firebase": {
  "CredentialPath": "firebase-credentials.json"
}
```

Firebase Console'dan `google-services.json` (Android) ve `GoogleService-Info.plist` (iOS) indir.
Sunucu tarafı için ayrıca `firebase-credentials.json` (service account key) indir.

---

### ADIM 10 — API: SignalR Hub

`src/OzelDers.API/Hubs/NotificationHub.cs` oluştur:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OzelDers.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnConnectedAsync();
    }
}
```

`src/OzelDers.API/Program.cs` dosyasına ekle:

```csharp
// builder.Services bölümüne:
builder.Services.AddSignalR();

// app.MapControllers() satırından önce:
app.MapHub<OzelDers.API.Hubs.NotificationHub>("/hubs/notifications");
```

---

### ADIM 11 — API: NotificationsController

`src/OzelDers.API/Controllers/NotificationsController.cs` oluştur:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Business.Interfaces;

namespace OzelDers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
        => _notificationService = notificationService;

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1)
        => Ok(await _notificationService.GetUserNotificationsAsync(GetUserId(), page));

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
        => Ok(new { count = await _notificationService.GetUnreadCountAsync(GetUserId()) });

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id, GetUserId());
        return Ok();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllAsReadAsync(GetUserId());
        return Ok();
    }
}
```

---

### ADIM 12 — API: FCM Token Endpoint

`src/OzelDers.API/Controllers/AccountController.cs` dosyasını aç (veya oluştur).
Mevcut endpoint'lerin yanına ekle:

```csharp
[HttpPost("fcm-token")]
[Authorize]
public async Task<IActionResult> SaveFcmToken([FromBody] FcmTokenDto dto)
{
    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var user = await _db.Users.FindAsync(userId);
    if (user == null) return NotFound();
    user.FcmToken = dto.Token;
    await _db.SaveChangesAsync();
    return Ok();
}

public record FcmTokenDto(string Token);
```

---

### ADIM 13 — API: AdminController'a Moderasyon Endpoint'leri Ekle

`src/OzelDers.API/Controllers/AdminController.cs` dosyasını aç, mevcut endpoint'lerin sonuna ekle:

```csharp
// ─── Moderasyon / İhlal Yönetimi ─────────────────────────────
[HttpGet("violations")]
public async Task<IActionResult> GetViolations(
    [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
{
    var items = await _db.ViolationLogs
        .Include(v => v.User)
        .OrderByDescending(v => v.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(v => new {
            v.Id, v.UserId,
            UserEmail = v.User.Email,
            UserName = v.User.FullName,
            v.ListingTitle, v.ViolationType,
            v.DetectedContent, v.DetectedBy,
            v.CreatedAt,
            v.User.ViolationCount,
            v.User.BannedUntil
        })
        .ToListAsync();

    var total = await _db.ViolationLogs.CountAsync();
    return Ok(new { total, page, pageSize, items });
}

[HttpPost("users/{userId}/ban")]
public async Task<IActionResult> BanUser(Guid userId, [FromBody] BanRequestDto dto)
{
    var user = await _db.Users.FindAsync(userId);
    if (user == null) return NotFound();

    user.BannedUntil = dto.IsPermanent ? DateTime.MaxValue : DateTime.UtcNow.AddDays(dto.Days);
    user.BanReason = $"Admin: {dto.Reason}";
    await _db.SaveChangesAsync();
    return Ok(new { message = "Ban uygulandı." });
}

[HttpPost("users/{userId}/unban")]
public async Task<IActionResult> UnbanUser(Guid userId)
{
    var user = await _db.Users.FindAsync(userId);
    if (user == null) return NotFound();

    user.BannedUntil = null;
    user.BanReason = null;
    await _db.SaveChangesAsync();
    return Ok(new { message = "Ban kaldırıldı." });
}

public record BanRequestDto(bool IsPermanent, int Days, string Reason);
```

---

### ADIM 14 — Worker: SendNotificationConsumer

`src/OzelDers.Worker/Consumers/SendNotificationConsumer.cs` oluştur:

```csharp
using FirebaseAdmin.Messaging;
using MassTransit;
using Microsoft.AspNetCore.SignalR.Client;
using OzelDers.Business.Events;
using OzelDers.Business.Interfaces;

namespace OzelDers.Worker.Consumers;

public class SendNotificationConsumer : IConsumer<SendNotificationEvent>
{
    private readonly ILogger<SendNotificationConsumer> _logger;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;

    public SendNotificationConsumer(
        ILogger<SendNotificationConsumer> logger,
        INotificationService notificationService,
        IEmailService emailService,
        ISmsService smsService)
    {
        _logger = logger;
        _notificationService = notificationService;
        _emailService = emailService;
        _smsService = smsService;
    }

    public async Task Consume(ConsumeContext<SendNotificationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("SendNotificationEvent alındı: {UserId} - {Type}", msg.UserId, msg.Type);

        try
        {
            // 1. Site içi bildirim (her zaman)
            await _notificationService.CreateAsync(
                msg.UserId, msg.Type, msg.Title, msg.Message, msg.ActionUrl);

            // 2. E-posta (kullanıcı izin verdiyse)
            if (msg.SendEmail && !string.IsNullOrEmpty(msg.UserEmail))
            {
                await _emailService.SendEmailAsync(msg.UserEmail, msg.Title,
                    $"<p>{msg.Message}</p>");
            }

            // 3. SMS (kullanıcı izin verdiyse)
            if (msg.SendSms && !string.IsNullOrEmpty(msg.UserPhone))
            {
                await _smsService.SendAsync(msg.UserPhone, msg.Message);
            }

            // 4. FCM Push (mobil)
            if (msg.SendPush && !string.IsNullOrEmpty(msg.FcmToken))
            {
                try
                {
                    await FirebaseMessaging.DefaultInstance.SendAsync(new Message
                    {
                        Token = msg.FcmToken,
                        Notification = new FirebaseAdmin.Messaging.Notification
                        {
                            Title = msg.Title,
                            Body = msg.Message
                        },
                        Data = new Dictionary<string, string>
                        {
                            ["type"] = msg.Type,
                            ["actionUrl"] = msg.ActionUrl ?? ""
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FCM push gönderilemedi: {UserId}", msg.UserId);
                    // Push başarısız olsa bile diğer kanallar etkilenmesin
                }
            }

            _logger.LogInformation("Bildirim gönderildi: {UserId} - {Type}", msg.UserId, msg.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bildirim gönderilemedi: {UserId}", msg.UserId);
            throw;
        }
    }
}
```

`src/OzelDers.Worker/Program.cs` dosyasına consumer kaydını ekle:

```csharp
x.AddConsumer<SendNotificationConsumer>();
```

---

### ADIM 15 — Worker: FirebaseAdmin Paketi

```bash
dotnet add package FirebaseAdmin --project src/OzelDers.Worker/OzelDers.Worker.csproj
```

`src/OzelDers.Worker/Program.cs` dosyasına ekle:

```csharp
// Firebase başlatma
var firebaseCredPath = builder.Configuration["Firebase:CredentialPath"];
if (!string.IsNullOrEmpty(firebaseCredPath) && File.Exists(firebaseCredPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredPath)
    });
}
```

---

### ADIM 16 — SharedUI: NotificationBell Component

`src/OzelDers.SharedUI/Components/NotificationBell.razor` oluştur:

```razor
@inject HttpClient Http
@inject NavigationManager Navigation
@implements IAsyncDisposable

<div class="notif-bell-wrapper">
    <button class="notif-bell-btn" @onclick="ToggleDropdown">
        🔔
        @if (unreadCount > 0)
        {
            <span class="notif-badge">@(unreadCount > 99 ? "99+" : unreadCount.ToString())</span>
        }
    </button>

    @if (isOpen)
    {
        <div class="notif-dropdown">
            <div class="notif-dropdown-header">
                <span>Bildirimler</span>
                @if (unreadCount > 0)
                {
                    <button class="notif-read-all" @onclick="MarkAllRead">Tümünü okundu işaretle</button>
                }
            </div>
            @if (notifications.Any())
            {
                @foreach (var n in notifications.Take(5))
                {
                    <div class="notif-item @(n.IsRead ? "" : "unread")" @onclick="() => OpenNotification(n)">
                        <div class="notif-icon">@GetIcon(n.Type)</div>
                        <div class="notif-content">
                            <div class="notif-title">@n.Title</div>
                            <div class="notif-msg">@n.Message</div>
                            <div class="notif-time">@GetTimeAgo(n.CreatedAt)</div>
                        </div>
                    </div>
                }
                <a href="/bildirimler" class="notif-see-all">Tümünü Gör →</a>
            }
            else
            {
                <div class="notif-empty">Bildirim yok</div>
            }
        </div>
    }
</div>

@code {
    private int unreadCount = 0;
    private List<NotificationDto> notifications = new();
    private bool isOpen = false;
    private System.Threading.Timer? _timer;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await LoadNotifications();
        // Her 30 saniyede bir güncelle
        _timer = new System.Threading.Timer(async _ =>
        {
            await LoadNotifications();
            await InvokeAsync(StateHasChanged);
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private async Task LoadNotifications()
    {
        try
        {
            var result = await Http.GetFromJsonAsync<List<NotificationDto>>("api/notifications");
            if (result != null) notifications = result;
            var countResult = await Http.GetFromJsonAsync<UnreadCountDto>("api/notifications/unread-count");
            if (countResult != null) unreadCount = countResult.Count;
        }
        catch { }
    }

    private void ToggleDropdown() => isOpen = !isOpen;

    private async Task MarkAllRead()
    {
        await Http.PutAsync("api/notifications/read-all", null);
        await LoadNotifications();
    }

    private async Task OpenNotification(NotificationDto n)
    {
        isOpen = false;
        await Http.PutAsync($"api/notifications/{n.Id}/read", null);
        if (!string.IsNullOrEmpty(n.ActionUrl))
            Navigation.NavigateTo(n.ActionUrl);
    }

    private static string GetIcon(string type) => type switch
    {
        "ListingApproved" => "✅",
        "ListingRejected" => "❌",
        "NewApplication"  => "📩",
        "TokenLoaded"     => "💰",
        "Warning"         => "⚠️",
        "Ban"             => "🚫",
        "MessageReceived" => "💬",
        _                 => "🔔"
    };

    private static string GetTimeAgo(DateTime dt)
    {
        var diff = DateTime.UtcNow - dt;
        if (diff.TotalMinutes < 1) return "Az önce";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} dk önce";
        if (diff.TotalDays < 1) return $"{(int)diff.TotalHours} saat önce";
        return $"{(int)diff.TotalDays} gün önce";
    }

    public async ValueTask DisposeAsync() => _timer?.Dispose();

    private record UnreadCountDto(int Count);
}
```

---

### ADIM 17 — SharedUI: BanWarningPopup Component

`src/OzelDers.SharedUI/Components/BanWarningPopup.razor` oluştur:

```razor
@inject HttpClient Http
@inject NavigationManager Navigation

@if (showBanPopup && banInfo != null)
{
    <div class="ban-popup-overlay">
        <div class="ban-popup">
            <div class="ban-popup-icon">🚫</div>
            <h2>Hesabınız Askıya Alındı</h2>
            <p>@banInfo.Message</p>
            @if (!banInfo.IsPermanent && banInfo.BannedUntil.HasValue)
            {
                <p class="ban-until">Ban bitiş: <strong>@banInfo.BannedUntil.Value.ToString("dd.MM.yyyy HH:mm")</strong></p>
            }
            <p class="ban-reason">Sebep: @banInfo.Reason</p>
            <button class="btn btn-secondary" @onclick="() => showBanPopup = false">Tamam</button>
        </div>
    </div>
}

@if (showWarningPopup)
{
    <div class="warning-popup-overlay">
        <div class="warning-popup">
            <div class="warning-popup-icon">⚠️</div>
            <h3>Uyarı Aldınız</h3>
            <p>İlan içeriğiniz platform kurallarına aykırı bulundu. Tekrar eden ihlallerde hesabınız askıya alınabilir.</p>
            <p class="warning-count">Toplam ihlal: <strong>@violationCount</strong></p>
            <button class="btn btn-primary" @onclick="() => showWarningPopup = false">Anladım</button>
        </div>
    </div>
}

@code {
    private bool showBanPopup = false;
    private bool showWarningPopup = false;
    private BanInfoDto? banInfo;
    private int violationCount = 0;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        try
        {
            // Son okunmamış ban/uyarı bildirimi var mı kontrol et
            var notifications = await Http.GetFromJsonAsync<List<NotificationDto>>("api/notifications");
            if (notifications == null) return;

            var banNotif = notifications.FirstOrDefault(n => n.Type == "Ban" && !n.IsRead);
            var warnNotif = notifications.FirstOrDefault(n => n.Type == "Warning" && !n.IsRead);

            if (banNotif != null)
            {
                // Ban bilgisini API'den al
                var me = await Http.GetFromJsonAsync<MeDto>("api/account/me");
                if (me?.BannedUntil != null)
                {
                    banInfo = new BanInfoDto(
                        me.BannedUntil == DateTime.MaxValue,
                        me.BannedUntil,
                        me.BanReason ?? "",
                        me.BannedUntil == DateTime.MaxValue
                            ? "Hesabınız kalıcı olarak askıya alınmıştır."
                            : $"Hesabınız {me.BannedUntil:dd.MM.yyyy} tarihine kadar askıya alınmıştır."
                    );
                    showBanPopup = true;
                    StateHasChanged();
                }
            }
            else if (warnNotif != null)
            {
                var me = await Http.GetFromJsonAsync<MeDto>("api/account/me");
                violationCount = me?.ViolationCount ?? 0;
                showWarningPopup = true;
                StateHasChanged();
            }
        }
        catch { }
    }

    private record BanInfoDto(bool IsPermanent, DateTime? BannedUntil, string Reason, string Message);
    private record MeDto(DateTime? BannedUntil, string? BanReason, int ViolationCount);
    private record NotificationDto(int Id, string Type, bool IsRead);
}
```

---

### ADIM 18 — NavMenu.razor'a NotificationBell Ekle

`src/OzelDers.SharedUI/Components/Layout/NavMenu.razor` dosyasını aç.
`<AuthorizeView>` içindeki `<Authorized>` bloğunda, mesaj ikonunun yanına ekle:

```razor
<AuthorizeView>
    <Authorized>
        <!-- Mevcut kodlar... -->
        <NotificationBell />  <!-- ← buraya ekle -->
        <a href="/panel/mesajlarim" ...>💬</a>
        <!-- ... -->
    </Authorized>
</AuthorizeView>
```

---

### ADIM 19 — MainLayout.razor'a BanWarningPopup Ekle

`src/OzelDers.SharedUI/Components/Layout/MainLayout.razor` dosyasını aç.
`<CookieConsent />` satırının altına ekle:

```razor
<AuthorizeView>
    <Authorized>
        <BanWarningPopup />
    </Authorized>
</AuthorizeView>
```

---

### ADIM 20 — Admin Paneli: Moderasyon Sayfası

`src/OzelDers.SharedUI/Pages/Admin/` klasöründe `Violations.razor` oluştur.
Mevcut admin sayfalarının (AdminDashboard.razor, UserManagement.razor) yapısını taklit et:
- Tablo: Kullanıcı, İhlal Türü, Tespit Eden, Tarih, Toplam İhlal, Ban Durumu
- Butonlar: "Ban Uygula", "Ban Kaldır"
- `GET /api/admin/violations` endpoint'inden veri çek

---

### ADIM 21 — MAUI: FCM Token Kaydı

`src/OzelDers.App/MauiProgram.cs` dosyasına ekle:

```csharp
// Plugin.Firebase.CloudMessaging NuGet paketi gerekir
// dotnet add package Plugin.Firebase.CloudMessaging --project src/OzelDers.App

builder.Services.AddSingleton<IFcmTokenService, FcmTokenService>();
```

`src/OzelDers.App/Services/FcmTokenService.cs` oluştur:

```csharp
public class FcmTokenService
{
    private readonly HttpClient _http;

    public FcmTokenService(HttpClient http) => _http = http;

    public async Task RegisterTokenAsync()
    {
        try
        {
            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
                await _http.PostAsJsonAsync("api/account/fcm-token", new { token });
        }
        catch { /* FCM yoksa sessizce geç */ }
    }
}
```

`App.xaml.cs` içinde uygulama başlarken çağır:
```csharp
await _fcmTokenService.RegisterTokenAsync();
```

---

### ADIM 22 — Build ve Test

```bash
dotnet build OzelDers.slnx
```

Test senaryoları:
1. İlan oluştur → "Onay Bekliyor" bildirimi çan simgesinde görünmeli
2. Admin ilanı onayla → "İlanınız Onaylandı" bildirimi gelmeli
3. Telefon numaralı ilan oluştur → "Uyarı" popup çıkmalı
4. 5 ihlal yap → "Ban" popup çıkmalı, API 403 dönmeli
5. Admin `/admin/violations` sayfasında ihlalleri görmeli
6. Admin ban uygula/kaldır butonları çalışmalı
