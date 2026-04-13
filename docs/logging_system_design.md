# OzelDers — Gelişmiş Loglama Sistemi Tasarım Dökümanı

## 1. Mevcut Durum ve Sorun

Şu an projede Serilog paketi kurulu ama `UseSerilog()` çağrısı yapılmamış. Yani aktif loglama yok, sadece .NET'in varsayılan konsol çıktısı var. Uygulama kapanınca her şey kayboluyor.

---

## 2. Hedef: Ne İstiyoruz?

İki ana log kategorisi:

| Kategori | Ne Tutar | Nerede |
|---|---|---|
| **Endpoint Log** | Her HTTP isteği: payload, response/hata, kullanıcı, zaman | `endpoint_logs` tablosu |
| **Fonksiyon Log** | Her servis fonksiyonu: hata kodu, gelen parametre, hata mesajı, kullanıcı | `function_logs` tablosu |

---

## 3. Fonksiyon Hata Kodu Sistemi: Eski Yöntem vs. Modern Yöntem

### 3.1 Eski Yöntem (Senin Anlattığın)
Her fonksiyona elle `007126` gibi sabit bir kod yazılıyor. Ctrl+F ile bulunuyor.

**Sorun:** Tamamen manuel. Yeni fonksiyon eklerken sayıyı takip etmek gerekiyor, unutulabiliyor, çakışabiliyor.

### 3.2 Modern Yöntem: `CallerMemberName` + `CallerFilePath` Attribute'ları

C#'ta `System.Runtime.CompilerServices` namespace'inde 3 özel attribute var:

```csharp
[CallerMemberName]  // Çağıran fonksiyonun adı → "CreateAsync"
[CallerFilePath]    // Dosyanın tam yolu → "...Services/ListingManager.cs"
[CallerLineNumber]  // Satır numarası → 47
```

Bu attribute'lar **derleme zamanında** otomatik doldurulur. Yani sen sadece şunu yazarsın:

```csharp
_logService.LogFunctionError(ex, dto);
// Derleyici otomatik olarak şunu gönderir:
// memberName = "CreateAsync"
// filePath = "C:/...Services/ListingManager.cs"
// lineNumber = 47
```

**Sonuç:** Dosya adından + fonksiyon adından otomatik bir kod üretilir. Örneğin:
- `ListingManager.CreateAsync` → `LM-001`
- `MessageManager.SendAsync` → `MM-001`

Ctrl+F ile `LM-001` aratınca direkt o fonksiyona gidilir.

### 3.3 Hibrit Yaklaşım (Önerilen)

İkisinin en iyisini birleştiriyoruz:
- **Otomatik:** `CallerFilePath` + `CallerMemberName` ile dosya/fonksiyon adı otomatik alınır
- **Manuel prefix:** Her servis dosyasına bir sabit prefix kodu tanımlanır (LM, MM, TM vb.)
- **Sonuç:** `LM-CreateAsync` gibi okunabilir, aranabilir, çakışmayan kodlar

---

## 4. Mimari Karar: Nereye Yazılacak?

### Seçenek A: Ana PostgreSQL Veritabanına (Önerilen ✅)
- Ayrı tablolar: `endpoint_logs`, `function_logs`
- Avantaj: Ekstra altyapı yok, EF Core ile direkt sorgulanabilir, Admin panelinden görülebilir
- Dezavantaj: Yoğun trafikte DB'ye yük binebilir (çözüm: async fire-and-forget yazma)

### Seçenek B: Elasticsearch'e
- Zaten projede ES var
- Avantaj: Kibana ile görsel dashboard, full-text arama
- Dezavantaj: Log için ayrı index yönetimi, daha karmaşık

### Seçenek C: Ayrı Log Veritabanı (PostgreSQL)
- `ozelders_logs` adında ayrı bir DB
- Avantaj: Ana DB'yi kirletmez
- Dezavantaj: Ayrı connection string, migration yönetimi

**Karar: Seçenek A** — Ana DB'ye ayrı tablolar. Basit, yönetilebilir, Admin panelinden görülebilir. İleride ES'e taşımak da kolay.

---

## 5. Veritabanı Şeması

### 5.1 `endpoint_logs` Tablosu

