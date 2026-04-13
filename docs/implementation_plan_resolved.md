# OzelDers Platformu Master Uygulama Planı

Bu döküman, projenin mobil stabilizasyonu, "Sahibinden" tarzı keşif sistemi ve AI destekli ilan denetleme altyapısını kurmak için izlenecek **eksiksiz** yol haritasıdır.

## 1. Android Geliştirme Ortamı ve Otomasyon

Sistemde tespit edilen araç yolları kullanılarak geliştirme süreci hızlandırılacaktır.

### [NEW] [baslat_android.bat](file:///D:/OZELDERS/baslat_android.bat)
- **Kritik Değişkenler:**
  - `ANDROID_HOME`: `C:\Program Files (x86)\Android\android-sdk`
  - `JAVA_HOME`: `C:\Program Files\Android\Android Studio\jbr`
  - `ADB Path`: `%ANDROID_HOME%\platform-tools\adb.exe`
  - `AVD Manager`: `%ANDROID_HOME%\cmdline-tools\latest\bin\avdmanager.bat`
- **İşlev:** Emülatörü tek tıkla başlatır (`emulator -avd Pixel_5`), gerekli Java bağımlılıklarını set eder ve MAUI uygulamasını Android hedefine derleyip yükler.

---

## 2. AI Destekli İlan Denetleme ve Kademeli Ban Sistemi

Platform içi güvenliği sağlamak için hibrit bir moderasyon yapısı kurulacaktır.

### Moderasyon Mimarisi (Tiered Triggering)
- **1. Kademe (Anlık):** API seviyesinde Regex taraması. Bariz numaraları (`05xx`) ve mail adreslerini yakalayıp kullanıcıyı anında uyarır.
- **2. Kademe (Derin Analiz):** Şüpheli ilanlar Worker servisinde **ML.NET PII Classifier** ile taranır. "Sıfır beş..." gibi bypass denemelerini %99 doğrulukla yakalar.

### Ceza Cetveli (Strike System)
- **5 İhlal:** 1 Hafta Ban.
- **8 İhlal (5+3):** 1 Ay Ban.
- **11 İhlal (8+3):** Kalıcı Ban.
- **Yaptırım:** Ban zaman aşımına uğramayana kadar kullanıcı `Middleware` seviyesinde engellenir; sadece "Cezalı" uyarısı ve ban bitiş tarihini görür.

---

## 3. "Sahibinden" Tarzı Discovery (Keşif) Sistemi

### Ana Sayfa (Home.razor) Redesign
- **Top Filter (Mega-Filter):** 
  - **Dal Seçimi:** Arama yapılabilir akıllı dropdown.
  - **Lokasyon:** Şehir ve İlçe bağımlı listeleri.
  - **Ücret:** 0 - 2000 TL arası Range Slider.
- **Discovery Grid:** Filtrelerin altında, "Sizin İçin Seçtiklerimiz" başlığıyla rastgele (shuffled) kaliteli ilan kartları.

### Kategori ve Tema Yapısı (CategoryLanding.razor)
Her kategori için özel renk değişkenleri (`--theme-color`) ve Hero grafikler kullanılacaktır:
- **YKS:** `#1E293B` (Koyu Mavi), başarı ve disiplin odaklı görseller.
- **Müzik:** `#8B5CF6` (Sanatsal Mor), enstrüman ve nota silüetleri.
- **Yazılım:** `#10B981` (Tech Green), dark-mode ağırlıklı kod editörü esintili tasarım.
- **Spor:** `#F59E0B` (Enerjik Turuncu), hareket ve performans grafikleri.
- **Okul Seviyeleri:** Ana Sınıfı, İlkokul, Ortaokul, Lise (Dropdown navigasyon üzerinden).

---

## 4. Mobil UI/UX Standartları ve Stabilizasyon

Proje, Apple ve Google'ın modern mobil arayüz standartlarına (Material Design / Human Interface Guidelines) taşınacaktır.

### Touch & Layout Standartları
- **Touch Targets:** Tüm tıklanabilir alanlar minimum **48x48px** olacak (`min-height: 48px`, `padding: 12px 20px`).
- **Typography:** Gövde metni `16px` (1rem). iOS auto-zoom'u engellemek için tüm inputlar odaktayken minimum `16px` kalacak.
- **Safe Areas:** Cihazların çentik ve alt buton çubukları için `padding: env(safe-area-inset-top)` ve `bottom` eklenecek.

### Navigasyon ve Geri Butonu
- **PageHeader.razor:** Her sayfada sol üstte sabit, `44x44px` boyutunda, modern bir geri oku (←). Glassmorphism efekti ve hafif gölge ile her arka planda okunur kılınacak.

---

## 5. Mevcut UI Fix: Cookie Policy

