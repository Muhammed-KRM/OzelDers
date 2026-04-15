# OzelDers — Moderasyon Sistemi Uygulama Rehberi

**Versiyon:** 1.0  
**Tarih:** Nisan 2026  
**Hedef Kitle:** Bu projeyi geliştiren developer (sen)

---

## BÖLÜM 1 — Amaç ve Genel Bakış

### Ne Yapmak İstiyoruz?

Kullanıcılar ilan başlığı ve açıklamasına telefon numarası, e-posta veya harici link yazarak
platformu atlayıp birbirleriyle direkt iletişim kurmaya çalışıyor. Bu durum jeton sistemini
devre dışı bırakıyor ve platformun gelir modelini doğrudan zedeliyor.

Bunu engellemek için üç katmanlı bir sistem kuruyoruz:

```
KATMAN 1 — Regex + Unicode Normalizasyon
  Nerede: API içinde, ilan kaydedilmeden önce
  Hız: <5ms (kullanıcı beklemez)
  Ne yakalar: Standart telefon formatları, e-posta, link, Unicode homoglyph bypass

KATMAN 2 — Ollama (Yerel LLM, few-shot prompting)
  Nerede: Worker servisi içinde, ilan kaydedildikten sonra async
  Hız: 2-4 saniye (arka planda, kullanıcı beklemez)
  Ne yakalar: "sıfır beş üç iki..." gibi yazıyla yazılmış bypass'lar

KATMAN 3 — Strike ve Ban Sistemi
  Nerede: Business katmanı + Middleware
  Ne yapar: İhlal sayısına göre uyarı/ban uygular
```

### Mobil Uygulama Etkilenir mi?

**Hayır.** Ollama ve tüm moderasyon mantığı sunucu tarafında çalışır.
MAUI uygulaması sadece API'ye HTTP isteği atar. API'nin arkasında ne olduğunu bilmez.
Web sitesi de aynı şekilde. İkisi için de sıfır değişiklik gerekir.

### Mevcut Durumla Fark

`ListingManager.cs` içinde zaten `PerformAutoModeration()` adında basit bir metot var.
Bu metot sadece temel regex yapıyor ve `ListingStatus.Pending` döndürüyor.
Biz bunu tamamen değiştirip genişleteceğiz.


---

## BÖLÜM 2 — Kullanılacak Teknolojiler

### 2.1 Regex (.NET Built-in)

Ekstra kurulum yok. `System.Text.RegularExpressions` zaten projede mevcut.
`ListingManager.cs`'te de kullanılıyor. Aynı şekilde devam edeceğiz.

### 2.2 Ollama (Yerel LLM Sunucusu)

**Nedir:** Açık kaynak, ücretsiz, Docker ile çalışan yerel LLM sunucusu.
**Neden:** Eğitim gerektirmez. Prompt içine 5-10 örnek yazarsın, model anlayıp sınıflandırır.
**Maliyet:** Sıfır. Hem geliştirme hem production'da ücretsiz.

**Kullanılacak Model:** `phi3-mini` (Microsoft, 3.8B parametre)
- CPU'da ~20 token/sn hız
- Türkçe'yi anlıyor
- RAM: ~4GB

**Docker'a nasıl eklenir:** `docker-compose.yml`'e bir servis olarak eklenir.
Worker servisi `http://ollama:11434` adresine HTTP isteği atar.

### 2.3 OllamaSharp (NuGet Paketi)

.NET'ten Ollama'ya bağlanmak için resmi C# kütüphanesi.

```
dotnet add package OllamaSharp
```

Bu paketi sadece `OzelDers.Worker` projesine ekleyeceğiz.
API veya SharedUI'a dokunmuyoruz.

### 2.4 Mevcut Altyapı (Değişmeyenler)

Aşağıdakiler zaten projede var, moderasyon sistemi bunları kullanacak:

| Teknoloji | Kullanım |
|---|---|
| PostgreSQL | ViolationLog tablosu, User ban alanları |
| Redis | Ban durumu cache'i (her istekte DB'ye gitme) |
| RabbitMQ + MassTransit | Worker'a async mesaj gönderme |
| ILogService | Hata loglama (mevcut standart) |
| ILogger<T> | Bilgi loglama (mevcut standart) |


---

## BÖLÜM 3 — Proje Mimarisine Göre Nereye Ne Yazılacak

Projenin katman yapısı: `Data → Business → API / Worker / SharedUI`

