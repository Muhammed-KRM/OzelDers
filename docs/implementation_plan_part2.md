#0 Özel Ders İlan Platformu — Uygulama Planı Bölüm 2 (Faz 3-8) — v4.0 Blazor Hybrid

> Bu doküman `implementation_plan_part1.md` (Faz 1-2) dosyasının devamıdır.

---

## FAZ 3: Elasticsearch + Redis Entegrasyonu

### Adım 3.1: Docker ile Altyapı Servislerini Ayağa Kaldırma

```yaml
# d:\OZELDERS\docker-compose.dev.yml
services:
  postgres:
    image: postgres:16
    ports: ["5432:5432"]
    environment:
      POSTGRES_DB: ozelders
      POSTGRES_USER: ozelders_user
      POSTGRES_PASSWORD: dev_password
    volumes: [pgdata:/var/lib/postgresql/data]

  elasticsearch:
    image: elasticsearch:8.12.0
    ports: ["9200:9200"]
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
      - ES_JAVA_OPTS=-Xms512m -Xmx512m

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

  rabbitmq:
    image: rabbitmq:3-management
    ports: ["5672:5672", "15672:15672"]

volumes:
  pgdata:
```

```powershell
cd d:\OZELDERS
docker-compose -f docker-compose.dev.yml up -d
```

### Adım 3.2: Business Katmanına ES/Redis Entegrasyonu

Business katmanındaki interface'ler zaten tanımlanmıştı. Şimdi implementasyonları ekliyoruz:

```
OzelDers.Business/
├── Interfaces/
│   ├── ISearchService.cs        # (Faz 1'de tanımlanmıştı)
│   └── ICacheService.cs         # (Faz 1'de tanımlanmıştı)
├── Infrastructure/              # [NEW] Dış servis implementasyonları
│   ├── ElasticsearchService.cs  # ISearchService implementasyonu
│   ├── RedisCacheService.cs     # ICacheService implementasyonu
│   └── RabbitMqEventBus.cs      # IEventBus implementasyonu
└── ...
```

> NOT: N-Tier mimaride Infrastructure kodu Business içinde veya ayrı bir katmanda olabilir.
> Burada basitlik adına Business içine alıyoruz. İleride ayrıştırılabilir.

### Adım 3.3: Elasticsearch Index Mapping

**Index şeması (`listings` index):**
```json
{
  "mappings": {
    "properties": {
      "id": { "type": "keyword" },
      "title": { "type": "text", "analyzer": "turkish" },
      "description": { "type": "text", "analyzer": "turkish" },
      "teacherName": { "type": "text", "analyzer": "turkish" },
      "branchSlug": { "type": "keyword" },
      "branchName": { "type": "text", "analyzer": "turkish" },
      "citySlug": { "type": "keyword" },
      "districtSlug": { "type": "keyword" },
      "hourlyPrice": { "type": "integer" },
      "lessonType": { "type": "keyword" },
      "isVitrin": { "type": "boolean" },
      "vitrinExpiresAt": { "type": "date" },
      "averageRating": { "type": "float" },
      "reviewCount": { "type": "integer" },
      "status": { "type": "keyword" },
      "createdAt": { "type": "date" },
      "location": { "type": "geo_point" }
    }
  },
  "settings": {
    "analysis": {
      "analyzer": {
        "turkish": {
          "tokenizer": "standard",
          "filter": ["lowercase", "turkish_stop", "turkish_stemmer"]
        }
      }
    }
  }
}
```

**Arama sorgusu akışı:**
1. `bool` query: `must` (keyword match) + `filter` (branch, city, price range, status=active)
2. `function_score`: `isVitrin=true` ilanlar +50 boost
3. `sort`: Vitrin önce, sonra relevance/price/rating/date
4. `highlight`: Eşleşen kelimeleri vurgula
5. `aggs`: Branş/şehir bazlı faceted search (filtre sayaçları)

### Adım 3.4: Redis Cache Stratejisi

| Anahtar Deseni | TTL | Veri |
|----------------|-----|------|
| `branches:all` | 1 saat | Tüm branş listesi |
| `cities:all` | 1 saat | Tüm şehir+ilçe listesi |
| `listing:{slug}` | 15 dk | Tekil ilan detay |
| `search:{hash}` | 5 dk | Arama sonucu (query hash) |
| `stats:home` | 30 dk | Ana sayfa istatistikleri |
| `vitrin:listings` | 10 dk | Vitrin ilan listesi |
| `user:token:{userId}` | — | Gerçek zamanlı jeton bakiye |

**Cache Invalidation Kuralları:**
- `ListingCreated/Updated/Deleted` → ilgili `listing:{slug}` sil + `search:*` sil + `stats:home` sil
- `VitrinPurchased/Expired` → `vitrin:listings` sil
- `TokenPurchased/Spent` → `user:token:{userId}` güncelle

### Adım 3.5: SearchManager Güncelleme