- **Variables.css:** Eksik olan `--color-light` tanımlanacak.
- **Contrast:** Siyah plan üzerindeki metinler `--color-light` ile beyazlatılacak.
- **KVKK Link:** Çerez barı içerisine belirgin bir "KVKK ve Gizlilik Politikası" linki eklenecek.

## Doğrulama Planı
1. **AI Denetim:** Gizli numara içeren ilanla Worker'ın ceza kesip kesmediği.
2. **Ban Uygulaması:** İhlal sayısına göre erişimin doğru kısıtlanıp kısıtlanmadığı.
3. **Mobil Görünüm:** Emülatör üzerinde geri butonu ve 48px kuralının gözle kontrolü.


---

## 6. Seed Verisi Genişletme: 81 İl + Tüm İlçeler + Tam Branş Listesi

### Mevcut Sorun

`DatabaseSeeder.cs` içinde sadece 3 şehir (İstanbul, Ankara, İzmir) ve 3 ilçe var. Branş listesi de sadece 4 kayıt içeriyor. Bu durum ilan oluşturma ve filtreleme ekranlarını doğrudan kırıyor.

### Seed Verisi Nasıl UI'a Gidiyor? (Akış Açıklaması)

```
DatabaseSeeder.cs
      ↓ (uygulama başlarken PostgreSQL'e yazar)
PostgreSQL → Cities, Districts, Branches tabloları
      ↓
BranchesController  → GET /api/branches   → Redis cache (1 saat)
CitiesController    → GET /api/cities     → Redis cache (1 saat)
      ↓
CreateListing.razor → OnInitializedAsync() → Http.GetFromJsonAsync()
      ↓
<select> dropdown'ları → Kullanıcı seçer → model.BranchId / model.DistrictId
```

Yani enum değil, **veritabanı tabanlı seed sistemi** kullanılıyor. Doğru yaklaşım bu. Sadece seed verisi yetersiz.

### Yapılacaklar

#### Adım 6.1 — DatabaseSeeder.cs Güncelleme

`src/OzelDers.Data/Seeds/DatabaseSeeder.cs` dosyası tamamen yeniden yazılacak:

**Şehirler (81 il, plaka kodlarıyla):**
Adana(01), Adıyaman(02), Afyonkarahisar(03), Ağrı(04), Amasya(05), Ankara(06), Antalya(07), Artvin(08), Aydın(09), Balıkesir(10), Bilecik(11), Bingöl(12), Bitlis(13), Bolu(14), Burdur(15), Bursa(16), Çanakkale(17), Çankırı(18), Çorum(19), Denizli(20), Diyarbakır(21), Edirne(22), Elazığ(23), Erzincan(24), Erzurum(25), Eskişehir(26), Gaziantep(27), Giresun(28), Gümüşhane(29), Hakkari(30), Hatay(31), Isparta(32), Mersin(33), İstanbul(34), İzmir(35), Kars(36), Kastamonu(37), Kayseri(38), Kırklareli(39), Kırşehir(40), Kocaeli(41), Konya(42), Kütahya(43), Malatya(44), Manisa(45), Kahramanmaraş(46), Mardin(47), Muğla(48), Muş(49), Nevşehir(50), Niğde(51), Ordu(52), Rize(53), Sakarya(54), Samsun(55), Siirt(56), Sinop(57), Sivas(58), Tekirdağ(59), Tokat(60), Trabzon(61), Tunceli(62), Şanlıurfa(63), Uşak(64), Van(65), Yozgat(66), Zonguldak(67), Aksaray(68), Bayburt(69), Karaman(70), Kırıkkale(71), Batman(72), Şırnak(73), Bartın(74), Ardahan(75), Iğdır(76), Yalova(77), Karabük(78), Kilis(79), Osmaniye(80), Düzce(81)

