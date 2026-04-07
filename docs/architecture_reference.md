# Özel Ders İlan Platformu — Blazor Hybrid Mimari Referans Dokümanı

## Bu Dokümanın Amacı

Bu doküman, projenin en kritik mimari kararını — **tek kod tabanından Web + Mobil + Masaüstü** 
çıkışını — detaylandırır. `implementation_plan_part1.md` ve `part2.md` dosyalarına ek olarak 
DI stratejisi, platform farklılıkları ve geliştirme sırasında dikkat edilecek kuralları açıklar.

---

## 1. Proje Bağımlılık Grafiği

```
  OzelDers.Data (en alt, sıfır bağımlılık)
       ▲
       │ referans
       │
  OzelDers.Business (Data'ya bağımlı)
       ▲                    ▲
       │                    │
  OzelDers.API         OzelDers.Worker
  (Business'a           (Business'a
   bağımlı)              bağımlı)


  OzelDers.SharedUI (bağımsız, sadece Interface'leri bilir)
       ▲                    ▲
       │                    │
  OzelDers.Web         OzelDers.App
  (SharedUI             (SharedUI
   referans alır,        referans,
   API üzerinden         API üzerinden
   iletişim)             iletişim)
```

> **ÖNEMLİ STRATEJİK DEĞİŞİKLİK:** Pazara çıkış süresini (Time to Market) hızlandırmak ve "Over-Engineering" yükünü azaltmak amacıyla **MAUI platformu Faz 1'de askıya alınmıştır.** Sistem (ES, Redis, RabbitMQ) tüm kurumsal gücüyle 100% web öncelikli olarak tamamlanacak, API-First yapısı korunduğu için ileride ihtiyaç duyulduğunda MAUI (veya Flutter/React Native) ile mobil platforma zahmetsizce geçilebilecektir.

**KRİTİK KURAL:** SharedUI projesi ASLA OzelDers.Data veya OzelDers.Business'ı referans almaz.
SharedUI sadece Business/Interfaces'deki sözleşmeleri (IListingService vb.) bilir.
Bu interface'ler SharedUI'ın kendi bünyesinde veya paylaşılan bir Contracts projesinde tutulur.

---

## 2. Interface Paylaşım Stratejisi

Interface'lerin SharedUI'dan erişilebilir olması gerekir. İki yol var:

### Seçenek A: Interface'ler Business İçinde (Basit)
```
OzelDers.Business/Interfaces/IListingService.cs
```
- Web Host: Business'ı zaten referans alır → Interface'lere erişir
- MAUI Host: Business'ı referans almaz → Problem!

**Çözüm:** MAUI projesine de Business referansı eklenir (sadece Interface için).
Bu durumda MAUI Data'ya da geçişli bağımlılık alır ama runtime'da kullanmaz.