```csharp
// OzelDers.Business/Services/SearchManager.cs
public class SearchManager : ISearchService
{
    private readonly IElasticClient _elastic;
    private readonly ICacheService _cache;

    public async Task<SearchResultDto> SearchAsync(SearchFilterDto filters)
    {
        // 1. Cache kontrol (Redis)
        // DİKKAT: GetHashCode() .NET'te deterministic değildir, cache key için özel metin üretilir.
        var cacheKey = $"search:{filters.City ?? "none"}_{filters.Branch ?? "none"}_p{filters.Page}";
        var cached = await _cache.GetAsync<SearchResultDto>(cacheKey);
        if (cached != null) return cached;

        // 2. Elasticsearch sorgusu
        var response = await _elastic.SearchAsync<ListingIndexDocument>(s => s
            .Index("listings")
            .Query(q => q
                .Bool(b => b
                    .Must(m => m.MultiMatch(mm => mm
                        .Query(filters.Query)
                        .Fields(f => f.Field("title").Field("description").Field("teacherName"))
                    ))
                    .Filter(f =>
                        f.Term(t => t.Field("status").Value("active")),
                        filters.Branch != null ? f.Term(t => t.Field("branchSlug").Value(filters.Branch)) : null,
                        filters.City != null ? f.Term(t => t.Field("citySlug").Value(filters.City)) : null
                    )
                )
            )
            .Sort(sort => sort.Descending(d => d.IsVitrin).Descending(SortSpecialField.Score))
            .From((filters.Page - 1) * filters.PageSize)
            .Size(filters.PageSize)
        );

        // 3. DTO'ya map et
        var result = new SearchResultDto { /* ... */ };

        // 4. Cache'e yaz (5 dk)
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }
}
```

### Adım 3.6: Background Worker Event Consumers

```
OzelDers.Worker/
├── Consumers/
│   ├── ListingCreatedConsumer.cs     # → ES'e index, cache invalidate
│   ├── ListingUpdatedConsumer.cs     # → ES'te güncelle, cache invalidate
│   ├── ListingDeletedConsumer.cs     # → ES'ten sil, cache invalidate
│   ├── ImageUploadedConsumer.cs      # → Fotoğrafı boyutlandır + filigran bas
│   ├── VitrinExpiredConsumer.cs      # → Süresi biten vitrin ilanlarını kapat
│   ├── WelcomeTokenConsumer.cs       # → Yeni öğretmene 3 jeton tanımla
│   └── NotificationConsumer.cs       # → E-posta/SMS gönder
├── Jobs/
│   ├── VitrinExpiryCheckJob.cs       # Her saat: vitrin süresi kontrol
│   └── StatisticsUpdateJob.cs        # Her 30dk: istatistik cache güncelle
└── Program.cs
```

### Adım 3.7: Web & MAUI Host'larda SearchManager Entegrasyonu

Web Host'ta SearchApiService direkt API'ye (HttpClient ile) bağlanır (Kesin API-First Kuralı):
```csharp
// OzelDers.Web/Program.cs'e ekle:
builder.Services.AddScoped<ISearchService, SearchApiService>();
```

MAUI Host'ta SearchApiService API'ye istek atar:
```csharp
// OzelDers.App/Services/SearchApiService.cs
public class SearchApiService : ISearchService
{
    private readonly HttpClient _http;
    public async Task<SearchResultDto> SearchAsync(SearchFilterDto filters)
        => await _http.GetFromJsonAsync<SearchResultDto>($"api/listings/search?...");
}
```

---

## FAZ 4: Jeton Sistemi + Ödeme Entegrasyonu

### Adım 4.1: Jeton İş Akışı

```
  Öğretmen                 Frontend              API              İyzico           PostgreSQL
     │                        │                    │                 │                  │
     ├── "10 Jeton Al" ──────►│                    │                 │                  │
     │                        ├── POST /tokens ───►│                 │                  │
     │                        │   {packageId: 2}   │                 │                  │
     │                        │                    ├── Ödeme formu ─►│                  │
     │                        │                    │   (3D Secure)   │                  │
     │                        │◄── Form URL ───────┤                 │                  │
     │                        │                    │                 │                  │
     │   ┌── Kart bilgisi ───►│──────────────────────────────────────►│                  │
     │   │   (İyzico iframe)  │                    │                 │                  │
     │                        │                    │◄── Callback ────┤                  │
     │                        │                    │   (Başarılı)    │                  │
     │                        │                    ├── Balance += 10 ──────────────────►│
     │                        │                    ├── Transaction kayıt ──────────────►│
     │                        │◄── {balance: 13} ──┤                 │                  │
     │                        │                    │                 │                  │
```

### Adım 4.2: Jeton Paketleri (Seed Data)

| Paket | Jeton | Fiyat | Birim Fiyat | Badge |
|-------|-------|-------|-------------|-------|
| Başlangıç | 5 | 99 TL | 19.8 TL | — |
| **Popüler** | 10 | 149 TL | 14.9 TL | En Çok Tercih Edilen |
| Profesyonel | 25 | 299 TL | 11.96 TL | En Avantajlı |
| Kurumsal | 50 | 499 TL | 9.98 TL | Maksimum Tasarruf |

