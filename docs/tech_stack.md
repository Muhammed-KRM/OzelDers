# 🚀 Teknoloji Envanteri ve Mimari Özet

Bu döküman, projenin teknolojik omurgasını ve kullanılan araçların rollerini özetler.

## 1. Backend & Veri (C# / .NET 9)
*   **ASP.NET Core Web API:** Mobil uygulamanın ve dış servislerin bağlandığı merkez.
*   **PostgreSQL:** Kalıcı verilerin (Kullanıcı, İlanlar, Mesajlar) saklandığı ana veritabanı.
*   **Entity Framework Core:** Veritabanı işlemlerini C# ile yürüten ORM katmanı.
*   **Elasticsearch:** Gelişmiş filtreleme ve CQRS mimarisinde "Read (Okuma)" veritabanı olarak kullanılır. (CV için Kritik Seviye: Yüksek)
*   **Redis:** Performansı artırmak, rate-limiting (istek sınırlama) ve dağıtık önbellekleme (Distributed Cache) işlemleri için kullanılır.

## 2. Frontend & UI (Blazor)
*   **Blazor Web App:** Tarayıcıda çalışan SEO uyumlu web arayüzü.
*   **Blazor Hybrid (.NET MAUI):** Android, iOS ve Masaüstü için native uygulama kabuğu.
*   **SharedUI (RCL):** Tüm platformların kullandığı ortak butonlar, sayfalar ve CSS dosyaları.
*   **SignalR:** Web ile sunucu arasındaki gerçek zamanlı iletişim (mesaj bildirimleri vb.).

## 3. Arka Plan ve İletişim (RabbitMQ)
*   **RabbitMQ:** Asenkron iletişim, mikroservis arası mesajlaşma ve arka plan işleri (Görsel İşleme, Toplu E-posta) için kullanılır. İş ilanlarında en çok aranan "Message Broker" deneyimidir.
*   **MassTransit:** RabbitMQ iletişimini yöneten, soyutlama sağlayan ana kütüphane.
*   **MediatR:** API ve Business katmanı arasında CQRS (Okuma/Yazma ayrımı) prensibini uygulamak için kullanılır. (Enterprise seviye .NET kodu standardı).
*   **OzelDers.Worker:** Kuyruktaki işleri (E-posta, Görsel Boyutlandırma, ES Indexleme) sırayla yapan arka plan (Background) servisi.

## 4. AI & Moderasyon
*   **ML.NET:** İlanlardaki iletişim bilgilerini (telefon, e-posta) yakalamak için kullanılan yerel makine öğrenmesi kütüphanesi.
*   **Smart Regex:** API seviyesinde hızlı ve akıllı modelleme ile ilk katman denetimi sağlar.
*   **LlamaSharp / Ollama (Opsiyonel):** Worker seviyesinde daha derinlemesine niyet analizi yapabilen yerel dil modelleri.

## 5. Güvenlik ve Altyapı
*   **JWT:** Bearer Token tabanlı kimlik doğrulama.
*   **AES-256:** Hassas veriler (TCKN, IBAN) için veritabanı düzeyinde şifreleme.
*   **ImageSharp:** Fotoğrafların WebP formatına dönüştürülmesi ve watermark basılması.
*   **Docker:** Tüm bu teknolojilerin (Postgre, Redis, ES, RabbitMQ) tek komutla ayağa kaldırılmasını sağlayan konteyner yapısı.
*   **Nginx:** Gelen trafiği Web App veya API'ye yönlendiren Reverse Proxy.

## 6. Geliştirme ve SDK Ortamı
*   **Android SDK & Tools:** MAUI projelerinin Android platformuna derlenmesi için gerekli `avdmanager`, `emulator` ve `platform-tools`.
*   **JDK (Java Development Kit):** Android derleme süreçleri için `jbr` (Android Studio) veya OpenJDK bağımlılığı.

## 7. Doküman Referansları
| Konu | Doküman |
| :--- | :--- |
| Katmanlı Mimari ve Klasörler | [implementation_plan_part1.md](file:///d:/OZELDERS/docs/implementation_plan_part1.md) |
| Servis Kayıtları ve Platform Farkları | [architecture_reference.md](file:///d:/OZELDERS/docs/architecture_reference.md) |
| Altyapı Kurulumu (Docker/ES/Redis) | [implementation_plan_part2.md](file:///d:/OZELDERS/docs/implementation_plan_part2.md) |
| Jeton ve Ödeme Sistemleri | [implementation_plan_part2.md](file:///d:/OZELDERS/docs/implementation_plan_part2.md) |
