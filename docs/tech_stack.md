# 🚀 Teknoloji Envanteri ve Mimari Özet

Bu döküman, projenin teknolojik omurgasını ve kullanılan araçların rollerini özetler.

## 1. Backend & Veri (C# / .NET 10)
*   **ASP.NET Core Web API:** Mobil uygulamanın ve dış servislerin bağlandığı merkez.
*   **PostgreSQL 16:** Kalıcı verilerin (Kullanıcı, İlanlar, Mesajlar) saklandığı ana veritabanı.
*   **Entity Framework Core 10:** Veritabanı işlemlerini C# ile yürüten ORM katmanı.
*   **Elasticsearch 8.12:** Gelişmiş filtreleme ve CQRS mimarisinde "Read (Okuma)" veritabanı olarak kullanılır. İlanlar oluşturulunca Worker tarafından otomatik indexlenir.
*   **Redis 7:** Performansı artırmak ve dağıtık önbellekleme (Distributed Cache) işlemleri için kullanılır. Şehir/branş listeleri 1 saat cache'lenir.

## 2. Frontend & UI (Blazor)
*   **Blazor Server (.NET 10):** Tarayıcıda çalışan SEO uyumlu web arayüzü.
*   **Blazor Hybrid (.NET MAUI):** Android, iOS ve Masaüstü için native uygulama kabuğu (erken aşama).
*   **SharedUI (RCL):** Tüm platformların kullandığı ortak bileşenler, sayfalar ve CSS dosyaları.
*   **AuthTokenHandler:** Her HTTP isteğine otomatik Bearer token ekleyen DelegatingHandler. Token `ProtectedLocalStorage`'da `UserSession` key'inde şifreli saklanır.

## 3. Arka Plan ve İletişim
*   **RabbitMQ 3:** Asenkron iletişim ve mesaj kuyruğu. İlan oluşturma/güncelleme/silme eventleri buradan geçer.
*   **MassTransit v8.3.6:** RabbitMQ iletişimini yöneten soyutlama kütüphanesi. **v9 ücretli lisans gerektirdiği için v8 kullanılıyor — v9'a geçilmemeli.**
*   **MediatR:** API ve Business katmanı arasında CQRS prensibini uygulamak için kullanılır.
*   **OzelDers.Worker:** Kuyruktaki işleri yapan arka plan servisi. Consumer'lar: `ListingCreatedConsumer`, `ListingUpdatedConsumer`, `ListingDeletedConsumer`. Ayrıca `VitrinExpirationWorker` her 1 saatte vitrin sürelerini kontrol eder.

## 4. AI & Moderasyon (Planlanıyor)
*   **ML.NET:** İlanlardaki iletişim bilgilerini (telefon, e-posta) yakalamak için yerel makine öğrenmesi.
*   **Smart Regex:** API seviyesinde hızlı ilk katman denetimi (mevcut, `ListingManager.PerformAutoModeration`).
*   **Strike Sistemi:** 5/8/11 ihlalde 1 hafta/1 ay/kalıcı ban (planlanıyor).

## 5. Güvenlik ve Altyapı
*   **JWT:** Bearer Token tabanlı kimlik doğrulama. Token `ProtectedLocalStorage`'da şifreli saklanır.
*   **AES-256:** Hassas veriler (TCKN, IBAN) için veritabanı düzeyinde şifreleme.
*   **ImageSharp:** Fotoğrafların WebP formatına dönüştürülmesi.
*   **Docker Compose:** Tüm servislerin tek komutla ayağa kaldırılması. `.env` dosyasından ortam değişkenleri okunur.
*   **Nginx:** Gelen trafiği Web App veya API'ye yönlendiren Reverse Proxy.

## 6. Geliştirme Ortamı
*   **.NET 10 SDK**
*   **Android SDK & Tools:** MAUI projelerinin Android platformuna derlenmesi için.
*   **JDK:** Android derleme süreçleri için.
*   **pgAdmin:** PostgreSQL yönetimi için.

## 7. Doküman Referansları
| Konu | Doküman |
| :--- | :--- |
| Devir Teslim ve Güncel Durum | [devir_teslim.md](devir_teslim.md) |
| Ana Yol Haritası | [implementation_plan_resolved.md](implementation_plan_resolved.md) |
| Katmanlı Mimari ve Klasörler | [implementation_plan_part1.md](implementation_plan_part1.md) |
| Servis Kayıtları ve Platform Farkları | [architecture_reference.md](architecture_reference.md) |
| Altyapı Kurulumu | [implementation_plan_part2.md](implementation_plan_part2.md) |
| Logging Sistemi | [logging_system_design.md](logging_system_design.md) |