### Adım 4.3: Jeton Harcama Kuralları (Domain Logic)

```csharp
// OzelDers.Business/Services/TokenManager.cs
public class TokenManager : ITokenService
{
    // DİKKAT: Race Condition'ı (çifte harcama) önlemek için Entity Framework'te Optimistic Concurrency 
    // veya Redis üzerinden Distributed Lock uygulanmalıdır.
    public async Task<Result> SpendTokenAsync(Guid userId, int amount, string reason)
    {
        var user = await _userRepo.GetByIdAsync(userId);

        if (user.TokenBalance < amount)
            throw new InsufficientTokenException(user.TokenBalance, amount);

        user.TokenBalance -= amount;
        _userRepo.Update(user);

        await _transactionRepo.AddAsync(new TokenTransaction
        {
            UserId = userId,
            Amount = -amount,
            Type = TransactionType.Spend,
            Description = reason
        });

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }
}
```

- Mesaj açma maliyeti: **1 jeton**
- Aynı öğrenciden gelen mesajlar ilk açmadan sonra ücretsiz (aynı conversation)
- Hediye jeton: Yeni kayıt = 3, referans = +2 bonus

### Adım 4.4: Ödeme Sağlayıcı Entegrasyonu

```
OzelDers.Business/Infrastructure/Payment/
├── IyzicoPaymentService.cs     # IPaymentService implementasyonu
├── IyzicoOptions.cs            # API key, secret key, base URL
└── PaymentResult.cs            # Success/Failure + transaction ID
```

- 3D Secure zorunlu
- Callback URL: `POST /api/tokens/payment-callback`
- Idempotency: Aynı transactionId ile tekrar gelen callback işlenmez

### Adım 4.5: Blazor UI — Jeton Satın Al Sayfası

```razor
@* SharedUI/Pages/TeacherPanel/TokenBalance.razor *@
@page "/panel/jetonlarim"
@inject ITokenService TokenService

<div class="token-balance-hero">
    <h2>Jeton Bakiyeniz</h2>
    <span class="balance-number">@currentBalance</span>
</div>

<div class="packages-grid">
    @foreach (var package in packages)
    {
        <div class="package-card @(package.IsPopular ? "popular" : "")">
            @if (package.IsPopular)
            {
                <span class="badge-popular">En Çok Tercih Edilen</span>
            }
            <h3>@package.Name</h3>
            <p class="token-count">@package.TokenCount Jeton</p>
            <p class="price">@package.Price TL</p>
            <p class="unit-price">@((package.Price / package.TokenCount).ToString("F1")) TL / jeton</p>
            <button class="btn btn-primary" @onclick="() => PurchaseAsync(package.Id)">
                Satın Al
            </button>
        </div>
    }
</div>

<h3>Harcama Geçmişi</h3>
<table class="transaction-table">
    @foreach (var tx in transactions)
    {
        <tr>
            <td>@tx.CreatedAt.ToString("dd.MM.yyyy HH:mm")</td>
            <td>@tx.Description</td>
            <td class="@(tx.Amount > 0 ? "text-success" : "text-danger")">
                @(tx.Amount > 0 ? "+" : "")@tx.Amount jeton
            </td>
        </tr>
    }
</table>
```

---

## FAZ 5: Vitrin (Doping) + Mesajlaşma Sistemi

### Adım 5.1: Vitrin Paketleri

| Paket | Süre | Fiyat | Özellikler |
|-------|------|-------|-----------|
| Haftalık | 7 gün | 79 TL | Amber vurgu + üst sıra |
| Aylık | 30 gün | 249 TL | Amber vurgu + üst sıra + "Öne Çıkan" badge |
| Premium | 30 gün | 449 TL | Tümü + ana sayfa karuseline çıkma |

### Adım 5.2: Vitrin İş Kuralları

```csharp
// OzelDers.Business/Services/VitrinManager.cs
public class VitrinManager : IVitrinService
{
    public async Task<Result> PurchaseVitrinAsync(Guid listingId, int packageId, Guid userId)
    {
        // 1. Ödeme al (IPaymentService)
        // 2. Listing.IsVitrin = true
        // 3. Listing.VitrinExpiresAt = DateTime.UtcNow + paket süresi
        // 4. RabbitMQ: VitrinPurchasedEvent publish
        // 5. Worker: ES güncelle + Cache invalidate
    }
}
```

VitrinExpiryCheckJob (Worker - her saat):
1. VitrinExpiresAt < DateTime.UtcNow olan ilanları bul
2. IsVitrin = false yap
3. ES güncelle
4. Cache invalidate

### Adım 5.3: Çift Yönlü Mesajlaşma Sistemi

