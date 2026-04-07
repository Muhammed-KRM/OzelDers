# Özel Ders İlan Platformu — Uygulama Planı (v4.0 — Blazor Hybrid)

## Projenin Genel Bakışı

**Amaç:** Özel ders veren öğretmenlerle öğrenci arayan kişileri buluşturan, **Lead & Vitrin Modeli** üzerine kurgulanmış, **Web + Mobil + Masaüstü** çoklu platform desteğine sahip modern bir ilan platformu.

**Temel Felsefe:** "Tek Kod, Çoklu Platform" — Tüm UI tek bir Razor Class Library'de toplanır; Web ve MAUI projeleri sadece boş kabuk (host) olarak çalışır.

> KRİTİK DEĞİŞİKLİK (v3 → v4): Angular tamamen kaldırıldı. Frontend artık **Blazor Web App** (tarayıcı) + **.NET MAUI Blazor Hybrid** (mobil/masaüstü) mimarisi ile inşa edilecektir.

---

## Mimari Genel Bakış

```
                    ┌─────────────────────────────────────────────────────────┐
                    │                    HOST PROJELERİ                       │
                    │                                                         │
                    │  ┌─────────────────┐     ┌──────────────────────────┐  │
                    │  │  OzelDers.Web   │     │    OzelDers.App          │  │
                    │  │  (Blazor Web)   │     │  (.NET MAUI Blazor)     │  │
                    │  │  Tarayıcıda     │     │  Android/iOS/Win/Mac    │  │
                    │  └───────┬─────────┘     └──────────┬───────────────┘  │
                    │          │                           │                   │
                    │          │      ┌────────────────┐   │                   │
                    │          └──────┤ OzelDers.      ├───┘                   │
                    │                 │ SharedUI (RCL) │                       │
                    │                 │ Tüm sayfalar & │                       │
                    │                 │ componentler   │                       │
                    │                 └────────────────┘                       │
                    └─────────────────────────────────────────────────────────┘

                    ┌─────────────────────────────────────────────────────────┐
                    │                  BACKEND N-TIER                         │
                    │                                                         │
                    │  ┌──────────────────────┐   ┌─────────────────────┐    │
                    │  │  OzelDers.API        │   │ OzelDers.Worker     │    │
                    │  │  (ASP.NET Core)      │   │ (Background Svc)   │    │
                    │  │  JWT + Swagger       │   │ Kuyruk dinleyici    │    │
                    │  └──────────┬───────────┘   └──────────┬──────────┘    │
                    │             │                           │               │
                    │             ▼                           │               │
                    │  ┌──────────────────────┐              │               │
                    │  │  OzelDers.Business   │◄─────────────┘               │
                    │  │  İş kuralları +      │                               │
                    │  │  Servisler + DTOs    │                               │
                    │  └──────────┬───────────┘                               │
                    │             │                                            │
                    │             ▼                                            │
                    │  ┌──────────────────────┐                               │
                    │  │  OzelDers.Data       │                               │
                    │  │  EF Core + Repos     │                               │
                    │  │  Entities + Context  │                               │
                    │  └──────────┬───────────┘                               │
                    │             │                                            │
                    └─────────────┼────────────────────────────────────────────┘
                                  │
                    ┌─────────────┼────────────────────────────────────────────┐
                    │             ▼           DATA LAYER                       │
                    │  ┌──────┐ ┌──────────┐ ┌───────┐ ┌──────────┐          │
                    │  │Postgre│ │Elastic   │ │Redis  │ │RabbitMQ  │          │
                    │  │SQL    │ │search    │ │Cache  │ │Queue     │          │
                    │  └──────┘ └──────────┘ └───────┘ └──────────┘          │
                    └─────────────────────────────────────────────────────────┘
```

### İletişim Stratejisi (API-First — Tek Doğru Kaynak)