### 3.1 OzelDers.Data (Veritabanı Katmanı)

**Değişecek dosyalar:**

`src/OzelDers.Data/Entities/User.cs`
→ 4 yeni alan eklenecek: `ViolationCount`, `BannedUntil`, `LastViolationAt`, `BanReason`

**Yeni dosyalar:**

`src/OzelDers.Data/Entities/ViolationLog.cs`
→ Her ihlal kaydı için yeni entity

`src/OzelDers.Data/Migrations/`
→ `AddUserModerationFields` migration'ı
→ `AddViolationLogTable` migration'ı

`src/OzelDers.Data/Context/AppDbContext.cs`
→ `DbSet<ViolationLog>` eklenecek

### 3.2 OzelDers.Business (İş Katmanı)

**Yeni interface'ler:**

`src/OzelDers.Business/Interfaces/IModerationService.cs`
→ `CheckContent()` ve `AddStrikeAsync()` metodları

**Yeni servisler:**

`src/OzelDers.Business/Services/ModerationManager.cs`
→ Regex taraması burada (Katman 1)
→ Strike hesaplama burada

`src/OzelDers.Business/Infrastructure/Moderation/TurkishTextNormalizer.cs`
→ Unicode homoglyph temizleme
→ Türkçe rakam → sayı dönüşümü

**Değişecek dosyalar:**

`src/OzelDers.Business/Services/ListingManager.cs`
→ `PerformAutoModeration()` metodu `IModerationService.CheckContent()` ile değiştirilecek
→ `CreateAsync()` ve `UpdateAsync()` içinde çağrı güncellenecek

`src/OzelDers.Business/DependencyInjection.cs`
→ `IModerationService` → `ModerationManager` kaydı eklenecek

### 3.3 OzelDers.Worker (Arka Plan Servisi)

**Yeni dosyalar:**

`src/OzelDers.Worker/Services/OllamaService.cs`
→ Ollama API'sine HTTP isteği atan servis
→ Few-shot prompt burada tanımlanır

**Değişecek dosyalar:**

`src/OzelDers.Worker/Consumers/ListingCreatedConsumer.cs`
→ Mevcut consumer'a Ollama analizi eklenecek (Katman 2)
→ İhlal tespit edilirse `IModerationService.AddStrikeAsync()` çağrılacak

`src/OzelDers.Worker/Program.cs`
→ `OllamaService` DI kaydı eklenecek

### 3.4 OzelDers.API (API Katmanı)

**Yeni middleware:**

`src/OzelDers.API/Middleware/BanCheckMiddleware.cs`
→ Her istekte kullanıcının ban durumunu kontrol eder
→ Redis cache kullanır

**Değişecek dosyalar:**

`src/OzelDers.API/Program.cs`
→ `BanCheckMiddleware` pipeline'a eklenecek (Authentication'dan sonra)

### 3.5 docker-compose.yml

→ `ollama` servisi eklenecek

### 3.6 SharedUI / MAUI — DEĞİŞMEYECEK

Moderasyon tamamen sunucu tarafında. Mobil ve web'de sıfır değişiklik.


---

## BÖLÜM 4 — Projede Kullanılan Kod Standartları (Bunlara Uyacağız)

Projeyi inceledim. Şu standartlar var, moderasyon kodu da aynı şekilde yazılacak:

### 4.1 Hata Kodu Sistemi

Her Manager'ın başında hata kodları tanımlanıyor:
```csharp
// ListingManager.cs'te örnek:
private const string EC_CREATE = "LM-001";
private const string EC_SEARCH = "LM-008";
```

`ModerationManager.cs` için prefix `MM` olacak:
```csharp
private const string EC_CHECK  = "MM-001"; // CheckContent
private const string EC_STRIKE = "MM-002"; // AddStrikeAsync
```

### 4.2 Hata Loglama

Her `catch` bloğunda `_logService.LogFunctionErrorAsync()` çağrılıyor:
```csharp
catch (Exception ex)
{
    await _logService.LogFunctionErrorAsync(EC_CHECK, ex, inputData, userId);
    throw;
}
```
Moderasyon servisinde de aynı pattern kullanılacak.

### 4.3 ILogger Kullanımı