**Senaryo A: İlan Üzerinden Mesajlaşma (Normal)**
1. **Gönderen (A)**: İlan içine girer ve "Bu ilanla ilgileniyorum" der (ÜCRETSİZ).
2. **İlan Sahibi (B)**: Mesaj kutusunda bulanık bir mesaj görür. "1 Jeton Harca" butonuna basar.
3. Mesaj açılır ve **B** kişisi cevap verir. Artık bu sohbet ikisi için de ücretsizdir.

**Senaryo B: Direkt Teklif ve Reklam (Jetonlu Başlangıç)**
1. **Gönderen (A)**: Bir profili beğenir. Kendi ilanını seçerek ("Bak ben YKS Mentörüyüm") profiline **1 Jeton harcayarak** direkt reklam/teklif mesajı atar.
2. **Alıcı (B)**: Mesaj kutusuna girdiğinde mesajı **ZATEN AÇIK** olarak görür (Çünkü gönderen çoktan jeton ödemiştir). 
3. **B** kişisi ücretsiz şekilde teklifi değerlendirir veya reddeder.

**Mesaj Durumları ve Özellikleri:**
- `IsInitiatedWithToken`: Bu sohbetin başlama şeklini (İlan üzerinden mi, direkt jeton harcanarak mı) belirleyen boolean özellik (DB formatı).
- `Status`: `Sent` (Gönderildi), `Locked` (Jeton bekleniyor), `Unlocked` (Açıldı/Ücretsizleşti).

### Adım 5.4: Blazor UI — Mesajlar Sayfası

```razor
@* SharedUI/Pages/UserPanel/Messages.razor *@
@page "/panel/mesajlarim"
@inject IMessageService MessageService
@inject ITokenService TokenService

<div class="inbox">
    @foreach (var msg in messages)
    {
        <!-- Eğer mesaj karşı tarafın jetonuyla başlatıldıysa (Senaryo B) zaten Unlocked sayılır -->
        <div class="message-card @(msg.IsUnlocked || msg.IsInitiatedWithToken ? "" : "locked")">
            <div class="sender-info">
                <strong>@msg.SenderName</strong>
                <span class="badge">@(msg.IsInitiatedWithToken ? "Sana Özel Teklif 🌟" : "İlanına Gelen Mesaj")</span>
            </div>

            @if (msg.IsUnlocked || msg.IsInitiatedWithToken)
            {
                <p class="content">@msg.Content</p>
                <button class="btn btn-secondary" @onclick="() => ReplyAsync(msg.Id)">Ücretsiz Yanıtla</button>
            }
            else
            {
                <p class="content blurred">Bu içeriği görmek için kilidi açmalısınız.</p>
                <button class="btn btn-accent" @onclick="() => UnlockAsync(msg.Id)">
                    🔓 1 Jeton ile Aç (Bakiye: @tokenBalance)
                </button>
            }
        </div>
    }
</div>
```

---

## FAZ 6: Web Temelleri, Kullanıcı Paneli ve Dosya Yükleme

### Adım 6.1: Blazor Web Temel İnşası ve UI Tasarımı
Projenin görsel kimliğini yansıtacak 60-30-10 tasarım kurallarına uygun temel altyapı:
*   `Components/Layout/MainLayout.razor` ve `NavMenu.razor`
*   `wwwroot/css/index.css` (Glassmorphism ve yumuşak UI)
*   **Yetkilendirme:** `CustomAuthenticationStateProvider` yazılarak kullanıcıların giriş durumunu tarayıcıda yönetme.

### Adım 6.2: Herkese Açık (Public) Sayfalar
*   `Pages/Home.razor`: Karşılama, vitrin ve hızlı arama barı.
*   `Pages/Search.razor`: Gelişmiş filtrelemeli sonuç listesi.
*   `Pages/ListingDetail.razor`: İlan detay alanı ve ücretsiz/jetonlu mesaj atma butonları.
*   `Pages/Auth/Login.razor` ve `Pages/Auth/Register.razor`

### Adım 6.3: Yasal Sözleşmeler ve Zorunlu Sayfalar (Türkiye Mevzuatı)
Türkiye e-ticaret (6563) ve KVKK (6698) yasaları gereğince sitede olması zorunlu sayfalar:
*   `Pages/Legal/KullanimKosullari.razor`: Üyelik sözleşmesi.
*   `Pages/Legal/KVKK.razor`: Aydınlatma metni ve açık rıza beyanı.
*   `Pages/Legal/CerezPolitikasi.razor`: Çerez (cookie) bilgilendirmesi.
*   `Pages/Legal/MesafeliSatis.razor`: Jeton veya Vitrin satın alırken "Okudum ve kabul ediyorum" checkbox'ı ile onaylatılacak Ön Bilgilendirme Formu ve Mesafeli Satış Sözleşmesi.
*   `Pages/Legal/IptalIade.razor`: Cayma hakkı politikası sayfası.