### Seçenek B: Ayrı Contracts Projesi (Temiz)
```
OzelDers.Contracts/ (Class Library)
├── IListingService.cs
├── IAuthService.cs
├── DTOs/
│   ├── ListingDto.cs
│   ├── UserDto.cs
│   └── ...
└── OzelDers.Contracts.csproj
```
- SharedUI → Contracts referansı alır
- Business → Contracts referansı alır (Interface'leri implement eder)
- MAUI → Contracts referansı alır (ApiService'ler Interface'leri implement eder)
- Data'ya hiçbir geçişli bağımlılık oluşmaz

**ÖNERİ:** Proje büyüdükçe Seçenek B'ye geçilmesi önerilir. Başlangıçta Seçenek A yeterlidir.

---

## 3. Her Platform İçin DI Kayıt Tablosu

| Interface | Web Host (Program.cs) | MAUI Host (MauiProgram.cs) |
|-----------|----------------------|---------------------------|
| `IListingService` | `ListingApiService` (HttpClient) | `ListingApiService` (HttpClient) |
| `IAuthService` | `AuthApiService` (HttpClient) | `AuthApiService` (HttpClient) |
| `IMessageService` | `MessageApiService` (HttpClient) | `MessageApiService` (HttpClient) |
| `ITokenService` | `TokenApiService` (HttpClient) | `TokenApiService` (HttpClient) |
| `ISearchService` | `SearchApiService` (HttpClient) | `SearchApiService` (HttpClient) |
| `IVitrinService` | `VitrinApiService` (HttpClient) | `VitrinApiService` (HttpClient) |
| `IReviewService` | `ReviewApiService` (HttpClient) | `ReviewApiService` (HttpClient) |
| `IFilePickerService` | `WebFilePickerService` (JS Interop) | `MauiFilePickerService` (MediaPicker) |
| `ICacheService` | `RedisCacheService` | — (API cache'ler) |
| `IEmailService` | `SmtpEmailService` | — (API gönderir) |

---

## 4. Platforma Özel Kod Yazma Kuralları

### SharedUI'da Platform Kontrolü

```razor
@* Platformu inject ederek kontrol et *@
@inject IPlatformService Platform

@if (Platform.IsWeb)
{
    <div class="web-only-banner">Mobil uygulamamızı indirin!</div>
}

@if (Platform.IsMobile)
{
    <button @onclick="ScanQR">QR Kod Tara</button>
}
```

### Platform Servisi

```csharp
// Interface (SharedUI veya Contracts'ta)
public interface IPlatformService
{
    bool IsWeb { get; }
    bool IsMobile { get; }
    bool IsDesktop { get; }
    string PlatformName { get; } // "Web", "Android", "iOS", "Windows", "macOS"
}

// Web implementasyonu
public class WebPlatformService : IPlatformService
{
    public bool IsWeb => true;
    public bool IsMobile => false;
    public bool IsDesktop => false;
    public string PlatformName => "Web";
}

// MAUI implementasyonu
public class MauiPlatformService : IPlatformService
{
    public bool IsWeb => false;
    public bool IsMobile => DeviceInfo.Idiom == DeviceIdiom.Phone;
    public bool IsDesktop => DeviceInfo.Idiom == DeviceIdiom.Desktop;
    public string PlatformName => DeviceInfo.Platform.ToString();
}
```

---

## 5. SEO Stratejisi: Web vs MAUI

| Özellik | Blazor Web App (SSR) | MAUI Hybrid |
|---------|---------------------|-------------|
| Google indexleme | EVET (SSR ile HTML render) | HAYIR (native app) |
| `<title>` / `<meta>` | `<PageTitle>`, `<HeadContent>` | Kullanılmaz |
| JSON-LD | Evet, SSR ile render | Kullanılmaz |
| Sitemap | Evet, otomatik generation | — |
| robots.txt | Evet | — |
| Deep linking | URL routing | App Links / Universal Links |
| Open Graph | Evet | — |

**SONUÇ:** SEO sadece Web Host'u ilgilendirir. SharedUI'daki `<PageTitle>` ve `<HeadContent>` 
bileşenleri MAUI'de sessizce ignore edilir.

---

## 6. Geliştirme Ortamı Kurulumu

### Ön Koşullar

| Araç | Versiyon | Amaç |
|------|---------|------|
| Visual Studio 2022 | 17.8+ | IDE (MAUI workload dahil) |
| .NET SDK | 9.0+ | Runtime |
| Docker Desktop | Latest | PostgreSQL, ES, Redis, RabbitMQ |
| Android SDK | API 34+ | MAUI Android build |
| PostgreSQL | 16 | Veritabanı (Docker) |
| Node.js | 20+ | (Opsiyonel) Frontend tooling |

### Başlatma Sırası

```powershell
# 1. Altyapı servislerini başlat
cd d:\OZELDERS
docker-compose -f docker-compose.dev.yml up -d

# 2. Veritabanı migration
dotnet ef database update --project src/OzelDers.Data --startup-project src/OzelDers.API

# 3. API'yi başlat (MAUI için gerekli)
dotnet run --project src/OzelDers.API
# → https://localhost:5001 + Swagger UI

# 4. Web uygulamasını başlat
dotnet run --project src/OzelDers.Web
# → https://localhost:5000

# 5. MAUI'yi başlat (Windows)
dotnet run --project src/OzelDers.App -f net9.0-windows10.0.19041.0

# 6. MAUI'yi başlat (Android Emulator)
dotnet run --project src/OzelDers.App -f net9.0-android
```

### Debugging İpuçları

- **MAUI Android → Lokal API:** Emülatörden host makinaya `10.0.2.2` ile erişilir
- **MAUI Windows → Lokal API:** `localhost` direkt çalışır
- **Hot Reload:** Blazor `.razor` dosyalarında hot reload çalışır (hem Web hem MAUI)
- **SharedUI değişiklikleri:** Her iki host'u da etkiler (tek değişiklik, iki platform)

---

## 7. Test Stratejisi

| Katman | Test Türü | Araç | Kapsam |
|--------|-----------|------|--------|
| Data | Unit + Integration | xUnit + EF InMemory | Repository CRUD |
| Business | Unit | xUnit + Moq + FluentAssertions | Manager iş kuralları |
| API | Integration | xUnit + WebApplicationFactory | Controller endpoint'leri |
| SharedUI | Component | bUnit | Razor component render |
| E2E (Web) | UI | Playwright | Kritik akışlar |
| E2E (MAUI) | UI | Appium | Mobil akışlar (opsiyonel) |
| **Donanım Doğrulama** | Cihaz Testi | Gerçek Cihazlar | **MAUI'de Kritik:** Sadece emülatörde değil, özellikleri biten her sayfa/fonksiyon mutlaka gerçek Android/iOS cihazlarda test edilmelidir (Kamera, İzinler vb. emülatörde yanıltıcı olabilir). |

### bUnit Örneği (SharedUI Component Testi)

```csharp
[Fact]
public void ListingCard_Vitrin_GosterirBadge()
{
    // Arrange
    var listing = new ListingDto { IsVitrin = true, Title = "Test İlan" };
    var ctx = new TestContext();

    // Act
    var cut = ctx.RenderComponent<ListingCard>(
        parameters => parameters.Add(p => p.Listing, listing));

    // Assert
    cut.Find(".badge-vitrin").TextContent.Should().Contain("Öne Çıkan");
}
```

---

## 8. Özet: Angular vs Blazor Hybrid Karşılaştırma

| Kriter | Eski Plan (Angular) | Yeni Plan (Blazor Hybrid) |
|--------|---------------------|--------------------------|
| Dil | TypeScript + C# (iki dil) | Sadece C# (tek dil) |
| Platform | Sadece Web | Web + Android + iOS + Windows + macOS |
| SEO | Angular Universal (karmaşık) | Blazor SSR (native .NET) |
| Kod paylaşımı | Frontend/Backend ayrı | SharedUI ile %90+ paylaşım |
| API gereksinimi | Her zaman gerekli | **Her zaman gerekli (API-First)** |
| Performans (Web) | Client-side JS bundle | Server-side render (SignalR) |
| Performans (Mobil) | PWA / web view (yavaş) | Native WebView (hızlı) |
| Öğrenme eğrisi | Angular + .NET | Sadece .NET + Razor |
| DevOps | npm build + dotnet build | Sadece dotnet build |
| Real-time | SignalR ek kurulum | Blazor Server zaten SignalR tabanlı |