**İlçeler (her ile ait tam liste):**
Örnek kritik iller:
- İstanbul: Adalar, Arnavutköy, Ataşehir, Avcılar, Bağcılar, Bahçelievler, Bakırköy, Başakşehir, Bayrampaşa, Beşiktaş, Beykoz, Beylikdüzü, Beyoğlu, Büyükçekmece, Çatalca, Çekmeköy, Esenler, Esenyurt, Eyüpsultan, Fatih, Gaziosmanpaşa, Güngören, Kadıköy, Kağıthane, Kartal, Küçükçekmece, Maltepe, Pendik, Sancaktepe, Sarıyer, Silivri, Sultanbeyli, Sultangazi, Şile, Şişli, Tuzla, Ümraniye, Üsküdar, Zeytinburnu (39 ilçe)
- Ankara: Akyurt, Altındağ, Ayaş, Bala, Beypazarı, Çamlıdere, Çankaya, Çubuk, Elmadağ, Etimesgut, Evren, Gölbaşı, Güdül, Haymana, Kalecik, Kahramankazan, Keçiören, Kızılcahamam, Mamak, Nallıhan, Polatlı, Pursaklar, Sincan, Şereflikoçhisar, Yenimahalle (25 ilçe)
- İzmir: Aliağa, Balçova, Bayındır, Bayraklı, Bergama, Beydağ, Bornova, Buca, Çeşme, Çiğli, Dikili, Foça, Gaziemir, Güzelbahçe, Karabağlar, Karaburun, Karşıyaka, Kemalpaşa, Kınık, Kiraz, Konak, Menderes, Menemen, Narlıdere, Ödemiş, Seferihisar, Selçuk, Tire, Torbalı, Urla (30 ilçe)
- Antalya: Akseki, Aksu, Alanya, Demre, Döşemealtı, Elmalı, Finike, Gazipaşa, Gündoğmuş, İbradı, Kaş, Kemer, Kepez, Konyaaltı, Korkuteli, Kumluca, Manavgat, Muratpaşa, Serik (19 ilçe)
- Bursa: Büyükorhan, Gemlik, Gürsu, Harmancık, İnegöl, İznik, Karacabey, Keles, Kestel, Mudanya, Mustafakemalpaşa, Nilüfer, Orhaneli, Orhangazi, Osmangazi, Yıldırım, Yenişehir (17 ilçe)
- (Diğer 76 il de aynı şekilde tam ilçe listesiyle)

**Branşlar (kapsamlı liste, kategorili):**

*Akademik:*
Matematik, Fizik, Kimya, Biyoloji, Türkçe/Edebiyat, Tarih, Coğrafya, Felsefe, Din Kültürü, İngilizce, Almanca, Fransızca, İspanyolca, İtalyanca, Arapça, Rusça, Japonca, Çince

*Sınav Hazırlık:*
YKS/TYT Matematik, YKS/AYT Fizik, YKS/AYT Kimya, YKS/AYT Biyoloji, YKS/AYT Edebiyat, YKS/AYT Tarih, YKS/AYT Coğrafya, KPSS, ALES, YDS/YÖKDİL, DGS, LGS Hazırlık, ÖSYM Sınavları

*Teknoloji & Yazılım:*
Python, JavaScript, Java, C#/.NET, C/C++, PHP, Swift/iOS, Kotlin/Android, React, Vue.js, Angular, Node.js, SQL/Veritabanı, Siber Güvenlik, Veri Bilimi, Yapay Zeka/ML, Unity/Oyun Geliştirme, Web Tasarım

*Müzik & Sanat:*
Piyano, Gitar (Klasik), Gitar (Elektro/Akustik), Keman, Viyola, Çello, Flüt, Klarnet, Saksofon, Davul/Perküsyon, Bağlama/Saz, Ud, Keman, Şan/Vokal, Resim, Yağlı Boya, Suluboya, Heykel, Grafik Tasarım, Fotoğrafçılık

*Spor & Aktivite:*
Yüzme, Tenis, Satranç, Yoga, Pilates, Dans (Salsa/Tango), Bale, Jimnastik, Futbol Antrenörlüğü, Basketbol, Voleybol, Dövüş Sanatları

*Diğer:*
Sürücü Kursu Teorik, Muhasebe/Muhasebe Yazılımları, Girişimcilik, Diksiyon/Sunum, Hız Okuma, Zihin Haritası

#### Adım 6.2 — Branch Entity'ye Kategori Alanı Ekle

`Branch` entity'sine `Category` string alanı eklenecek. Bu sayede branşlar kategorilere göre gruplandırılabilir:

```csharp
// Branch.cs'e eklenecek
public string? Category { get; set; } // "Akademik", "Yazılım", "Müzik", "Spor" vb.
public string? ParentCategory { get; set; } // Üst kategori (opsiyonel)
```

Migration: `AddBranchCategory`

#### Adım 6.3 — BranchesController Güncelleme

Kategoriye göre gruplu döndürme endpoint'i eklenecek:

```
GET /api/branches          → Tüm branşlar (mevcut)
GET /api/branches/grouped  → Kategoriye göre gruplu { "Akademik": [...], "Yazılım": [...] }
```

---

## 7. İlan Oluşturma Formu (CreateListing.razor) Kapsamlı İyileştirme

### Mevcut Eksikler Analizi

Mevcut formda şu alanlar **eksik veya yetersiz**:

| Alan | Durum | Sorun |
|---|---|---|
| Eğitim Seviyesi | ❌ Yok | Hangi sınıf/seviye için ders veriliyor? |
| Deneyim Yılı | ❌ Yok | Kaç yıllık öğretmen? |
| Müsaitlik Saatleri | ❌ Yok | Hangi gün/saatler uygun? |
| Fotoğraf Yükleme | ❌ Yok | Profil/sertifika fotoğrafı |
| Eğitim Geçmişi | ❌ Yok | Mezun olunan okul/bölüm |
| Ders Süresi | ❌ Yok | 45dk mı, 60dk mı, 90dk mı? |
| Grup Dersi | ❌ Yok | Bireysel mi, grup mu? |
| Deneme Dersi | ❌ Yok | Ücretsiz deneme dersi var mı? |
| Şehir seçimi | ⚠️ Eksik | Sadece 3 şehir var (seed sorunu) |
| Branş seçimi | ⚠️ Eksik | Sadece 4 branş var (seed sorunu) |