Worker consumer'larda `ILogger<T>` ile bilgi loglanıyor:
```csharp
_logger.LogInformation("ListingCreatedEvent alındı: {ListingId}", context.Message.ListingId);
_logger.LogError(ex, "Hata: {ListingId}", context.Message.ListingId);
```
`OllamaService` ve güncellenmiş consumer'da da aynı şekilde.

### 4.4 DI Kaydı Stili

`DependencyInjection.cs`'te `AddScoped` ile kayıt:
```csharp
services.AddScoped<IModerationService, ModerationManager>();
```

### 4.5 Consumer Yapısı

`ListingCreatedConsumer.cs` yapısı korunacak:
- Constructor injection
- `try/catch` ile hata yönetimi
- `_logger.LogInformation` ile başlangıç logu
- `_logger.LogError` ile hata logu
- `throw` ile exception'ı yeniden fırlat (MassTransit retry mekanizması çalışsın)

### 4.6 Async/Await

Tüm metodlar `async Task` veya `async Task<T>` dönüyor. Senkron metot yok.


---

## BÖLÜM 5 — Adım Adım Uygulama Rehberi

---

### ADIM 1 — Ollama'yı Docker'a Ekle

`docker-compose.yml` dosyasını aç, `rabbitmq` servisinin altına şunu ekle:

```yaml
  ollama:
    image: ollama/ollama:latest
    ports: ["11434:11434"]
    volumes: [ollama_data:/root/.ollama]
    environment:
      - OLLAMA_KEEP_ALIVE=24h
```

Aynı dosyanın en altındaki `volumes:` bölümüne de ekle:
```yaml
  ollama_data:
```

Sonra `worker` servisinin `depends_on` listesine `ollama` ekle:
```yaml
  worker:
    depends_on: [postgres, redis, rabbitmq, ollama]
```

Ardından terminalde şunu çalıştır (phi3-mini modelini indir):
```bash
docker-compose up -d ollama
docker exec -it ozelders-ollama-1 ollama pull phi3:mini
```

Model ~2.3GB, bir kez indirilir, `ollama_data` volume'unda kalır.

---

### ADIM 2 — OllamaSharp Paketini Worker'a Ekle

Terminalde:
```bash
cd src/OzelDers.Worker
dotnet add package OllamaSharp
```

---

### ADIM 3 — Veritabanı Değişiklikleri

**3a. User.cs'e yeni alanlar ekle**

`src/OzelDers.Data/Entities/User.cs` dosyasını aç, mevcut alanların sonuna ekle:

```csharp
// Moderasyon alanları
public int ViolationCount { get; set; } = 0;
public DateTime? BannedUntil { get; set; }
public DateTime? LastViolationAt { get; set; }
public string? BanReason { get; set; }
```

**3b. ViolationLog entity'si oluştur**

`src/OzelDers.Data/Entities/ViolationLog.cs` adında yeni dosya oluştur:

```csharp
namespace OzelDers.Data.Entities;

public class ViolationLog
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? ListingId { get; set; }
    public string ListingTitle { get; set; } = "";
    public string ViolationType { get; set; } = ""; // "Phone", "Email", "Link"
    public string DetectedContent { get; set; } = "";
    public string DetectedBy { get; set; } = "";    // "Regex", "Ollama", "Admin"
    public bool IsManual { get; set; } = false;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**3c. AppDbContext.cs'e DbSet ekle**

`src/OzelDers.Data/Context/AppDbContext.cs` dosyasını aç, diğer `DbSet`'lerin yanına ekle:

```csharp
public DbSet<ViolationLog> ViolationLogs => Set<ViolationLog>();
```

**3d. Migration oluştur**

Terminalde (projenin kök dizininde):
```bash
dotnet ef migrations add AddModerationFields --project src/OzelDers.Data --startup-project src/OzelDers.API
dotnet ef database update --project src/OzelDers.Data --startup-project src/OzelDers.API
```

---

### ADIM 4 — TurkishTextNormalizer Oluştur

`src/OzelDers.Business/Infrastructure/Moderation/` klasörü oluştur.
İçine `TurkishTextNormalizer.cs` dosyası oluştur:

```csharp
using System.Text;

namespace OzelDers.Business.Infrastructure.Moderation;

public static class TurkishTextNormalizer
{
    // Kiril ve Yunan harflerini Latin karşılıklarına çevirir
    private static readonly Dictionary<char, char> HomoglyphMap = new()
    {
        ['\u0430'] = 'a', ['\u0435'] = 'e', ['\u043E'] = 'o',
        ['\u0440'] = 'p', ['\u0441'] = 'c', ['\u0445'] = 'x',
        ['\u03BF'] = 'o', ['\u03B1'] = 'a',
    };

