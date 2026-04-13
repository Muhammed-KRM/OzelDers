# 🚀 Teknik Devir Teslim: Master Bağlam ve Gelecek Vizyonu

Bu döküman, **OzelDers** projesinin şu ana kadarki teknik gelişimini, üzerinde uzlaşılan tüm kararları ve bir sonraki oturumda izlenecek detaylı yol haritasını özetler. Yeni oturumda bu belgeyi referans almak, bağlamın %100 korunmasını sağlayacaktır.

## 🧠 1. Teknik Hafıza (Mevcut Durum)
- **Mimari:** .NET MAUI Blazor Hybrid (.NET 10). Backend: API + Worker. DB: PostgreSQL + Redis + ES.
- **Düzeltilenler:** WebView2'deki CSS krizleri (`background-attachment: fixed` vb.) ve eksik kütüphane (`SweetAlert2`, `Google Fonts`) referansları MAUI projesine (`index.html`) başarıyla entegre edildi.
- **Ortam:** Android araçlarının (SDK/JDK) sistemdeki tam yolları keşfedildi ve doğrulandırıldı.

## 🛠️ 2. Android Otomasyonu (Bulunan Yollar)
Bir sonraki oturumda `baslat_android.bat` dosyası şu yollarla kurulacaktır:
- **ANDROID_HOME:** `C:\Program Files (x86)\Android\android-sdk`
- **JDK (Java):** `C:\Program Files\Android\Android Studio\jbr`
- **Emulator:** Pixel_5 (veya yüklü olan en stabil AVD).
- **Komutlar:** `emulator -avd <name>`, `dotnet build -t:Run -f net9.0-android`.

## 🛡️ 3. AI Moderasyon ve Ban Sistemi (Detaylı Tasarım)
Bu sistem, platformun güvenliğini otonom bir şekilde sağlayacaktır:

### A. Kademeli Tetikleme (Tiered Triggering)
- **API (Pre-Filter):** Anlık Regex taraması. Bariz numaralar (`05xx`) yakalandığında ilan kaydedilmeden kullanıcı uyarılır.
- **Worker (Deep-AI):** Şüpheli olan ilanlar RabbitMQ üzerinden Worker'a gider ve **ML.NET PII Classifier** ile taranır. "Sıfır beş..." gibi gizli bypasslar burada yakalanır.

### B. Ceza Algoritması (Strike System)
- **Strike 5:** 1 Hafta Ban.
- **Strike 8:** 1 Ay Ban.
- **Strike 11:** Kalıcı Uzaklaştırma.
- **Uygulama:** Kullanıcı `BanUntil > DateTime.Now` ise, `ModerationMiddleware` üzerinden sadece "Banlısınız" uyarısını görür, hiçbir menüye (Mesajlar, İlanlar vb.) ulaşamaz.

## 🎨 4. UI/UX ve "Sahibinden" Modernizasyonu
Uygulamanın görsel dili "Premium Startup" havasına taşınacaktır:

### Mobil Standartlar (Apple/Google Compliance)
- **Touch Targets:** Minimum `48x48px` tıklama alanları.
- **Geri Butonu:** Her sayfada sol üstte sabit, `44x44px` Glassmorphism etkili geri oku.
- **Okunabilirlik:** Base `16px` font. Input focus zoom koruması.

### Keşif (Discovery) Sistemi
- **Mega-Filter:** Ana sayfa tepesinde Branş + Lokasyon + Fiyat (Range Slider) paneli.
- **Tematik Kategoriler:** Dinamik kategori sayfaları (`/kategori/{slug}`) branşa özel renklerle yüklenecek:
  - **YKS:** `#1E293B` (Koyu Mavi)
  - **Müzik:** `#8B5CF6` (Sanatsal Mor)
  - **Yazılım:** `#10B981` (Tech Green)
  - **Spor:** `#F59E0B` (Enerjik Amber)
- **Navigasyon:** Ana Sınıfı, İlkokul, Ortaokul, Lise dropdown hiyerarşisi.

## 📬 5. Bir Sonraki Oturumda Yapılacaklar (Sırayla)
1. **Düşük Öncelikli Fix:** `variables.css` içine `--color-light: #f8f9fa` ekleyip çerez barını KVKK linkiyle düzeltmek.
2. **Setup:** Android emülatörünü başlatan `.bat` dosyalarını oluşturup MAUI uygulamasını Android emülatöründe çalıştırmak.
3. **Core:** `PageHeader.razor` (Geri butonu) ve 48px touch target kurallarını tüm sayfalara yaymak.
4. **Logic:** `User` entity'sine strike alanlarını ekleyip Worker tarafındaki moderatör servisini (ML.NET) yazmak.
5. **UI-Final:** Sahibinden tarzı filtreleme panelini ve kategori landing sayfalarını kodlamak.

---
> [!IMPORTANT]
> Tüm bu detaylar `implementation_plan.md` ve ilgili `docs/` dökümanlarına da işlenmiştir. Yeni oturumda projenin "Ruhu" bu dökümandadır.
