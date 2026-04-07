# Proje Mimarisi ve Katmanlı Yapı (Detaylı Kurulum Rehberi)

Bu döküman, **Özel Ders Platformu** sistemindeki projelerin nihai "API-First" (Önce API) mimarisini, Visual Studio üzerinden oluşturulacağı zamanki şablon isimlerini, hangi katmanda hangi teknolojinin kullanıldığını ve uyulması gereken **prensipleri/standartları** detaylı bir şekilde açıklar.

---

## 1. Mimarinin Kalbi: API-First Yaklaşımı ve İstek Akışı (Request Flow)

Sistem üç ana bölüme ayrılır: **Arka Uç (Backend)**, **Ortak Arayüz (SharedUI)**, ve **Çalıştırıcı Kabuklar (Hosts)**.

**Tipik Bir İstek (Request) Akışı Şöyledir:**
1. **Host (Web/MAUI):** Kullanıcı bir butona tıklar. `HttpClient` kütüphanesi kullanılarak API'ye JSON formatında (`https://api.ozelders.com/listings`) bir istek atılır. Mobil cihazdayken başlığa (header) güvenli depodan alınan JWT token de eklenir.
2. **API (Güvenlik Kapısı):** İstek API'ye gelir. API, `JwtBearer` ile kullanıcının yetkisi olup olmadığına bakar ve `RateLimiting` (Erişim sınırlandırma) ile siber saldırı (DDoS) var mı kontrol eder. Güvenli bulursa isteği Business katmanına iletir.
3. **Business (Beyin):** Veriler doğrulanır (`FluentValidation`) ve iş kuralları çalıştırılır. İşlem veritabanına kaydedilecekse Data katmanına emir verilir.
4. **Data (Hafıza):** `Entity Framework Core` ile SQL sorgusu oluşturulur ve PostgreSQL veritabanına yazılır/okunur.
5. **Geri Dönüş:** Veri, API üzerinden `DTO (Data Transfer Object)` adı verilen güvenlikli taşıma modelleri (JSON) ile tekrar UI'a döndürülür.

---

## 2. Proje Klasör Yapısı, Klasörler ve Teknolojiler

Visual Studio'da Proje (Solution) oluştururken seçmeniz gereken spesifik şablonlar ve her katmanın detayları şunlardır:

### 📦 OzelDers.Data
- **VS Şablonu:** `Sınıf Kitaplığı` / `Class Library` (.NET 9)
- **Rolü ve Sınırları:** Sadece veritabanı ile konuşur. Gelen verinin iş kuralına doğru olup olmadığına bakmaz. Sadece yaz ve oku yapar.
- **İçereceği Dosyalar:**
  - `Entities/` klasörü (`User.cs`, `Listing.cs`)
  - `Context/` klasörü (`AppDbContext.cs`)
  - `Repositories/` klasörü (`ListingRepository.cs`)
- **Kullanılan Teknolojiler:**
  - `Entity Framework Core` (ORM)
  - `Npgsql.EntityFrameworkCore.PostgreSQL` (Veritabanı sürücüsü)
- **Uyulacak Standartlar:** `Repository Pattern` (Veriye erişimi soyutlama) kullanılacaktır.

### 📦 OzelDers.Business
- **VS Şablonu:** `Sınıf Kitaplığı` / `Class Library` (.NET 9)
- **Rolü ve Sınırları:** Sistemin BEYNİDİR. Tüm algoritmalar buradadır. Veriyi işlemek için "CQRS" mantığıyla komutları ayıran yapıdır.
- **İçereceği Dosyalar:**
  - `Handlers/` klasörü (MediatR tabanlı CQRS Komutları ve Sorguları)
  - `Interfaces/` klasörü (`IListingService.cs`)
  - `DTOs/` klasörü (`ListingCreateDto.cs`)
  - `Validators/` klasörü (`ListingValidator.cs`)