    // Türkçe rakam kelimeleri → rakam
    private static readonly Dictionary<string, string> NumberWords = new()
    {
        ["sıfır"] = "0", ["bir"] = "1", ["iki"] = "2", ["üç"] = "3",
        ["dört"] = "4", ["beş"] = "5", ["altı"] = "6", ["yedi"] = "7",
        ["sekiz"] = "8", ["dokuz"] = "9",
    };

    public static string Normalize(string text)
    {
        // 1. Homoglyph temizle
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
            sb.Append(HomoglyphMap.TryGetValue(c, out var clean) ? clean : c);
        var result = sb.ToString().ToLowerInvariant();

        // 2. Türkçe rakam kelimelerini sayıya çevir
        foreach (var (word, digit) in NumberWords)
            result = result.Replace(word, digit);

        return result;
    }
}
```

---

### ADIM 5 — IModerationService Interface'i Oluştur

`src/OzelDers.Business/Interfaces/IModerationService.cs` dosyası oluştur:

```csharp
namespace OzelDers.Business.Interfaces;

public interface IModerationService
{
    /// <summary>
    /// İlan içeriğini Regex ile tarar. Hızlı, sync çalışır.
    /// </summary>
    ModerationResult CheckContent(string title, string description);

    /// <summary>
    /// Kullanıcıya ihlal ekler, gerekirse ban uygular.
    /// </summary>
    Task AddStrikeAsync(Guid userId, Guid? listingId, string listingTitle,
        string violationType, string detectedContent, string detectedBy);
}

public record ModerationResult(bool IsViolation, string? ViolationType, string? Message)
{
    public static ModerationResult Clean() => new(false, null, null);
    public static ModerationResult Violation(string type, string msg) => new(true, type, msg);
}
```

---

### ADIM 6 — ModerationManager Oluştur

`src/OzelDers.Business/Services/ModerationManager.cs` dosyası oluştur:

```csharp
using System.Text.RegularExpressions;
using OzelDers.Business.Infrastructure.Moderation;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Entities;
using OzelDers.Data.Repositories;

namespace OzelDers.Business.Services;

public class ModerationManager : IModerationService
{
    private const string EC_CHECK  = "MM-001";
    private const string EC_STRIKE = "MM-002";

    private readonly IRepository<User> _userRepo;
    private readonly IRepository<ViolationLog> _violationRepo;
    private readonly ILogService _logService;

    // Compiled regex'ler — static, bir kez derlenir
    private static readonly Regex PhoneRegex = new(
        @"(\+90|0090|0)[\s\-\.\(\)\*\[\]\/\\]?[5][0-9][\s\-\.\(\)\*\[\]\/\\]?\d{2}[\s\-\.\(\)\*\[\]\/\\]?\d{3}[\s\-\.\(\)\*\[\]\/\\]?\d{2}[\s\-\.\(\)\*\[\]\/\\]?\d{2}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneSimpleRegex = new(
        @"\b0[5][0-9]\d{8}\b", RegexOptions.Compiled);

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
```

---

### ADIM 7 — DependencyInjection.cs'e Kayıt Ekle

`src/OzelDers.Business/DependencyInjection.cs` dosyasını aç.
Diğer `AddScoped` satırlarının yanına ekle:

```csharp
services.AddScoped<IModerationService, ModerationManager>();
```

---

### ADIM 8 — ListingManager.cs'i Güncelle

`src/OzelDers.Business/Services/ListingManager.cs` dosyasını aç.

**8a.** Constructor'a `IModerationService` ekle:
```csharp
private readonly IModerationService _moderationService;

public ListingManager(
    // ... mevcut parametreler ...
    IModerationService moderationService)
{
    // ...
    _moderationService = moderationService;
}
```

**8b.** `CreateAsync()` içindeki `PerformAutoModeration()` çağrısını değiştir:

Eski kod:
```csharp
Status = PerformAutoModeration(dto.Title, sanitizedDescription),
```

Yeni kod:
```csharp
Status = _moderationService.CheckContent(dto.Title, sanitizedDescription).IsViolation
    ? ListingStatus.Pending
    : ListingStatus.Active,
```

**8c.** `UpdateAsync()` içindeki moderasyon bloğunu da güncelle:

Eski:
```csharp
var modStatus = PerformAutoModeration(dto.Title, sanitizedDescription);
if (modStatus == ListingStatus.Active)
    listing.Status = dto.IsActive ? ListingStatus.Active : ListingStatus.Suspended;
else
    listing.Status = ListingStatus.Pending;
```

Yeni:
```csharp
var modResult = _moderationService.CheckContent(dto.Title, sanitizedDescription);
listing.Status = modResult.IsViolation
    ? ListingStatus.Pending
    : (dto.IsActive ? ListingStatus.Active : ListingStatus.Suspended);
```

**8d.** Artık kullanılmayan `PerformAutoModeration()` metodunu sil.


---

### ADIM 9 — OllamaService Oluştur (Worker)

`src/OzelDers.Worker/Services/OllamaService.cs` dosyası oluştur:

```csharp
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace OzelDers.Worker.Services;

public class OllamaService
{
    private readonly OllamaApiClient _client;
    private readonly ILogger<OllamaService> _logger;
    private const string Model = "phi3:mini";