### Adım 6.4: Kullanıcı Paneli (Unified Dashboard)
Öğretmen veya öğrenci fark etmeksizin profillerine erişen kullanıcı sayfaları:
*   `Pages/User/Dashboard.razor`: Kısa özet bakiye/istatistik.
*   `Pages/User/MyListings.razor`, `CreateListing.razor`, `EditListing.razor`: İlan yönetimi.
*   `Pages/User/Messages.razor`: Jeton mekanizmalı mesaj kutusu.
*   `Pages/User/Wallet.razor`: Jeton satın alma ve harcama ekranı.
*   `Pages/User/ProfileSettings.razor`: Kişisel bilgiler ve IBAN ekranı.

### Adım 6.5: Bildirim Sistemi
*   **E-posta Şablonları:** `welcome.html`, `new-message.html`, vb.
*   (Altyapı Faz 5'te SmtpEmailService ile tamamlanmıştır)

### Adım 6.6: Dosya Yükleme ve İşleme

```
  Kullanıcı         SharedUI          API/Business        RabbitMQ         Worker           Disk
     │                 │                   │                  │                │               │
     ├─ Foto seç ────►│                   │                  │                │               │
     │                 ├─ POST /upload ───►│                  │                │               │
     │                 │  (multipart)      │                  │                │               │
     │                 │                   ├── Dosya tipi     │                │               │
     │                 │                   │   kontrol        │                │               │
     │                 │                   │   (jpg/png/webp  │                │               │
     │                 │                   │    max 5MB)      │                │               │
     │                 │                   ├── Kaydet ────────────────────────────────────────►│
     │                 │                   ├── Event ────────►│                │               │
     │                 │◄─ {imageId} ──────┤                  │                │               │
     │                 │                   │                  ├── Consume ────►│               │
     │                 │                   │                  │                ├── Boyutlandır  │
     │                 │                   │                  │                │   200x200      │
     │                 │                   │                  │                │   600x400      │
     │                 │                   │                  │                │   1200x800     │
     │                 │                   │                  │                ├── WebP dönüş   │
     │                 │                   │                  │                ├── Filigran     │
     │                 │                   │                  │                ├── Kaydet ─────►│
     │                 │                   │                  │                ├── Orijinal sil►│
```

**Dosya İsimlendirme:** `{userId}/{listingId}/{size}_{guid}.webp`
**Kütüphane:** `ImageSharp` (.NET cross-platform image processing)

### Adım 6.7: MAUI Platformda Kamera Entegrasyonu

MAUI'de fotoğraf yüklerken platforma özel kamera/galeri erişimi:

```csharp
// OzelDers.App/Services/MauiFilePickerService.cs
public class MauiFilePickerService : IFilePickerService
{
    public async Task<FileResult?> PickPhotoAsync()
    {
        // MAUI'nin MediaPicker API'si
        var photo = await MediaPicker.Default.PickPhotoAsync();
        return photo;
    }

    public async Task<FileResult?> CapturePhotoAsync()
    {
        // Kamera ile çekim
        var photo = await MediaPicker.Default.CapturePhotoAsync();
        return photo;
    }
}
```

SharedUI'da Interface çağrılır:
```razor
@inject IFilePickerService FilePicker

<button @onclick="SelectPhoto">Fotoğraf Seç</button>

@code {
    async Task SelectPhoto()
    {
        var file = await FilePicker.PickPhotoAsync();
        // Web'de dosya seçici açılır, MAUI'de galeri açılır
    }
}
```

Web Host DI: `builder.Services.AddScoped<IFilePickerService, WebFilePickerService>();`
MAUI Host DI: `builder.Services.AddScoped<IFilePickerService, MauiFilePickerService>();`

---

## FAZ 7: Admin Paneli, Güvenlik Sertleştirme ve MAUI

### Adım 7.1: Admin Paneli (SharedUI İçinde)
Tüm sistem çalıştığında yöneticinin sistemi denetlemesi üzerine kurulur.
*   `SharedUI/Pages/Admin/AdminDashboard.razor`
*   `SharedUI/Pages/Admin/UserManagement.razor`
*   `SharedUI/Pages/Admin/ListingManagement.razor`
*   `SharedUI/Pages/Admin/Reports.razor`

### Adım 7.2: Güvenlik Katmanları

| Katman | Uygulama | Detay |
|--------|----------|-------|
| **Şifreleme** | AES-256-CBC | TCKN, IBAN şifreleme (EF Core Value Converter) |
| **Hashing** | BCrypt (work factor: 12) | Şifre hashleme |
| **JWT** | RS256 (asymmetric) | Access token (15 dk) + Refresh token (7 gün) |
| **XSS** | Content Security Policy | Blazor bileşen modeli zaten XSS-safe |
| **Rate Limit** | Microsoft.AspNetCore.RateLimiting | (Yerleşik kütüphane) Login: 5/dk, Search: 30/dk, Register: 3/saat |
| **Input Sanitize** | HtmlSanitizer | Kullanıcı girdilerini temizle |
| **CORS** | Whitelist | API: sadece bilinen origin'ler |
| **SQL Injection** | EF Core parameterized | Otomatik (raw SQL yok) |
| **Scraping** | Rate limit + Captcha | 100+ arama → CAPTCHA |

> NOT (Blazor Güvenlik Avantajı): Blazor Server/SSR modunda tüm C# kodu sunucuda çalışır, 
> istemciye gönderilmez. Bu, Angular/React'tan farklı olarak iş mantığının reverse-engineer
> edilememesi anlamına gelir. MAUI Hybrid'da ise kod derlenmiş binary olarak cihazda çalışır.

### Adım 7.3: JWT Akışı (Web vs MAUI)

**Web Host (Blazor Server):**
- JWT'ye ihtiyaç yok (zaten sunucuda çalışıyor)
- Authentication cookie tabanlı (`AuthenticationStateProvider`)
- `CascadingAuthenticationState` ile tüm bileşenlere yayılır

**MAUI Host (Blazor Hybrid):**
- API'ye her istekte JWT gönderir
- **KRİTİK:** Hassas veriler (JWT Token, Refresh Token vb.) asla düz metin olarak kaydedilmez. Kesinlikle `Microsoft.Maui.Storage.SecureStorage` API'si kullanılarak cihazın (iOS Keychain, Android Keystore, Windows Credential Locker) güvenli bölgesinde saklanır.
- Refresh token ile otomatik yenileme

```csharp
// OzelDers.App/Services/AuthApiService.cs
public class AuthApiService : IAuthService
{
    private readonly HttpClient _http;

    public async Task<AuthResultDto> LoginAsync(UserLoginDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", dto);
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();

        // Token'ı güvenli depoya kaydet
        await SecureStorage.Default.SetAsync("jwt_token", result.Token);
        await SecureStorage.Default.SetAsync("refresh_token", result.RefreshToken);

        return result;
    }
}

// HttpClient'a token ekleyen DelegatingHandler
public class AuthHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await SecureStorage.Default.GetAsync("jwt_token");
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, ct);
    }
}
```

### Adım 7.3: KVKK Uyumluluk Kontrol Listesi

- [ ] Aydınlatma metni sayfası (kayıt sırasında onay checkbox)
- [ ] Çerez politikası + cookie consent banner (Web Host)
- [ ] Kullanıcı veri silme: `DELETE /api/account`
- [ ] Veri taşıma: `GET /api/account/export` → JSON/CSV
- [ ] Kişisel veri envanteri dokümanı
- [ ] Veri saklama süreleri (hesap silme: 6 ay sonra tam silme)
- [ ] Açık rıza yönetimi (pazarlama e-postaları)

### Adım 7.4: Global Hata Yönetimi

```csharp
// OzelDers.API/Middleware/ExceptionHandlingMiddleware.cs
// Her hata → RFC 7807 ProblemDetails formatında:
// {
//   "type": "https://ozelders.com/errors/insufficient-tokens",
//   "title": "Yetersiz Jeton",
//   "status": 400,
//   "detail": "Bu işlem için 1 jeton gerekli, bakiyeniz: 0",
//   "traceId": "abc123"
// }
```

**Hata Kategorileri:**
- `400` — Validasyon, yetersiz jeton, iş kuralı ihlali
- `401` — JWT geçersiz/süresi dolmuş
- `403` — Yetkisiz erişim
- `404` — Bulunamadı
- `429` — Rate limit aşıldı
- `500` — Beklenmeyen hata

**Blazor Tarafında Hata Yakalama:**
```razor
@* SharedUI/Components/Layout/MainLayout.razor *@
<ErrorBoundary @ref="errorBoundary">
    <ChildContent>
        @Body
    </ChildContent>
    <ErrorContent Context="exception">
        <div class="error-panel">
            <h3>Bir hata oluştu</h3>
            <p>@exception.Message</p>
            <button @onclick="() => errorBoundary?.Recover()">Tekrar Dene</button>
        </div>
    </ErrorContent>
</ErrorBoundary>
```

---

## FAZ 8: SEO, İçerik Pazarlaması ve Dizin Sayfaları
Bir pazaryerinin kan damarı SEO'dur (Google'dan gelecek ücretsiz organik trafik).