- **Kullanılan Teknolojiler (Enterprise CV Booster):**
  - `MediatR` (Modern .NET iş ilanlarının %90'ı CQRS ve MediatR tecrübesi arar)
  - `Elasticsearch` (Aramaları hızlandırmak için - Big Data / Search Engine deneyimi)
  - `Redis` (Sık sorulanları akılda tutmak için - Distributed Caching deneyimi)
  - `FluentValidation` ve `AutoMapper`
  - `BCrypt` (Şifreleri tek yönlü hash'leme)
- **Uyulacak Standartlar:** "SOLID" prensipleri esastır. Veritabanının PostgreSQL veya SQL Server olduğunu BİLMEZ, sadece Repository Interface'i ile konuşur. Kullanıcı arayüzüne (UI) dair hiçbir koda sahip olamaz.

### 📦 OzelDers.Worker *(Arka Plan İşçisi - Kurumsal CV Gücü)*
- **VS Şablonu:** `Worker Service`
- **Rolü ve Sınırları:** Bildirim gönderme, resim boyutlandırma gibi uzun süren işleri API'nin sırtından alan projedir. Sistemin donmasını engeller.
- **Kullanılan Teknolojiler:**
  - `RabbitMQ` (Mesaj Kuyruğu / Message Broker)
  - `MassTransit` (RabbitMQ iletişimini yöneten ana kütüphane)
- **Neden Önemli?** İş ilanlarında aranan en popüler "Asenkron İşlem (Microservices)" tecrübesidir.

### 📦 OzelDers.API *(Başlangıç (Startup) Projesi)*
- **VS Şablonu:** `ASP.NET Core Web API`
- **Rolü ve Sınırları:** Güvenlik bekçisi ve kapıdır. Sadece gelen istekleri yönlendirir, kendi içinde algoritma (Business Logic) yazılmaz.
- **İçereceği Dosyalar:**
  - `Controllers/` klasörü (`ListingsController.cs`, `AuthController.cs`)
  - `Program.cs` (Tüm sistemin güvenlik, bağımlılık (DI) ve CORS ayarlarının yapıldığı merkez ayar dosyası)
- **Kullanılan Teknolojiler (Güvenlik Ağırıklı):**
  - `Microsoft.AspNetCore.Authentication.JwtBearer` (Giriş Token'ları doğrulama)
  - `Microsoft.AspNetCore.RateLimiting` (Bir saniyede atılabilecek maksimum istek limiti)
  - `Serilog` (Hata ve işlemlerin standart kaydının (log) tutulması)
  - CORS politikaları: Sadece bilinen domainlerden (`WithOrigins`) gelen isteklere izin verme.
- **Uyulacak Standartlar:** Sadece JSON döner. Asla HTML/UI render etmez. Hataları RFC formatında (Problem Details) döndürür.

### 📦 OzelDers.SharedUI
- **VS Şablonu:** `Razor Sınıf Kitaplığı` / `Razor Class Library (RCL)`
- **Rolü ve Sınırları:** Tüm sistemin "yüzü"dür. Butonlar, Tablolar, Arama çubuğu hepsi buradadır. Kendi başına çalışamaz (Host projelere muhtaçtır).
- **İçereceği Dosyalar:**
  - `Pages/` klasörü (`Home.razor`, `Search.razor`)
  - `Components/` klasörü (`ListingCard.razor`, `Navigation.razor`)
  - `wwwroot/css/` (Tema renkleri ve stiller)
- **Kullanılan Teknolojiler:**
  - `ASP.NET Core Razor Components` (C# ile HTML yazma)
  - Pure CSS / SCSS (Tasarım kodları)
- **Uyulacak Standartlar:** İçinde kesinlikle `HttpClient` kullanımı veya veritabanı komutu OLAMAZ. Veriyi sadece `IListingService` gibi sözleşmeler (Interface) üzerinden ister. Verinin API'den mi yoksa DB'den mi geldiği SharedUI'nin umurunda değildir.

### 📦 OzelDers.Web *(Tarayıcı Uygulaması Host'u)*
- **VS Şablonu:** `Blazor Web App`
- **Kurulum Ayarı:** Oluştururken "Interactive render mode" olarak `Server` ve `Global` seçilir.
- **Rolü ve Sınırları:** İnternet tarayıcısından (Chrome/Safari) siteye girildiğinde çalışacak omurgadır. Görüntüyü *SharedUI*'dan çeker. 
- **İçereceği Dosyalar:**
  - `Program.cs` (Servislerin API olarak kullanılacağının tanıtılması)
  - `App.razor` (Kök HTML yapısı)
- **Kullanılan Teknolojiler & Güvenlik:**
  - `HttpClient` kullanarak internet üzerinden API'ye iletişim kurar. 
  - (Zorunlu SEO Stratejisi): SSR (Sunucu Taraflı İşleme - Server Side Rendering) kullanır, böylece arama motorları içeriği okuyabilir.

### 📦 OzelDers.App *(Mobil/Masaüstü Uygulaması Host'u)*
- **VS Şablonu:** `.NET MAUI Blazor Hybrid App`
- **Rolü ve Sınırları:** İnsanların App Store veya Play Store'dan indireceği, masaüstünde çift tıklayıp açacağı projedir. Web ile tıpatıp aynı *SharedUI* dosyalarını çeker ve bir tarayıcı kabuğunda çalıştırır.
- **İçereceği Dosyalar:**
  - `Platforms/` klasörü (Android, iOS, Windows için özel ayarlar ve ikonlar)
  - `MauiProgram.cs` (Uygulamanın başlangıç ayarları)
- **Kullanılan Teknolojiler & Güvenlik:**
  - `Microsoft.Maui.Storage.SecureStorage` **[KRİTİK GÜVENLİK ADIMI]**: Kullanıcının oturum anahtarı (JWT) gibi hassas verileri telefonun en güvenli yeri olan (Kasasında) donanımsal alanda şifreli saklar.
  - `MediaPicker`: Fotoğraf yüklemek istendiğinde web sitesinden farklı olarak telefonun yerel galerisiyle haberleşir.

---

## 3. Katmanlar Arası Sınır (Bağlantı) Kuralları

Projelerin birbirleriyle olan hiyerarşisi kesin kurallara bağlıdır:

1. **DataAccess Kesinliği:** `Business` projesi olmadan `Data` hiçbir işe yaramaz. `Business` veritabanına ulaşmak için `Data` projesini **referans alır**. 
2. **API Bağımlılığı:** `API` projesi sistemi yönetmek için `Business` projesini **referans alır** (Böylelikle Data'ya da dolaylı yoldan erişir).
3. **UI Bağımsızlığı (Altın Kural):**
   - Ne `Web` Host ne de `App` (MAUI) Host **ASLA** `Business` veya `Data` katmanını referans olarak bilemez.
   - Bu kabuk projeler sadece `SharedUI` projesini bilirler.
   - Veriye ulaşacakları zaman içlerindeki `HttpClient` ile ağ (Network) üzerinden Web API projesine (https://api.ozelders.com) HTTP GET/POST istekleri atarlar.

Bu sınırları katı bir şekilde uygulamak, projenin büyümesi, bozulmadan yeni özellikler eklenebilmesi ve güvenliği için hayati önem taşır.
