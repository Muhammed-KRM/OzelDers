# OzelDers — Sonraki Adımlar Yol Haritası

**Tarih:** Nisan 2026  
**Durum:** Temel uygulama çalışır durumda, bu döküman büyüme için gereken adımları tanımlar.

---

## BÖLÜM 1 — Gerçek Ödeme Sistemi

### 1.1 Türkiye'de Bireysel Ödeme Alma Durumu

Türkiye'de online ödeme almak için **şirket kurulması zorunlu değil** ama yasal bir çerçeve gerekiyor. İki yol var:

**Yol A — Şahıs Şirketi (Önerilen)**
- Vergi dairesine gidip şahıs şirketi açılır (~1 gün, ücretsiz)
- Genç girişimci istisnası: 29 yaş altı, ilk 3 yıl gelir vergisi muafiyeti
- Aylık sabit gider: SGK primi (~1.500-2.000 TL/ay)
- Bu yolla iyzico, PayTR, Sipay gibi tüm sistemlere başvurabilirsin

**Yol B — Bireysel Başvuru (Sınırlı)**
- Bazı sistemler (Sipay, Papara) bireysel IBAN ile çalışıyor
- Ama yüksek hacimli işlemlerde vergi riski var
- Uzun vadeli platform için önerilmez

### 1.2 Ödeme Sistemi Karşılaştırması

| Sistem | Komisyon | Şirket Gerekli? | .NET SDK | Türkçe Destek |
|---|---|---|---|---|
| **iyzico** | %2.85 + 0.25 TL | Evet (şahıs yeterli) | ✅ Resmi | ✅ |
| **PayTR** | %1.99 + KDV | Evet | ✅ Resmi | ✅ |
| **Sipay** | %2.5 | Hayır (bireysel) | ⚠️ Yok (HTTP) | ✅ |
| **Stripe** | %2.9 + $0.30 | Türkiye'de çalışmıyor | ✅ | ❌ |
| **Paddle** | %5 | Hayır (MoR) | ✅ | ❌ |

### 1.3 Öneri: iyzico

**Neden iyzico:**
- Türkiye'nin en yaygın ödeme sistemi
- Resmi .NET SDK var (`iyzipay` NuGet paketi)
- Taksit desteği (Türk kullanıcılar için kritik)
- Şahıs şirketiyle başvurulabilir
- Mevcut `PayTRPaymentService` ve `StripePaymentService` altyapısı var, aynı `IPaymentService` interface'ine iyzico eklenecek

### 1.4 Uygulama Adımları

1. Şahıs şirketi aç (vergi dairesi, 1 gün)
2. iyzico.com'dan başvur (belge: vergi levhası, kimlik, IBAN)
3. Onay ~3-5 iş günü
4. `dotnet add package iyzipay` ile SDK ekle
5. `IyzicoPaymentService.cs` yaz, `IPaymentService` interface'ini implement et
6. `appsettings.json`'a API key ekle
7. `FakePayment.razor` sayfasını gerçek ödeme akışıyla değiştir

---

---

## BÖLÜM 2 — DevOps, Hosting ve Ölçeklendirme

### 2.1 Şu Anki Durum

Proje Docker Compose ile çalışıyor. Bu geliştirme için mükemmel ama production için yetersiz:
- Tek sunucu — sunucu çökerse site çöker
- Manuel deployment — kod değişince elle `docker-compose up --build` yapmak gerekiyor
- Auto scaling yok — 1000 kullanıcı aynı anda gelirse sistem çöker

### 2.2 Hosting Seçenekleri

**Seçenek A — Hetzner Cloud (Önerilen Başlangıç)**
- Fiyat: 2 vCPU / 4GB RAM = ~6€/ay (en ucuz seçenek)
- Almanya'da data center (GDPR uyumlu, Türkiye'ye yakın)
- Docker Compose ile direkt çalışır, Kubernetes'e geçiş kolay
- Dezavantaj: Managed Kubernetes pahalı (~60€/ay)

**Seçenek B — DigitalOcean**
- Fiyat: 2 vCPU / 4GB = ~24$/ay
- Managed Kubernetes (DOKS): ~12$/ay + node maliyeti
- Daha iyi dokümantasyon, daha kolay yönetim
- Türk startup'lar için popüler

**Seçenek C — Azure (Büyüme için)**
- Azure Kubernetes Service (AKS): Kubernetes yönetimi ücretsiz, sadece VM maliyeti
- .NET projesi olduğu için Azure ile entegrasyon mükemmel
- GitHub Actions ile CI/CD çok kolay
- Fiyat: Başlangıç ~50-100$/ay

### 2.3 Önerilen Yol: Aşamalı Geçiş

**Faz 1 (Şimdi — İlk 6 ay): Hetzner VPS + Docker Compose**
- Tek sunucu, Docker Compose ile çalış
- Nginx reverse proxy (zaten var)
- Maliyet: ~15-20€/ay
- Yeterli kullanıcı sayısı: ~500 eş zamanlı

**Faz 2 (6-12 ay): DigitalOcean Managed Kubernetes**
- 3 node cluster
- Auto scaling aktif
- Maliyet: ~80-120$/ay
- Yeterli kullanıcı sayısı: ~5.000 eş zamanlı

**Faz 3 (12+ ay): Azure AKS**
- Multi-region deployment
- Azure CDN, Azure Cache for Redis
- Maliyet: ~300-500$/ay

### 2.4 CI/CD Pipeline

GitHub Actions ile otomatik deployment:

```yaml
# .github/workflows/deploy.yml
on:
  push:
    branches: [main]
jobs:
  deploy:
    steps:
      - dotnet build + test
      - docker build + push to registry
      - SSH to server + docker-compose up
```