```
  SharedUI'daki .razor dosyası:
  ┌─────────────────────────────────────────┐
  │  @inject IListingService ListingService │
  │  var items = await                      │
  │       ListingService.GetAllAsync();     │
  │  (Nereden geldiğini bilmez!)            │
  └────────────────┬────────────────────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
        ▼                     ▼
  Web Host (Program.cs)  MAUI Host (MauiProgram.cs)
  ┌──────────────────┐   ┌──────────────────────────┐
  │ AddScoped<        │   │ AddScoped<                │
  │  IListingService, │   │  IListingService,         │
  │  ListingApiService>│  │  ListingApiService>       │
  └────────┬─────────┘   └─────────┬────────────────┘
           │                       │
           ▼                       ▼
      ┌─────────────────────────────────┐
      │          Web API                │
      │   (Burada 'ListingManager'      │
      │    çalışır ve DB'ye gider)      │
      └─────────────────────────────────┘
```

**Neden API-First Yaklaşımı?**
- **Sıfır Kod Tekrarı:** Hem Web Host hem de MAUI Host aynı `ApiService` implementasyonunu kullanır.
- **Sıkı Güvenlik:** İstemci tarafı (Web de dahil) veritabanı bağlantı bilgilerini asla bilmez.
- **Tam Senkronizasyon:** İş kuralları sadece `OzelDers.Business` içinde tek bir yerde yazılır.

---

## Faz Haritası

| Faz | Kapsam | Tahmini Süre (Tek Kişi) |
|-----|--------|-------------|
| **1** | Altyapı & Backend (DB, Entity, Business, Repolar) | 3 hafta |
| **2** | API & Güvenlik (JWT, Controllers, Validation) | 2 hafta |
| **3** | SharedUI (RCL) (Tüm sayfalar ve UI bileşenleri) | 5 hafta |
| **4** | Entegrasyonlar (Jeton, ES, Redis, Worker, Payment vb.) | 3 hafta |
| **5** | Host Projeleri (Web yayını, MAUI Derleme & Test) | 3 hafta |
| **6** | Test & Bug Fix (E2E Testler, Donanım Testleri, Son Dokunuşlar) | 2 hafta |

> [!IMPORTANT]
> **Kurumsal Vizyon (Enterprise CV Masterpiece):**
> Bu projenin en büyük değerlerinden biri karmaşık mimari tasarımdır. Bu sebeple **Elasticsearch** (CQRS Okuma/Arama motoru), **Redis** (Dağıtık Önbellek) ve **RabbitMQ** (Mesaj Kuyruğu) gibi teknolojiler sistemin kalbini oluşturur ve sistem başlangıcından itibaren aktif olarak kullanılacaktır. Bu araçlar projenin vizyonu gereği iptal veya ertelemeye kapalıdır.

---

## FAZ 1: Solution Kurulumu + Data + Business

### Adım 1.1: Solution ve Proje Oluşturma

```powershell
cd d:\OZELDERS

# Solution oluştur
dotnet new sln -n OzelDers

# 1. Core & Backend
dotnet new classlib -n OzelDers.Data -o src/OzelDers.Data
dotnet new classlib -n OzelDers.Business -o src/OzelDers.Business
dotnet new webapi -n OzelDers.API -o src/OzelDers.API
dotnet new worker -n OzelDers.Worker -o src/OzelDers.Worker

# 2. Frontend (Merkezi UI)
dotnet new razorclasslib -n OzelDers.SharedUI -o src/OzelDers.SharedUI

# 3. Hosts
dotnet new blazor -n OzelDers.Web -o src/OzelDers.Web
dotnet new maui-blazor -n OzelDers.App -o src/OzelDers.App

# 4. Tests
dotnet new xunit -n OzelDers.UnitTests -o tests/OzelDers.UnitTests
dotnet new xunit -n OzelDers.IntegrationTests -o tests/OzelDers.IntegrationTests

# Solution'a ekle
dotnet sln add src/OzelDers.Data
dotnet sln add src/OzelDers.Business
dotnet sln add src/OzelDers.API
dotnet sln add src/OzelDers.Worker
dotnet sln add src/OzelDers.SharedUI
dotnet sln add src/OzelDers.Web
dotnet sln add src/OzelDers.App
dotnet sln add tests/OzelDers.UnitTests
dotnet sln add tests/OzelDers.IntegrationTests
```

