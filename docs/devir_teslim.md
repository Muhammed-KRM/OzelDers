# OzelDers Projesi — Devir Teslim Dökümanı

**Tarih:** 14 Nisan 2026  
**Durum:** Sistem çalışıyor, temel özellikler aktif

---

## Proje Nedir?

**OzelDers.com** — Türkiye'ye yönelik özel ders platformu. Öğretmenler ilan oluşturup öğrencilerle buluşuyor. "Sahibinden" tarzı discovery sistemi hedefleniyor.

---

## Ortamı Ayağa Kaldırma

**Ön koşul:** `.env` dosyası proje kökünde olmalı (git'e gönderilmez).

```
DB_PASSWORD=OzelDers_Dev_2024!
JWT_KEY=OzelDers_JWT_Secret_Key_Min32Chars!!
AES_KEY=OzelDers_AES_Key_32Chars_Here!!
ADMIN_EMAIL=admin@ozelders.com
ADMIN_PASSWORD=Admin_OzelDers_2024!
ELASTIC_PASSWORD=OzelDers_Elastic_2024!
```

```powershell
docker-compose up --build -d
```

**Erişim noktaları:**

| Servis | URL |
|---|---|
| Web UI | http://localhost:8080 |
| API | http://localhost:5001 |
| Swagger | http://localhost:5001/swagger |
| Elasticsearch | http://localhost:9200 (elastic / OzelDers_Elastic_2024!) |
| RabbitMQ Yönetim | http://localhost:15672 (guest / guest) |
| PostgreSQL | localhost:5432 (pgAdmin ile bağlanılabilir) |

---

## Klasör Yapısı

```
src/
  OzelDers.API/          → REST API, Controller'lar, Middleware
  OzelDers.Web/          → Blazor Server uygulaması
  OzelDers.SharedUI/     → Paylaşılan Razor bileşenleri ve sayfalar
  OzelDers.Business/     → Servisler, DTO'lar, Validation, Events
  OzelDers.Data/         → Entity'ler, Repository'ler, Migration'lar, Seed
  OzelDers.Worker/       → Background servisler, MassTransit Consumer'lar
  OzelDers.App/          → MAUI mobil uygulama (erken aşama, dokunulmadı)
docs/
  implementation_plan_resolved.md  → Ana yol haritası (her zaman buraya bak)
```

---

## Bu Oturumda Yapılanlar (13-14 Nisan 2026)

### 1. Docker Ortamı Stabilize Edildi
- `.env` dosyası yoktu → oluşturuldu
- Eski postgres volume şifre uyuşmazlığı → volume silindi, temiz başlatıldı
- `docker-compose up --build -d` artık sorunsuz çalışıyor, tüm servisler `Up`

### 2. MassTransit v9 → v8.3.6 Düşürüldü
- MassTransit v9 ücretli lisans istiyor, v8.3.6 tamamen ücretsiz
- `OzelDers.Business.csproj`, `OzelDers.Worker.csproj`, `OzelDers.API.csproj` güncellendi
- `DummyPublishEndpoint` kaldırıldı, gerçek RabbitMQ bağlantısı kuruldu
- API `Program.cs`'e `AddMassTransit` + RabbitMQ konfigürasyonu eklendi

### 3. Elasticsearch Indexleme Pipeline'ı Aktif
- Worker'daki Consumer'lar (ListingCreated/Updated/Deleted) yeniden aktif edildi
- Test edildi: ilan oluşturulunca Worker logu `İlan Elasticsearch'e indexlendi` veriyor
- İlk ilan oluşturulana kadar ES'te `listings` index'i yok — bu normal, otomatik oluşuyor

### 4. Seed Verisi Genişletildi
- `DatabaseSeeder.cs` tamamen yeniden yazıldı
- 81 il (plaka kodlarıyla), tüm ilçeler, 87 branş (kategorili) eklendi
- `Branch` entity'sine `Category`, `IsPopular`, `DisplayOrder` alanları eklendi
- Migration: `20260413153028_AddBranchCategoryAndFullSeedData`

### 5. CitiesController Düzeltildi
- İlçeler şehirlerle birlikte `GET /api/cities` endpoint'inden dönüyor
- `CreateListing.razor`'daki `city.Districts` kullanımı çalışır hale geldi

### 6. Swagger Production'da Açıldı
- Önceden sadece Development'ta açıktı → her ortamda erişilebilir yapıldı

### 7. Yanıltıcı Log Kodu Temizlendi
- `CreateListing.razor`'da `localStorage.getItem("authToken")` yanlış key'e bakıyordu
- Token `ProtectedLocalStorage`'da `UserSession` key'inde şifreli duruyor
- `AuthTokenHandler` her istekte otomatik Bearer token ekliyor — sistem güvenliydi
- Log kodu düzeltildi

---

## Kritik Mimari Notlar

### Token / Auth Akışı
```
Login → UserSession (ProtectedLocalStorage'a şifreli kaydedilir)
Her HTTP isteği → AuthTokenHandler → ProtectedLocalStorage'dan UserSession okur → Bearer header ekler
API → [Authorize] → JWT'den userId alır → işlem yapar
```
`localStorage.getItem("authToken")` ile token okunamaz — `ProtectedLocalStorage` şifreli saklıyor.

### Elasticsearch Indexleme Akışı
```
API: ListingManager.CreateAsync
  → _publishEndpoint.Publish(ListingCreatedEvent)
  → RabbitMQ kuyruğu
  → Worker: ListingCreatedConsumer.Consume
  → _searchService.IndexListingAsync(listing)
```

### Seed / Cache Akışı
```
DatabaseSeeder.SeedAsync → PostgreSQL (Cities, Districts, Branches)
CitiesController → GET /api/cities → Redis cache (1 saat, ilçeler dahil)
BranchesController → GET /api/branches → Redis cache (1 saat)
```
Cache sorununda: `docker exec ozelders-redis-1 redis-cli FLUSHALL`

### Worker Servisleri
| Servis | Görev |
|---|---|
| `VitrinExpirationWorker` | Her 1 saatte vitrin süresi dolan ilanları kapatır |
| `ListingCreatedConsumer` | ES indexleme + search cache temizleme |
| `ListingUpdatedConsumer` | ES index güncelleme + listing cache temizleme |
| `ListingDeletedConsumer` | ES'ten silme + cache temizleme |

---

## Sık Kullanılan Komutlar

```powershell
# Tüm sistemi başlat
docker-compose up --build -d

# Sadece bir servisi rebuild et (hızlı)
docker-compose up --build -d api
docker-compose up --build -d web
docker-compose up --build -d worker

# Redis cache temizle
docker exec ozelders-redis-1 redis-cli FLUSHALL

# Worker loglarını canlı izle
docker logs ozelders-worker-1 -f

# Migration ekle
dotnet ef migrations add MigrationAdi --project src/OzelDers.Data --startup-project src/OzelDers.API

# Migration uygula
dotnet ef database update --project src/OzelDers.Data --startup-project src/OzelDers.API
```

---

## Dikkat Edilmesi Gerekenler

1. **MassTransit v8 kalmalı** — v9'a geçme, ücretli lisans istiyor.
2. **Docker rebuild sırası** — Sadece değişen servisi rebuild et, tümünü rebuild etmek 4-5 dakika sürüyor.
3. **Migration zorunlu** — Entity değişikliği yapılınca mutlaka migration ekle.
4. **ES index** — İlk ilan oluşturulana kadar `listings` index'i yok, 404 normal.
5. **`implementation_plan_resolved.md`** — Projenin ana yol haritası. Her yeni özellik öncesi buraya bak.
6. **`.env` dosyası** — Git'e gönderilmez, kaybolursa yukarıdaki değerlerle yeniden oluştur.

---

## Sıradaki Görevler

Ana yol haritası için `docs/implementation_plan_resolved.md` dosyasına bak. Öncelik sırası:

### 1. Discovery Sistemi — Ana Sayfa (Yüksek Öncelik)
`src/OzelDers.SharedUI/Pages/Home.razor` yeniden tasarlanacak:
- Mega-Filter: Dal seçimi (arama yapılabilir dropdown), Şehir/İlçe, Fiyat range slider (0-2000 TL)
- "Sizin İçin Seçtiklerimiz" grid: Aktif ilanlar shuffle edilmiş
- `GET /api/listings/search` endpoint'i kullanılacak

### 2. CreateListing Wizard Form (Yüksek Öncelik)
`src/OzelDers.SharedUI/Pages/UserPanel/CreateListing.razor` 5 adımlı yapıya geçecek:
- Adım 1: İlan türü
- Adım 2: Temel bilgiler + branş (kategorili dropdown — `GET /api/branches/grouped`)
- Adım 3: Ders detayları (süre, fiyat, deneme dersi, bireysel/grup)
- Adım 4: Konum + müsaitlik takvimi
- Adım 5: Deneyim + fotoğraf yükleme + önizleme

Önce entity/DTO güncellemeleri + migration gerekiyor:
- `Listing` entity'sine: `EducationLevel`, `ExperienceYears`, `LessonDurationMinutes`, `HasTrialLesson`, `AvailabilityJson`
- `BranchesController`'a `GET /api/branches/grouped` endpoint'i

### 3. Mobil UI Standartları (Orta Öncelik)
- Tüm tıklanabilir alanlar min 48x48px
- Input'larda min 16px font (iOS auto-zoom engeli)
- Safe area padding
- `PageHeader.razor` bileşeni: her sayfada geri butonu

### 4. AI Moderasyon + Ban Sistemi (Orta Öncelik)
- Strike sistemi: 5 ihlal → 1 hafta, 8 → 1 ay, 11 → kalıcı ban
- Middleware seviyesinde ban kontrolü
- ML.NET PII Classifier (Worker'da)

### 5. Android Bat Scripti (Düşük Öncelik)
- `baslat_android.bat` — emülatör başlatma + MAUI deploy