### Adım 8.1: Statik Bilgi ve Güven Sayfaları
Kullanıcıların siteye güven duyması ve öğrenim işleyişini kavraması için:
*   `Pages/Marketing/NasilCalisir.razor`: İllüstrasyonlarla öğrenci ve öğretmen akışının anlatımı.
*   `Pages/Marketing/Hakkimizda.razor`: Kurumsal iletişim bilgileri.
*   `Pages/Marketing/SSS.razor`: Sıkça Sorulan Sorular (Fiyatlandırma, güvenlik, şikayetler).

### Adım 8.2: SEO Dizin (Directory) Sayfaları
Arka planda dinamik oluşan, on binlerce anahtar kelimeyi yakalayan kategori sayfaları:
*   **Şehir Bazlı:** `/{city}/ozel-ders-verenler` (Örn: `/istanbul/ozel-ders-verenler` -> Tüm İstanbul ilanlarını gösteren özel optimize edilmiş sayfa).
*   **Branş Bazlı:** `/{branch}/ozel-ders` (Örn: `/matematik/ozel-ders` -> Matematik ile ilgili genel makale ve yetenekli öğretmen listesi).
*   **Karma (Şehir + Branş):** `/{city}/{branch}-ozel-ders` (Örn: `/ankara/keman-ozel-ders`).