### Adım 1.2: Proje Referansları

```powershell
# Business → Data
dotnet add src/OzelDers.Business reference src/OzelDers.Data

# API → Business
dotnet add src/OzelDers.API reference src/OzelDers.Business

# Worker → Business
dotnet add src/OzelDers.Worker reference src/OzelDers.Business

# Web Host → SharedUI referans alır (API üzerinden iletişim)
dotnet add src/OzelDers.Web reference src/OzelDers.SharedUI

# MAUI Host → SharedUI (API üzerinden)
dotnet add src/OzelDers.App reference src/OzelDers.SharedUI
```

### Adım 1.3: NuGet Paketleri

| Proje | Paket | Amaç |
|-------|-------|------|
| **Data** | EF Core, Npgsql.EF, EF Tools | PostgreSQL + ORM |
| **Business** | FluentValidation, AutoMapper | Validasyon + Mapping |
| **API** | Swashbuckle, JWT Bearer, Serilog | Swagger, Auth, Log |
| **Worker** | MassTransit.RabbitMQ | Kuyruk tüketimi |
| **SharedUI** | Microsoft.AspNetCore.Components.Web | Blazor components |
| **Web** | — (Blazor dahili) | SSR desteği |
| **App** | MAUI Blazor WebView | Hybrid rendering |

### Adım 1.4: Klasör Yapısı (Tam Ağaç)