    // Few-shot prompt — eğitim yok, sadece örnekler
    private const string SystemPrompt = """
        Sen bir içerik moderatörüsün. Türkçe ilan metinlerinde telefon numarası,
        e-posta adresi veya harici link olup olmadığını tespit ediyorsun.
        Sadece JSON formatında yanıt ver, başka hiçbir şey yazma.

        Örnekler:
        Metin: "Matematik dersi veriyorum, 10 yıl deneyimim var"
        Yanıt: {"violation": false}

        Metin: "0532 123 45 67 numaralı telefonu arayın"
        Yanıt: {"violation": true, "type": "phone"}

        Metin: "sıfır beş üç iki bir iki üç dört beş altı yedi"
        Yanıt: {"violation": true, "type": "phone"}

        Metin: "bilgi@gmail.com adresine yazın"
        Yanıt: {"violation": true, "type": "email"}

        Metin: "www.sitem.com adresimi ziyaret edin"
        Yanıt: {"violation": true, "type": "link"}

        Metin: "s-ı-f-ı-r b-e-ş üç iki..."
        Yanıt: {"violation": true, "type": "phone"}
        """;

    public OllamaService(IConfiguration config, ILogger<OllamaService> logger)
    {
        var ollamaUrl = config["Ollama:BaseUrl"] ?? "http://ollama:11434";
        _client = new OllamaApiClient(new Uri(ollamaUrl));
        _logger = logger;
    }