```sql
CREATE TABLE endpoint_logs (
    id          BIGSERIAL PRIMARY KEY,
    trace_id    VARCHAR(64),          -- ASP.NET Core TraceIdentifier
    method      VARCHAR(10),          -- GET, POST, PUT, DELETE
    path        VARCHAR(500),         -- /api/listings/search
    query       TEXT,                 -- ?page=1&branch=matematik
    request_body TEXT,                -- JSON payload (hassas alanlar maskelenir)
    response_body TEXT,               -- Dönen JSON (hata durumunda hata detayı)
    status_code INT,                  -- 200, 400, 404, 500
    user_id     UUID,                 -- JWT'den çözülen kullanıcı ID (null = anonim)
    user_email  VARCHAR(256),         -- Hızlı arama için
    ip_address  VARCHAR(45),          -- IPv4/IPv6
    user_agent  VARCHAR(500),         -- Tarayıcı/cihaz bilgisi
    duration_ms INT,                  -- İstek süresi (ms)
    created_at  TIMESTAMPTZ DEFAULT NOW()
);
```

### 5.2 `function_logs` Tablosu

```sql
CREATE TABLE function_logs (
    id              BIGSERIAL PRIMARY KEY,
    error_code      VARCHAR(100),     -- "LM-CreateAsync" veya "LM-001"
    class_name      VARCHAR(200),     -- "ListingManager"
    method_name     VARCHAR(200),     -- "CreateAsync"
    file_path       VARCHAR(500),     -- Kaynak dosya yolu
    line_number     INT,              -- Hatanın satır numarası
    error_message   TEXT,             -- Exception.Message
    stack_trace     TEXT,             -- Exception.StackTrace
    input_type      VARCHAR(100),     -- "ListingCreateDto", "Guid", "String"
    input_value     TEXT,             -- JSON serialize edilmiş parametre
    user_id         UUID,             -- Mümkünse JWT'den
    trace_id        VARCHAR(64),      -- Endpoint log ile ilişkilendirmek için
    severity        VARCHAR(20),      -- "Error", "Warning", "Critical"
    created_at      TIMESTAMPTZ DEFAULT NOW()
);
```

---

## 6. Uygulama Adımları (Sırayla)

---

### ADIM 1: Entity'leri Oluştur

**Dosya:** `src/OzelDers.Data/Entities/EndpointLog.cs`

```csharp
namespace OzelDers.Data.Entities;

public class EndpointLog
{
    public long Id { get; set; }
    public string? TraceId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Query { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public int StatusCode { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Dosya:** `src/OzelDers.Data/Entities/FunctionLog.cs`

```csharp
namespace OzelDers.Data.Entities;