```
d:\OZELDERS\
├── OzelDers.sln
│
├── src/
│   ├── OzelDers.Data/                          # VERİ KATMANI
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Listing.cs
│   │   │   ├── ListingImage.cs
│   │   │   ├── Message.cs
│   │   │   ├── Review.cs
│   │   │   ├── TokenTransaction.cs
│   │   │   ├── Branch.cs
│   │   │   ├── City.cs
│   │   │   ├── District.cs
│   │   │   ├── TokenPackage.cs
│   │   │   └── VitrinPackage.cs
│   │   ├── Enums/
│   │   │   ├── ListingType.cs
│   │   │   ├── ListingStatus.cs
│   │   │   ├── LessonType.cs
│   │   │   ├── TransactionType.cs
│   │   │   └── MessageStatus.cs
│   │   ├── Context/
│   │   │   └── AppDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── UserConfiguration.cs
│   │   │   ├── ListingConfiguration.cs
│   │   │   └── ... (her entity)
│   │   ├── Repositories/
│   │   │   ├── IRepository.cs
│   │   │   ├── GenericRepository.cs
│   │   │   ├── IListingRepository.cs
│   │   │   ├── ListingRepository.cs
│   │   │   ├── IUserRepository.cs
│   │   │   ├── UserRepository.cs
│   │   │   ├── IMessageRepository.cs
│   │   │   └── MessageRepository.cs
│   │   ├── Seeds/
│   │   │   ├── CitySeeder.cs
│   │   │   ├── DistrictSeeder.cs
│   │   │   ├── BranchSeeder.cs
│   │   │   └── TokenPackageSeeder.cs
│   │   └── Migrations/
│   │
│   ├── OzelDers.Business/                      # İŞ KATMANI
│   │   ├── Interfaces/
│   │   │   ├── IAuthService.cs
│   │   │   ├── IListingService.cs
│   │   │   ├── ISearchService.cs
│   │   │   ├── IMessageService.cs
│   │   │   ├── ITokenService.cs
│   │   │   ├── IVitrinService.cs
│   │   │   ├── IReviewService.cs
│   │   │   ├── ICacheService.cs
│   │   │   ├── IFileStorageService.cs
│   │   │   ├── IPaymentService.cs
│   │   │   └── IEmailService.cs
│   │   ├── Services/
│   │   │   ├── AuthManager.cs
│   │   │   ├── ListingManager.cs
│   │   │   ├── SearchManager.cs
│   │   │   ├── MessageManager.cs
│   │   │   ├── TokenManager.cs
│   │   │   ├── VitrinManager.cs
│   │   │   └── ReviewManager.cs
│   │   ├── DTOs/
│   │   │   ├── UserDto.cs, UserRegisterDto.cs, UserLoginDto.cs
│   │   │   ├── ListingDto.cs, ListingCreateDto.cs, ListingUpdateDto.cs
│   │   │   ├── SearchResultDto.cs, SearchFilterDto.cs
│   │   │   ├── MessageDto.cs
│   │   │   ├── TokenTransactionDto.cs, TokenPackageDto.cs
│   │   │   ├── ReviewDto.cs
│   │   │   ├── VitrinPackageDto.cs
│   │   │   └── AuthResultDto.cs
│   │   ├── Validators/
│   │   │   ├── UserRegisterValidator.cs
│   │   │   ├── ListingCreateValidator.cs
│   │   │   └── MessageSendValidator.cs
│   │   ├── Helpers/
│   │   │   ├── SlugHelper.cs
│   │   │   ├── PasswordHasher.cs
│   │   │   ├── JwtHelper.cs
│   │   │   └── AesEncryptionHelper.cs
│   │   ├── Exceptions/
│   │   │   ├── BusinessException.cs
│   │   │   ├── InsufficientTokenException.cs
│   │   │   ├── NotFoundException.cs
│   │   │   └── UnauthorizedException.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── OzelDers.API/                           # API KATMANI
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── ListingsController.cs
│   │   │   ├── MessagesController.cs
│   │   │   ├── TokensController.cs
│   │   │   ├── VitrinController.cs
│   │   │   ├── ReviewsController.cs
│   │   │   ├── BranchesController.cs
│   │   │   └── CitiesController.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── RateLimitingMiddleware.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── OzelDers.Worker/                        # ARKA PLAN İŞÇİSİ
│   │   ├── Consumers/
│   │   │   ├── ListingCreatedConsumer.cs
│   │   │   ├── ImageUploadedConsumer.cs
│   │   │   ├── VitrinExpiredConsumer.cs
│   │   │   ├── WelcomeTokenConsumer.cs
│   │   │   └── NotificationConsumer.cs
│   │   ├── Jobs/
│   │   │   ├── VitrinExpiryCheckJob.cs
│   │   │   └── StatisticsUpdateJob.cs
│   │   └── Program.cs
│   │
│   ├── OzelDers.SharedUI/                      # MERKEZİ UI (RCL)
│   │   ├── Pages/
│   │   │   ├── Home.razor (+.cs +.css)
│   │   │   ├── Search.razor (+.cs +.css)
│   │   │   ├── ListingDetail.razor (+.cs +.css)
│   │   │   ├── Auth/
│   │   │   │   ├── Login.razor (+.cs)
│   │   │   │   └── Register.razor (+.cs)
│   │   │   ├── UserPanel/
│   │   │   │   ├── MyListings.razor
│   │   │   │   ├── Messages.razor
│   │   │   │   ├── TokenBalance.razor
│   │   │   │   └── ProfileEdit.razor
│   │   │   ├── Admin/
│   │   │   │   ├── AdminDashboard.razor
│   │   │   │   ├── UserManagement.razor
│   │   │   │   ├── ListingManagement.razor
│   │   │   │   └── Reports.razor
│   │   │   └── Static/
│   │   │       ├── About.razor
│   │   │       ├── Privacy.razor
│   │   │       ├── Kvkk.razor
│   │   │       ├── Faq.razor
│   │   │       └── NotFound.razor
│   │   ├── Components/
│   │   │   ├── Layout/
│   │   │   │   ├── MainLayout.razor (+.css)
│   │   │   │   ├── DashboardLayout.razor
│   │   │   │   └── NavMenu.razor
│   │   │   ├── ListingCard.razor (+.css)
│   │   │   ├── SearchBar.razor (+.css)
│   │   │   ├── StarRating.razor
│   │   │   ├── Pagination.razor
│   │   │   ├── SkeletonLoader.razor
│   │   │   ├── ToastNotification.razor
│   │   │   ├── FilterPanel.razor
│   │   │   ├── CategoryCard.razor
│   │   │   ├── StatCounter.razor
│   │   │   └── PasswordStrength.razor
│   │   ├── wwwroot/
│   │   │   ├── css/
│   │   │   │   ├── variables.css
│   │   │   │   ├── reset.css
│   │   │   │   ├── typography.css
│   │   │   │   ├── animations.css
│   │   │   │   ├── utilities.css
│   │   │   │   └── app.css
│   │   │   ├── images/
│   │   │   ├── icons/
│   │   │   └── js/
│   │   │       └── interop.js
│   │   ├── _Imports.razor
│   │   └── Routes.razor
│   │
│   ├── OzelDers.Web/                           # WEB HOST
│   │   ├── Components/
│   │   │   └── App.razor
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   └── OzelDers.App/                           # MAUI HOST
│       ├── Services/
│       │   ├── ListingApiService.cs
│       │   ├── AuthApiService.cs
│       │   ├── MessageApiService.cs
│       │   ├── TokenApiService.cs
│       │   └── ApiSettings.cs
│       ├── Platforms/
│       │   ├── Android/
│       │   ├── iOS/
│       │   └── Windows/
│       ├── wwwroot/
│       │   └── index.html
│       ├── MainPage.xaml
│       ├── MauiProgram.cs
│       └── OzelDers.App.csproj
│
├── tests/
│   ├── OzelDers.UnitTests/
│   └── OzelDers.IntegrationTests/
│
├── docs/
├── docker-compose.dev.yml
└── .gitignore
```