### Adım 8.3: Dinamik Site Haritası (Sitemap) & SSR
*   Bütün yayınlanan ilanların, öğretmenlerin ve kategorilerin `sitemap.xml` üzerinden Google botlarına otomatik servis edilmesi.
*   SEO sayfalarında Blazor Server'ın **Server-Side Rendering (SSR)** avantajıyla meta-tag (`<title>`, `<meta name="description">`) alanlarının dinamik atanması.
*   (Opsiyonel) Eğitici Blog içerikleri için statik markdown okuyucusu veya CMS entegrasyonu.


### Adım 7.5: Structured Logging (Serilog)

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.WithProperty("Application", "OzelDers.API")
    .Enrich.WithCorrelationId()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.Seq("http://localhost:5341")
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

---

## FAZ 9: Docker + CI/CD + Production

### Adım 9.1: Docker Containerization

```
d:\OZELDERS\
├── docker-compose.yml              # Production
├── docker-compose.dev.yml          # Development (sadece infra)
├── src/OzelDers.Web/Dockerfile     # Blazor Web App
├── src/OzelDers.API/Dockerfile     # ASP.NET Core API
├── src/OzelDers.Worker/Dockerfile  # Background Worker
└── nginx/
    └── nginx.conf                  # Reverse proxy
```

**Blazor Web App Dockerfile (Multi-stage):**
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore OzelDers.sln
RUN dotnet publish src/OzelDers.Web -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OzelDers.Web.dll"]
```

**API Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/OzelDers.API -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OzelDers.API.dll"]
```

**Docker Compose (Production):**
```yaml
services:
  web:
    build:
      context: .
      dockerfile: src/OzelDers.Web/Dockerfile
    ports: ["8080:8080"]
    depends_on: [postgres, redis]

  api:
    build:
      context: .
      dockerfile: src/OzelDers.API/Dockerfile
    ports: ["5001:8080"]
    depends_on: [postgres, redis, rabbitmq]

  worker:
    build:
      context: .
      dockerfile: src/OzelDers.Worker/Dockerfile
    depends_on: [postgres, elasticsearch, redis, rabbitmq]

  nginx:
    image: nginx:alpine
    ports: ["80:80", "443:443"]
    volumes: [./nginx/nginx.conf:/etc/nginx/nginx.conf]
    depends_on: [web, api]

  postgres:
    image: postgres:16
    volumes: [pgdata:/var/lib/postgresql/data]
    environment:
      POSTGRES_DB: ozelders
      POSTGRES_PASSWORD: ${DB_PASSWORD}

  elasticsearch:
    image: elasticsearch:8.12.0
    environment:
      - discovery.type=single-node
      - ES_JAVA_OPTS=-Xms1g -Xmx1g

  redis:
    image: redis:7-alpine

  rabbitmq:
    image: rabbitmq:3-management

volumes:
  pgdata:
```

**Nginx Config:**
```nginx
upstream blazor_web { server web:8080; }
upstream api_server { server api:8080; }

server {
    listen 80;
    server_name ozelders.com;

    # Blazor Web App (SSR)
    location / { proxy_pass http://blazor_web; }

    # API
    location /api/ { proxy_pass http://api_server; }

    # Static assets (long cache)
    location /_content/ {
        proxy_pass http://blazor_web;
        expires 30d;
        add_header Cache-Control "public, immutable";
    }
}
```

### Adım 9.2: MAUI App Build & Publish

```powershell
# Android APK
dotnet publish src/OzelDers.App -f net9.0-android -c Release

# Windows MSIX
dotnet publish src/OzelDers.App -f net9.0-windows10.0.19041.0 -c Release

# iOS (macOS gerekli)
dotnet publish src/OzelDers.App -f net9.0-ios -c Release
```

### Adım 9.3: CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/deploy.yml
name: Build & Deploy

on:
  push:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '9.0' }
      - run: dotnet restore
      - run: dotnet test --no-build

  deploy-web:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - run: docker-compose build web api worker
      - run: docker-compose push
      - run: ssh $SERVER "cd /app && docker-compose pull && docker-compose up -d"

  build-android:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - run: dotnet publish src/OzelDers.App -f net9.0-android -c Release
      # → APK artifact upload