public class FunctionLog
{
    public long Id { get; set; }
    public string ErrorCode { get; set; } = string.Empty;   // "LM-CreateAsync"
    public string ClassName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int LineNumber { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public string? InputType { get; set; }
    public string? InputValue { get; set; }
    public Guid? UserId { get; set; }
    public string? TraceId { get; set; }
    public string Severity { get; set; } = "Error";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

### ADIM 2: AppDbContext'e Ekle

**Dosya:** `src/OzelDers.Data/Context/AppDbContext.cs`

Mevcut DbSet'lerin altına ekle:

```csharp
public DbSet<EndpointLog> EndpointLogs => Set<EndpointLog>();
public DbSet<FunctionLog> FunctionLogs => Set<FunctionLog>();
```

---

### ADIM 3: Log Servis Interface'i

**Dosya:** `src/OzelDers.Business/Interfaces/ILogService.cs`

```csharp
using System.Runtime.CompilerServices;

namespace OzelDers.Business.Interfaces;

public interface ILogService
{
    // Endpoint log — Middleware tarafından çağrılır
    Task LogEndpointAsync(EndpointLogEntry entry);

    // Fonksiyon hata logu — Her servis catch bloğunda çağrılır
    Task LogFunctionErrorAsync(
        string errorCode,
        Exception ex,
        object? inputData = null,
        Guid? userId = null,
        string? traceId = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0);
}

public class EndpointLogEntry
{
    public string? TraceId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Query { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public int StatusCode { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public int DurationMs { get; set; }
}
```

---

### ADIM 4: Log Servis Implementasyonu

**Dosya:** `src/OzelDers.Business/Services/LogManager.cs`

```csharp
using System.Runtime.CompilerServices;
using System.Text.Json;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Context;
using OzelDers.Data.Entities;

namespace OzelDers.Business.Services;

public class LogManager : ILogService
{
    private readonly AppDbContext _db;

    public LogManager(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogEndpointAsync(EndpointLogEntry entry)
    {
        try
        {
            var log = new EndpointLog
            {
                TraceId = entry.TraceId,
                Method = entry.Method,
                Path = entry.Path,
                Query = entry.Query,
                RequestBody = MaskSensitiveData(entry.RequestBody),
                ResponseBody = entry.ResponseBody,
                StatusCode = entry.StatusCode,
                UserId = entry.UserId,
                UserEmail = entry.UserEmail,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent,
                DurationMs = entry.DurationMs,
                CreatedAt = DateTime.UtcNow
            };

            _db.EndpointLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Log yazma hatası uygulamayı çökertmemeli — sessizce geç
        }
    }

    public async Task LogFunctionErrorAsync(
        string errorCode,
        Exception ex,
        object? inputData = null,
        Guid? userId = null,
        string? traceId = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        try
        {
            // Dosya adından class adını çıkar: "ListingManager.cs" → "ListingManager"
            var className = Path.GetFileNameWithoutExtension(filePath);

            var log = new FunctionLog
            {
                ErrorCode = errorCode,
                ClassName = className,
                MethodName = memberName,
                FilePath = filePath,
                LineNumber = lineNumber,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                InputType = inputData?.GetType().Name,
                InputValue = inputData is null ? null : JsonSerializer.Serialize(inputData),
                UserId = userId,
                TraceId = traceId,
                Severity = ex is OutOfMemoryException or StackOverflowException ? "Critical" : "Error",
                CreatedAt = DateTime.UtcNow
            };

            _db.FunctionLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Log yazma hatası uygulamayı çökertmemeli
        }
    }

    // Şifre, token gibi hassas alanları maskele
    private static string? MaskSensitiveData(string? json)
    {
        if (string.IsNullOrEmpty(json)) return json;
        
        // "password":"herhangi_bir_şey" → "password":"***"
        json = System.Text.RegularExpressions.Regex.Replace(
            json, 
            @"""(password|token|refreshToken|aesKey|ibanEncrypted|tcknEncrypted)""\s*:\s*""[^""]*""",
            @"""$1"":""***""",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return json;
    }
}
```

---

### ADIM 5: Endpoint Logging Middleware

**Dosya:** `src/OzelDers.API/Middleware/RequestResponseLoggingMiddleware.cs`

```csharp
using System.Security.Claims;
using System.Text;
using OzelDers.Business.Interfaces;

namespace OzelDers.API.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;

    // Loglanmayacak path'ler (swagger, static dosyalar)
    private static readonly string[] SkipPaths = ["/swagger", "/favicon", "/uploads", "/_blazor"];

    public RequestResponseLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogService logService)
    {
        var path = context.Request.Path.Value ?? "";

        // Swagger ve static dosyaları loglama
        if (SkipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Request body'yi oku (stream bir kez okunabilir, buffer'a al)
        context.Request.EnableBuffering();
        var requestBody = await ReadBodyAsync(context.Request.Body);
        context.Request.Body.Position = 0;

        // Response body'yi yakala
        var originalResponseBody = context.Response.Body;
        using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        await _next(context);

        stopwatch.Stop();

        // Response body'yi oku
        responseBuffer.Position = 0;
        var responseBody = await new StreamReader(responseBuffer).ReadToEndAsync();
        responseBuffer.Position = 0;
        await responseBuffer.CopyToAsync(originalResponseBody);
        context.Response.Body = originalResponseBody;

        // Kullanıcı bilgisini JWT'den al
        Guid? userId = null;
        string? userEmail = null;
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var emailClaim = context.User.FindFirstValue(ClaimTypes.Email);
        if (Guid.TryParse(userIdClaim, out var parsedId)) userId = parsedId;
        userEmail = emailClaim;

        // Logu kaydet (fire-and-forget — isteği yavaşlatmaz)
        _ = logService.LogEndpointAsync(new EndpointLogEntry
        {
            TraceId = context.TraceIdentifier,
            Method = context.Request.Method,
            Path = path,
            Query = context.Request.QueryString.Value,
            RequestBody = requestBody,
            ResponseBody = responseBody.Length > 5000 ? responseBody[..5000] + "...[truncated]" : responseBody,
            StatusCode = context.Response.StatusCode,
            UserId = userId,
            UserEmail = userEmail,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            DurationMs = (int)stopwatch.ElapsedMilliseconds
        });
    }

    private static async Task<string?> ReadBodyAsync(Stream body)
    {
        if (!body.CanRead || body.Length == 0) return null;
        using var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
```

---

### ADIM 6: Hata Kodu Sabitleri (Her Servis Dosyasına)

Her servis dosyasının en üstüne bir `const` bölümü eklenecek. Bu sayede Ctrl+F ile aranabilir.

**Örnek — `ListingManager.cs` için:**

```csharp
// ═══════════════════════════════════════════════
// HATA KODLARI — ListingManager (Prefix: LM)
// ═══════════════════════════════════════════════
private const string EC_CREATE   = "LM-001";  // CreateAsync
private const string EC_UPDATE   = "LM-002";  // UpdateAsync
private const string EC_DELETE   = "LM-003";  // DeleteAsync
private const string EC_SEARCH   = "LM-004";  // SearchAsync
private const string EC_GETSLUG  = "LM-005";  // GetBySlugAsync
// ═══════════════════════════════════════════════
```

**Prefix Tablosu (Tüm Servisler):**

| Servis | Prefix |
|---|---|
| ListingManager | LM |
| MessageManager | MM |
| TokenManager | TM |
| AuthManager | AM |
| UserManager | UM |
| VitrinManager | VM |
| ReviewManager | RM |
| AdminManager | ADM |
| SettingManager | SM |

---

### ADIM 7: Servislere Try-Catch Ekleme

**Örnek — `ListingManager.CreateAsync`:**

```csharp
public async Task<ListingDto> CreateAsync(ListingCreateDto dto, Guid userId)
{
    try
    {
        // ... mevcut kod ...
    }
    catch (BusinessException)
    {
        throw; // Business exception'ları yeniden fırlat, loglama
    }
    catch (Exception ex)
    {
        await _logService.LogFunctionErrorAsync(
            errorCode: EC_CREATE,
            ex: ex,
            inputData: dto,
            userId: userId
            // memberName, filePath, lineNumber otomatik doldurulur
        );
        throw; // Üst katmana ilet
    }
}
```

---

### ADIM 8: DependencyInjection'a Kaydet

**Dosya:** `src/OzelDers.Business/DependencyInjection.cs`

```csharp
services.AddScoped<ILogService, LogManager>();
```

---

### ADIM 9: API Program.cs'e Middleware Ekle

**Dosya:** `src/OzelDers.API/Program.cs`

`ExceptionHandlingMiddleware`'den hemen önce:

```csharp
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

---

### ADIM 10: Migration Oluştur

```bash
cd src/OzelDers.API
dotnet ef migrations add AddLoggingTables --project ../OzelDers.Data
dotnet ef database update
```

---

## 7. Kullanım Örneği (Tam Senaryo)

Bir kullanıcı `POST /api/listings` isteği atar:

1. `RequestResponseLoggingMiddleware` isteği yakalar, body'yi buffer'a alır
2. İstek `ListingsController.Create`'e ulaşır
3. `ListingManager.CreateAsync` çalışır
4. Eğer hata olursa catch bloğu `LM-001` koduyla `function_logs`'a yazar
5. Middleware response'u yakalar, `endpoint_logs`'a yazar:
   - `method: POST`, `path: /api/listings`
   - `request_body: {"title":"Matematik Dersi",...}`
   - `response_body: {"id":"abc...","slug":"matematik-dersi"}`
   - `status_code: 201`, `user_id: xxx`, `duration_ms: 45`

---

## 8. Admin Panelinden Görüntüleme

`AdminController`'a iki endpoint eklenecek:

```
GET /api/admin/logs/endpoints?page=1&statusCode=500&userId=xxx
GET /api/admin/logs/functions?page=1&errorCode=LM-001
```

Admin panelindeki `Reports.razor` sayfasına log tabloları eklenecek.

---

## 9. Performans Notu

Log yazma işlemi **fire-and-forget** (`_ = logService.LogAsync(...)`) olarak yapılır. Bu sayede:
- İstek cevabı log yazılmasını beklemez
- Kullanıcı deneyimi etkilenmez
- Log yazma hatası uygulamayı çökertmez (try-catch ile sarılı)

Yoğun trafikte (1000+ istek/dk) ileride log yazma işlemi RabbitMQ kuyruğuna alınabilir.

---

## 10. Özet: Dosya Değişiklik Listesi

| Dosya | İşlem |
|---|---|
| `OzelDers.Data/Entities/EndpointLog.cs` | Yeni oluştur |
| `OzelDers.Data/Entities/FunctionLog.cs` | Yeni oluştur |
| `OzelDers.Data/Context/AppDbContext.cs` | 2 DbSet ekle |
| `OzelDers.Business/Interfaces/ILogService.cs` | Yeni oluştur |
| `OzelDers.Business/Services/LogManager.cs` | Yeni oluştur |
| `OzelDers.Business/DependencyInjection.cs` | Servis kaydı ekle |
| `OzelDers.API/Middleware/RequestResponseLoggingMiddleware.cs` | Yeni oluştur |
| `OzelDers.API/Program.cs` | Middleware ekle |
| Her `*Manager.cs` | Prefix sabitleri + try-catch ekle |
| Migration | `AddLoggingTables` migration çalıştır |