### Adım 1.5: Data Katmanı — Entity'ler

**User.cs:**
```csharp
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsTeacherProfileComplete { get; set; } // Öğretmen olarak bilgilerini tamamlamış mı?
    public string? PhoneEncrypted { get; set; }       // AES-256
    public string? TCKNEncrypted { get; set; }         // AES-256
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public int TokenBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public ICollection<TokenTransaction> TokenTransactions { get; set; } = new List<TokenTransaction>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
```

**Listing.cs:**
```csharp
public class Listing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; } // İlanı veren (Öğretmen veya Öğrenci)
    public ListingType Type { get; set; } // ÖğretmenAylık, ÖğrenciArayış
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int HourlyPrice { get; set; }
    public LessonType LessonType { get; set; }
    public int BranchId { get; set; }
    public int DistrictId { get; set; }
    public bool IsVitrin { get; set; }
    public DateTime? VitrinExpiresAt { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Pending;
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Owner { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public District District { get; set; } = null!;
    public ICollection<ListingImage> Images { get; set; } = new List<ListingImage>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
```

Diğer entity'ler (Message, Review, TokenTransaction, Branch, City, District, ListingImage, TokenPackage, VitrinPackage) aynı pattern ile oluşturulacaktır.

### Adım 1.6: Data Katmanı — DbContext ve Repository

**AppDbContext.cs:**
```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<TokenTransaction> TokenTransactions => Set<TokenTransaction>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<District> Districts => Set<District>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

**IRepository.cs (Generic):**
```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveChangesAsync();
}
```

### Adım 1.7: Business Katmanı — Interface ve Manager

**IListingService.cs:**
```csharp
public interface IListingService
{
    Task<SearchResultDto> SearchAsync(SearchFilterDto filters);
    Task<ListingDto?> GetBySlugAsync(string slug);
    Task<List<ListingDto>> GetVitrinListingsAsync();
    Task<ListingDto> CreateAsync(ListingCreateDto dto, Guid userId);
    Task<ListingDto> UpdateAsync(Guid id, ListingUpdateDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<List<ListingDto>> GetMyListingsAsync(Guid userId);
}
```

**ListingManager.cs:**
```csharp
public class ListingManager : IListingService
{
    private readonly IListingRepository _repo;
    private readonly IValidator<ListingCreateDto> _validator;