    public async Task<OllamaModerationResult> AnalyzeAsync(string title, string description,
        CancellationToken ct = default)
    {
        try
        {
            var userMessage = $"Metin: \"{title} {description}\"";

            var response = await _client.Chat(new ChatRequest
            {
                Model = Model,
                Messages =
                [
                    new Message { Role = "system", Content = SystemPrompt },
                    new Message { Role = "user",   Content = userMessage }
                ],
                Options = new() { Temperature = 0 } // Deterministik yanıt
            }, ct);

            var json = response?.Message?.Content?.Trim() ?? "{}";

            // JSON parse
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var violation = root.TryGetProperty("violation", out var v) && v.GetBoolean();
            var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;

            return new OllamaModerationResult(violation, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama analizi başarısız, temiz kabul ediliyor");
            // Ollama hata verirse ilanı engelleme — false negative tercih edilir
            return new OllamaModerationResult(false, null);
        }
    }
}

public record OllamaModerationResult(bool IsViolation, string? ViolationType);
```

---

### ADIM 10 — Worker Program.cs'e OllamaService Ekle

`src/OzelDers.Worker/Program.cs` dosyasını aç.
`builder.Services.AddMassTransit(...)` bloğundan önce ekle:

```csharp
builder.Services.AddSingleton<OzelDers.Worker.Services.OllamaService>();
```

---

### ADIM 11 — ListingCreatedConsumer'ı Güncelle

`src/OzelDers.Worker/Consumers/ListingCreatedConsumer.cs` dosyasını aç.

**11a.** Constructor'a `OllamaService` ve `IModerationService` ekle:

```csharp
private readonly OllamaService _ollamaService;
private readonly IModerationService _moderationService;

public ListingCreatedConsumer(
    ILogger<ListingCreatedConsumer> logger,
    IListingService listingService,
    ISearchService searchService,
    ICacheService cacheService,
    OllamaService ollamaService,
    IModerationService moderationService)
{
    // ... mevcut atamalar ...
    _ollamaService = ollamaService;
    _moderationService = moderationService;
}
```

**11b.** `Consume()` metoduna Ollama analizini ekle:

```csharp
public async Task Consume(ConsumeContext<ListingCreatedEvent> context)
{
    _logger.LogInformation("ListingCreatedEvent alındı: {ListingId}", context.Message.ListingId);
    try
    {
        var listing = await _listingService.GetByIdAsync(context.Message.ListingId);
        if (listing == null) return;

        // Katman 2: Ollama analizi (Regex'ten kaçan bypass'lar için)
        var ollamaResult = await _ollamaService.AnalyzeAsync(
            listing.Title, listing.Description, context.CancellationToken);

        if (ollamaResult.IsViolation)
        {
            _logger.LogWarning("Ollama ihlal tespit etti: {ListingId}, Tür: {Type}",
                listing.Id, ollamaResult.ViolationType);

            await _moderationService.AddStrikeAsync(
                listing.OwnerId,
                listing.Id,
                listing.Title,
                ollamaResult.ViolationType ?? "Unknown",
                $"{listing.Title} {listing.Description}",
                "Ollama");

            // İlanı Pending'e al (zaten Pending olabilir, emin olmak için)
            // Not: ListingService'e SetStatusAsync eklemen gerekebilir
        }

        // Mevcut işlemler devam eder
        await _searchService.IndexListingAsync(listing);
        await _cacheService.RemoveByPatternAsync("search:*");
        _logger.LogInformation("İlan işlendi: {ListingId}", listing.Id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "ListingCreatedEvent işlenirken hata: {ListingId}", context.Message.ListingId);
        throw;
    }
}
```

---

### ADIM 12 — BanCheckMiddleware Oluştur

`src/OzelDers.API/Middleware/BanCheckMiddleware.cs` dosyası oluştur:

```csharp
using System.Security.Claims;
using OzelDers.Business.Interfaces;

namespace OzelDers.API.Middleware;

public class BanCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly string[] AllowedPaths =
        ["/api/auth/login", "/api/auth/register", "/api/auth/refresh", "/health", "/swagger"];

    public BanCheckMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (AllowedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            await _next(context);
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<OzelDers.Data.Repositories.IRepository<OzelDers.Data.Entities.User>>();
        var user = await userRepo.GetByIdAsync(userId);

        if (user?.BannedUntil != null && user.BannedUntil > DateTime.UtcNow)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var isPermanent = user.BannedUntil == DateTime.MaxValue;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "account_banned",
                message = isPermanent
                    ? "Hesabınız kalıcı olarak askıya alınmıştır."
                    : $"Hesabınız {user.BannedUntil:dd.MM.yyyy HH:mm} tarihine kadar askıya alınmıştır.",
                bannedUntil = isPermanent ? (DateTime?)null : user.BannedUntil,
                isPermanent,
                reason = user.BanReason
            });
            return;
        }

        await _next(context);
    }
}
```

---

### ADIM 13 — Program.cs'e BanCheckMiddleware Ekle

`src/OzelDers.API/Program.cs` dosyasını aç.
`app.UseAuthentication()` satırından hemen sonra ekle:

```csharp
app.UseAuthentication();
app.UseMiddleware<BanCheckMiddleware>(); // ← buraya
app.UseAuthorization();
```

---

### ADIM 14 — appsettings'e Ollama URL Ekle

`src/OzelDers.Worker/appsettings.json` (veya `appsettings.Development.json`) dosyasına ekle:

```json
"Ollama": {
  "BaseUrl": "http://ollama:11434"
}
```

Development için `appsettings.Development.json`'a:
```json
"Ollama": {
  "BaseUrl": "http://localhost:11434"
}
```

---

### ADIM 15 — Build Al ve Test Et

```bash
dotnet build OzelDers.slnx
```

Hata yoksa:
```bash
docker-compose up -d
```

Test senaryosu:
1. Yeni ilan oluştur, başlığa `0532 123 45 67` yaz → Regex yakalamalı, `Status=Pending` olmalı
2. Yeni ilan oluştur, açıklamaya `sıfır beş üç iki...` yaz → Ollama yakalamalı
3. 5 ihlal yap → kullanıcı 7 gün ban almalı
4. Banlı kullanıcıyla API isteği at → 403 dönmeli