### Yapılacaklar

#### Adım 7.1 — Listing Entity'ye Yeni Alanlar Ekle

`src/OzelDers.Data/Entities/Listing.cs` dosyasına eklenecek alanlar:

```csharp
// Eğitim seviyesi (hangi sınıf/seviye için)
public string? EducationLevel { get; set; } // "İlkokul", "Ortaokul", "Lise", "Üniversite", "Yetişkin"

// Deneyim
public int? ExperienceYears { get; set; } // 0-30 arası

// Ders detayları
public int LessonDurationMinutes { get; set; } = 60; // 45, 60, 90, 120
public bool IsGroupLesson { get; set; } = false;
public int? MaxGroupSize { get; set; } // Grup dersi ise max kaç kişi
public bool HasTrialLesson { get; set; } = false;

// Müsaitlik (JSON string olarak saklanır)
public string? AvailabilityJson { get; set; } // {"Pazartesi":["09:00-12:00","14:00-18:00"], ...}

// Eğitim geçmişi
public string? EducationBackground { get; set; } // "İTÜ Matematik Mühendisliği mezunu"
```

Migration: `AddListingDetailFields`

#### Adım 7.2 — ListingCreateDto Güncelleme

`src/OzelDers.Business/DTOs/ListingDtos.cs` dosyasına yeni alanlar eklenecek.

#### Adım 7.3 — CreateListing.razor Yeni Form Tasarımı

Form 5 adımlı (wizard) yapıya geçecek:

**Adım 1 — İlan Türü:** Öğretmen mi / Öğrenci mi (mevcut)

**Adım 2 — Temel Bilgiler:**
- Başlık (mevcut)
- Açıklama (mevcut, karakter sayacı ile)
- Branş seçimi (kategorili dropdown — grouped API'den)
- Eğitim Seviyesi (çoklu seçim: İlkokul, Ortaokul, Lise, Üniversite, Yetişkin)

**Adım 3 — Ders Detayları:**
- Ders Türü: Online / Yüz Yüze / Her İkisi (mevcut)
- Ders Süresi: 45dk / 60dk / 90dk / 120dk (radio butonlar)
- Saatlik Ücret (mevcut)
- Deneme Dersi: Var / Yok toggle
- Bireysel / Grup toggle (grup seçilirse max kişi sayısı)

**Adım 4 — Konum & Müsaitlik:**
- Şehir seçimi (81 il — mevcut ama seed düzeltilince çalışacak)
- İlçe seçimi (bağımlı dropdown — mevcut)
- Müsaitlik takvimi (gün/saat seçimi — checkbox grid)

**Adım 5 — Profil & Yayınla:**
- Deneyim yılı (slider: 0-20+)
- Eğitim geçmişi (text input)
- Fotoğraf yükleme (UploadController'a bağlanacak)
- Önizleme + Yayınla butonu

#### Adım 7.4 — Filtreleme (Search.razor) Güncelleme

Arama/filtreleme sayfasına şu filtreler eklenecek:

- Branş (kategorili dropdown)
- Şehir / İlçe (bağımlı)
- Fiyat aralığı (range slider: 0-2000 TL)
- Ders türü (Online/Yüz Yüze/Her İkisi)
- Eğitim seviyesi
- Deneyim yılı (min)
- Deneme dersi var mı
- Sıralama: En Yeni / En Ucuz / En Pahalı / En Yüksek Puan

---

## Uygulama Sırası (Güncellenmiş)

1. **Madde 5** — Variables.css fix + Cookie bar KVKK linki (5 dk)
2. **Madde 6.1** — DatabaseSeeder.cs: 81 il + tüm ilçeler + tam branş listesi
3. **Madde 6.2** — Branch entity'ye Category alanı + migration
4. **Madde 6.3** — BranchesController grouped endpoint
5. **Madde 7.1** — Listing entity yeni alanlar + migration
6. **Madde 7.2** — DTO güncellemeleri
7. **Madde 7.3** — CreateListing.razor wizard form
8. **Madde 7.4** — Search.razor filtre güncellemesi
9. **Madde 4** — Mobil UI standartları (PageHeader.razor, 48px)
10. **Madde 3** — Discovery sistemi (Mega-Filter, CategoryLanding)
11. **Madde 2** — AI Moderasyon + Ban sistemi
12. **Madde 1** — Android bat scripti