```

### Adım 9.4: Monitoring & Alerting

| Araç | Amaç |
|------|-------|
| **Serilog + Seq** | Structured log toplama (dev) |
| **Prometheus + Grafana** | Metrik dashboard |
| **Uptime Kuma** | URL health check + Telegram alert |
| **PgHero** | PostgreSQL slow query analizi |

### Adım 9.5: Performance Hedefleri (SLA)

| Metrik | Hedef |
|--------|-------|
| Ana sayfa TTFB | < 200ms |
| Arama API | < 100ms (Elasticsearch) |
| İlan detay API | < 50ms (cache hit) |
| Blazor SSR ilk yüklenme | < 1.5s |
| MAUI uygulama açılış | < 2s |
| Uptime | >= 99.5% |
| P95 Response Time | < 500ms |

---

## ER Diyagramı (Tüm Tablolar)

```
  USER ─────────────┬──── LISTING ──────── LISTING_IMAGE
    │               │        │
    │               │        ├──── REVIEW
    │               │        │
    ├── TOKEN_TX    │        ├──── MESSAGE
    │               │        │
    │               │        ├──── BRANCH (hiyerarşik)
    │               │        │
    │               │        └──── DISTRICT ──── CITY
    │               │
    └───────────────┘
                    
  TOKEN_PACKAGE (seed data)
  VITRIN_PACKAGE (seed data)
```

**Tablolar ve Alanları:**

| Tablo | Anahtar Alanlar |
|-------|----------------|
| **User** | Id, Email, PasswordHash, FullName, Role, PhoneEncrypted, TCKNEncrypted, TokenBalance, IsActive, CreatedAt |
| **Listing** | Id, TeacherId, Title, Slug, Description, HourlyPrice, LessonType, BranchId, DistrictId, IsVitrin, VitrinExpiresAt, Status, AvgRating, CreatedAt |
| **ListingImage** | Id, ListingId, Url, SortOrder |
| **Message** | Id, SenderId, ReceiverId, ListingId, Content, IsUnlocked, TokenCost, Status, CreatedAt |
| **Review** | Id, ListingId, StudentId, Rating, Comment, CreatedAt |
| **TokenTransaction** | Id, UserId, Amount, Type, Description, PaymentRef, CreatedAt |
| **Branch** | Id, Name, Slug, ParentId (hiyerarşik) |
| **City** | Id, Name, Slug |
| **District** | Id, CityId, Name, Slug |
| **TokenPackage** | Id, Name, TokenCount, PriceTRY, IsPopular |
| **VitrinPackage** | Id, Name, DurationDays, PriceTRY |

---

## URL / Route Yapısı

| Sayfa | URL | SSR (Web) | MAUI |
|-------|-----|-----------|------|
| Ana Sayfa | `/` | Evet | Evet |
| Arama | `/arama?brans=X&sehir=Y` | Evet | Evet |
| İlan Detay | `/ilan/{slug}` | **Evet (Kritik)** | Evet |
| Giriş | `/giris` | Hayır | Evet |
| Kayıt | `/kayit` | Hayır | Evet |
| Panel | `/panel/*` | Hayır | Evet |
| Admin | `/admin/*` | Hayır | Hayır (Web only) |
| Nasıl Çalışır | `/nasil-calisir` | Evet | Evet |
| Hakkımızda | `/hakkimizda` | Evet | Evet |
| KVKK | `/kvkk` | Evet | Evet |
| Kullanım K. | `/kullanim-kosullari`| Evet | Evet |
| Gizlilik | `/gizlilik` | Evet | Evet |
| SSS | `/sss` | Evet | Evet |
| SEO Şehir Dizin | `/{city}/ozel-ders-verenler` | Evet | Hayır |
| SEO Branş Dizin | `/{branch}/ozel-ders` | Evet | Hayır |

---

## Verification Plan

### Her Faz Sonunda Yapılacak Kontroller

| Faz | Test | Komut/Yöntem |
|-----|------|-------------|
| 1 | Solution build başarılı | `dotnet build OzelDers.sln` |
| 1 | Unit testler geçiyor | `dotnet test` |
| 1 | EF Migration başarılı | `dotnet ef database update` |
| 2 | API Swagger çalışıyor | `https://localhost:5001/swagger` |
| 2 | Blazor Web çalışıyor | `dotnet run --project src/OzelDers.Web` |
| 2 | MAUI Windows çalışıyor | `dotnet run --project src/OzelDers.App -f net9.0-windows` |
| 2 | SharedUI sayfaları render | Tarayıcıda tüm rotaları test |
| 3 | ES index çalışıyor | `curl localhost:9200/listings/_search` |
| 3 | Redis cache çalışıyor | `redis-cli GET branches:all` |
| 4 | Jeton satın alma | İyzico sandbox ile E2E test |
| 5 | Vitrin boost | Arama sonuçlarında vitrin üstte |
| 6 | Fotoğraf işleme | Upload → Worker → boyutlandırılmış dosya |
| 7 | Rate limit | 6. login denemesi → 429 |
| 9 | Docker deploy | `docker-compose up -d` → healthy |
| 9 | Android APK | `dotnet publish -f net9.0-android` başarılı |