Bu sayede `git push` yapınca otomatik deploy olur.

### 2.5 Kubernetes Hakkında

Kubernetes karmaşık görünüyor ama temel kavramlar basit:
- **Pod**: Çalışan container (API, Worker, Web)
- **Deployment**: Kaç pod çalışsın, nasıl güncellensin
- **Service**: Pod'lara nasıl erişilsin
- **HPA (Horizontal Pod Autoscaler)**: Yük artınca otomatik pod ekle

Başlangıçta Kubernetes'e gerek yok. Docker Compose + iyi bir VPS yeterli. Kullanıcı sayısı artınca geçiş yapılır.

---

---

## BÖLÜM 3 — Tasarım İyileştirmeleri

### 3.1 Mevcut Durum

Tasarım çalışıyor ama şu eksikler var:
- Gerçek fotoğraf yok (stock görsel veya emoji kullanılıyor)
- Font tutarsızlığı (bazı yerlerde sistem fontu)
- Mobil görünümde bazı kaymalar
- İlan kartları sade, öne çıkmıyor
- Ana sayfa hero section zayıf

### 3.2 Öncelikli İyileştirmeler

**A. Font Sistemi**
Google Fonts'tan `Plus Jakarta Sans` (başlıklar) ve `Inter` (gövde) ekle:
```html
<!-- App.razor veya index.html'e -->
<link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;600;700;800&family=Inter:wght@400;500;600&display=swap" rel="stylesheet">
```

**B. İlan Kartı Redesign**
Mevcut kart sade. Eklenecekler:
- Öğretmen profil fotoğrafı (avatar)
- Yıldız rating görsel olarak (⭐⭐⭐⭐⭐)
- "Online / Yüz Yüze" badge
- Hover'da kart yükselmesi (zaten var ama güçlendirilebilir)

**C. Ana Sayfa Hero**
- Büyük başlık + gradient text (zaten var)
- Arkaya soyut blob şekiller ekle (CSS ile)
- İstatistik sayaçları animasyonlu hale getir (CountUp.js veya CSS)

**D. Renk Tutarlılığı**
`variables.css`'te tanımlı renkler bazı yerlerde inline style ile override ediliyor. Bunları temizle.

**E. Dark Mode (Opsiyonel)**
Navbar'da zaten toggle var ama dark mode CSS yazılmamış. Kullanıcılar tıklıyor ama hiçbir şey olmuyor.

### 3.3 Araçlar

- **Figma** — Tasarım mockup için (ücretsiz)
- **Unsplash API** — Gerçek fotoğraf için (ücretsiz)
- **Heroicons** — SVG ikon seti (ücretsiz, MIT)
- **Animate.css** — Hazır animasyonlar (ücretsiz)

---

---

## BÖLÜM 4 — Mobil Uygulama (MAUI) Test ve Çalıştırma

### 4.1 Mevcut Durum

`OzelDers.App` projesi var ama hiç çalıştırılmadı. `baslat_android.bat` scripti yazıldı ama emülatör kurulmadı.

### 4.2 Emülatör Kurulum Adımları

**Adım 1 — Android SDK Kontrol**
```
C:\Program Files (x86)\Android\android-sdk
```
Bu klasör varsa SDK kurulu. Yoksa Android Studio'dan kur.

**Adım 2 — AVD (Android Virtual Device) Oluştur**
Android Studio → Tools → Device Manager → Create Device
- Cihaz: Pixel 6
- API Level: 33 (Android 13)
- RAM: 2GB

**Adım 3 — VS'de MAUI Workload Kur**
```bash
dotnet workload install maui-android
```

**Adım 4 — VS'de Çalıştır**
- Startup project: `OzelDers.App`
- Target: Android Emulator (Pixel 6)
- F5

### 4.3 Bilinen Sorunlar ve Çözümleri

**Sorun 1: API URL**
`MauiProgram.cs`'te API adresi `http://10.0.2.2:5074` (Android emülatörde localhost = 10.0.2.2). API'nin çalışıyor olması gerekiyor.

**Sorun 2: HTTPS Sertifika**
Emülatörde HTTPS sertifika hatası alınabilir. Development için HTTP kullan:
```csharp
// MauiProgram.cs
var apiBase = "http://10.0.2.2:5074/";
```

**Sorun 3: Blazor WebView Performansı**
İlk açılışta yavaş olabilir. Normal, sonraki açılışlarda cache'den gelir.

**Sorun 4: Hot Reload**
MAUI'de hot reload sınırlı. Kod değişince genellikle yeniden deploy gerekir.

### 4.4 Gerçek Cihazda Test

Android telefonu USB ile bağla:
1. Telefonda Geliştirici Seçenekleri aç
2. USB Hata Ayıklama aç
3. VS'de cihaz listesinde görünür
4. F5 ile direkt telefona yükle

### 4.5 iOS Test

iOS için Mac gerekiyor. Seçenekler:
- Mac bilgisayar (pahalı)
- MacInCloud servisi (~10$/ay, uzak Mac)
- TestFlight ile beta test

---

## BÖLÜM 5 — Öncelik Sırası

Şu an için önerilen sıra:

1. **Mobil test** — En az maliyet, hemen yapılabilir. Emülatör kur, çalıştır, sorunları gör.
2. **Tasarım iyileştirmeleri** — Font ekle, ilan kartını güçlendir. 1-2 gün.
3. **Hetzner VPS'e deploy** — Gerçek ortamda test. ~15€/ay.
4. **Şahıs şirketi + iyzico** — Yasal süreç, 1-2 hafta.
5. **CI/CD pipeline** — GitHub Actions, 1 gün.
6. **Kubernetes** — Kullanıcı sayısı artınca.