    public ListingManager(IListingRepository repo, IValidator<ListingCreateDto> validator)
    {
        _repo = repo;
        _validator = validator;
    }

    public async Task<ListingDto> CreateAsync(ListingCreateDto dto, Guid teacherId)
    {
        // 1. FluentValidation ile doğrula
        // 2. Slug oluştur (SlugHelper)
        // 3. Entity'ye map et
        // 4. Repository ile kaydet
        // 5. DTO olarak döndür
    }
}
```

### Adım 1.8: Veritabanı Migration

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/OzelDers.Data --startup-project src/OzelDers.API
dotnet ef database update --project src/OzelDers.Data --startup-project src/OzelDers.API
```

---

## FAZ 2: API + SharedUI + Web/MAUI Hosts

### Adım 2.1: API Controller'ları

```csharp
[ApiController]
[Route("api/[controller]")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;

    [HttpGet("search")]
    public async Task<ActionResult<SearchResultDto>> Search([FromQuery] SearchFilterDto filters)
        => Ok(await _listingService.SearchAsync(filters));

    [HttpGet("{slug}")]
    public async Task<ActionResult<ListingDto>> GetBySlug(string slug)
    {
        var listing = await _listingService.GetBySlugAsync(slug);
        return listing is null ? NotFound() : Ok(listing);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ListingDto>> Create(ListingCreateDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _listingService.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetBySlug), new { slug = result.Slug }, result);
    }
}
```

### Adım 2.2: API Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddBusinessServices(); // Extension method

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ });

builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
    options.AddPolicy("StrictPolicy", b => b.WithOrigins("https://ozelders.com").AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("StrictPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### Adım 2.3: SharedUI Tasarım Sistemi

Renk Paleti (CSS Custom Properties):

| Token | Açık Tema | Karanlık Tema | Kullanım |
|-------|-----------|---------------|----------|
| --color-primary | #4F46E5 (Indigo) | #818CF8 | CTA butonları |
| --color-secondary | #0EA5E9 (Sky) | #38BDF8 | Badge'ler |
| --color-accent | #F59E0B (Amber) | #FBBF24 | Vitrin vurgusu |
| --color-surface | #FFFFFF | #1E1E2E | Kart yüzeyleri |
| --color-background | #F8FAFC | #11111B | Sayfa arka planı |

Tipografi: Outfit (başlık) + Inter (gövde) — Google Fonts

### Adım 2.4: SharedUI Sayfa Detayları

Tüm sayfalar .razor formatında, code-behind (.razor.cs) ve scoped CSS (.razor.css) ile:

**Home.razor — Ana Sayfa:**
1. Hero Section: Gradient bg, "Aradığın Öğretmeni Bul" başlığı, SearchBar componenti
2. Popüler Kategoriler: 8 adet CategoryCard grid
3. Vitrin İlanları: Horizontal carousel, amber border
4. Nasıl Çalışır: 3 adımlı infografik
5. İstatistikler: StatCounter componenti (animated)
6. CTA: "Öğretmen misiniz?" kayıt yönlendirme

**Search.razor — Arama Sonuçları:**
1. FilterPanel (sol, desktop) veya slide-in (mobil)
2. ListingCard grid (3/2/1 kolon responsive)
3. Vitrin ilanları üstte, amber glow
4. Pagination componenti
5. URL query params senkronizasyonu

**ListingDetail.razor — İlan Detay (SEO KRİTİK):**
1. Profil foto, isim, branş, konum, puan
2. Bilgi kartları (ücret, süre, tecrübe)
3. Hakkında açıklama
4. Mesaj Gönder CTA
5. Yorumlar + StarRating
6. JSON-LD Structured Data (SEO)

### Adım 2.5: SharedUI Routing

```razor
@* Routes.razor *@
<Router AppAssembly="@typeof(Routes).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
            <NotFound />
        </LayoutView>
    </NotFound>
</Router>
```

Sayfa route'ları:
- @page "/" → Home.razor
- @page "/arama" → Search.razor
- @page "/ilan/{Slug}" → ListingDetail.razor
- @page "/giris" → Login.razor
- @page "/kayit" → Register.razor
- @page "/panel/ilanlarim" → MyListings.razor
- @page "/panel/mesajlarim" → Messages.razor
- @page "/admin" → AdminDashboard.razor

### Adım 2.6: Web Host DI (API Üzerinden)

```csharp
// OzelDers.Web/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Tüm servisler HttpClient ile API'ye bağlanır (Mobil ile aynı mantık)
builder.Services.AddHttpClient("OzelDersApi", client =>
{
    client.BaseAddress = new Uri("https://api.ozelders.com");
});

builder.Services.AddScoped<IListingService, ListingApiService>();
builder.Services.AddScoped<IAuthService, AuthApiService>();
builder.Services.AddScoped<IMessageService, MessageApiService>();
builder.Services.AddScoped<ITokenService, TokenApiService>();

var app = builder.Build();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();
app.Run();
```

### Adım 2.7: MAUI Host DI (API Üzerinden)

```csharp
// OzelDers.App/MauiProgram.cs
var builder = MauiApp.CreateBuilder();
builder.UseMauiApp<App>();
builder.Services.AddMauiBlazorWebView();

builder.Services.AddHttpClient("OzelDersApi", client =>
{
    client.BaseAddress = new Uri("https://api.ozelders.com");
});

// API üzerinden implementasyonlar
builder.Services.AddScoped<IListingService, ListingApiService>();
builder.Services.AddScoped<IAuthService, AuthApiService>();
builder.Services.AddScoped<IMessageService, MessageApiService>();

return builder.Build();
```

### Adım 2.8: MAUI API Service Örneği

```csharp
// OzelDers.App/Services/ListingApiService.cs
public class ListingApiService : IListingService
{
    private readonly HttpClient _http;

    public ListingApiService(IHttpClientFactory factory)
        => _http = factory.CreateClient("OzelDersApi");

    public async Task<SearchResultDto> SearchAsync(SearchFilterDto filters)
    {
        var query = $"api/listings/search?branch={filters.Branch}&page={filters.Page}";
        return await _http.GetFromJsonAsync<SearchResultDto>(query);
    }

    public async Task<ListingDto?> GetBySlugAsync(string slug)
        => await _http.GetFromJsonAsync<ListingDto>($"api/listings/{slug}");
}
```

### Adım 2.9: SEO (Blazor Web App SSR)

```razor
@page "/ilan/{Slug}"
@inject IListingService ListingService

<PageTitle>@listing?.Title - Özel Ders | OzelDers.com</PageTitle>
<HeadContent>
    <meta name="description" content="@listing?.Description?.Substring(0, 160)" />
    <meta property="og:title" content="@listing?.Title" />
    <link rel="canonical" href="https://ozelders.com/ilan/@Slug" />
    <script type="application/ld+json">
    {
        "@@context": "https://schema.org",
        "@@type": "Service",
        "name": "@listing?.Title",
        "provider": { "@@type": "Person", "name": "@listing?.TeacherName" },
        "offers": { "@@type": "Offer", "price": "@listing?.HourlyPrice", "priceCurrency": "TRY" }
    }
    </script>
</HeadContent>
```

> Faz 3-8 detayları `implementation_plan_part2.md` dosyasında devam eder.
