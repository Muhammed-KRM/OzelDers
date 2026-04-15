# OzelDers — Icerik Moderasyon ve Ban Sistemi Tasarim Dokumani

**Versiyon:** 2.0  
**Tarih:** Nisan 2026  
**Durum:** Tasarim Asamasi

---

## 1. Sorunun Gercek Boyutu: Disintermediation

Bu sistemin varlik nedeni sadece "kotu icerik" degil, platformun gelir modelini dogrudan tehdit eden **disintermediation** (platform atlama) sorunudur.

Kullanici A bir ilan acar. Kullanici B ilani gorur, ama platform uzerinden mesajlasmak yerine ilandaki telefon numarasini alip direkt arar. Sonuc: Platform hic jeton kazanmaz, hic komisyon almaz, iki kullanici da bir daha donmayabilir.

Sharetribe, Airbnb, Etsy, Sahibinden gibi platformlarin tumu bu sorunla mucadele eder. Arastirmalar gosteriyor ki disintermediation, marketplace is modellerinin "Acikhali" (Achilles heel) olarak tanimlanmaktadir. Cozum iki ayaklidir:

1. **Teknik engel:** Telefon/email tespiti ve engelleme
2. **Deger onerisi:** Platformda kalmak icin yeterli sebep vermek (jeton sistemi, guven, sikayet mekanizmasi)

Bu dokuman teknik engel kismini ele alir.

---

## 2. Gercek Dunya Ornekleri: Buyuk Platformlar Ne Yapiyor?

### Airbnb
- Mesajlasma sisteminde telefon numarasi ve email tespiti icin NLP kullanir
- Makine ogrenimi ile "yuksek riskli rezervasyon" tespiti yapar (yuzlerce sinyal analiz eder)
- Kural ihlalinde hesap askiya alma, tekrarda kalici ban

### Etsy (En Iyi Ornek)
Etsy'nin yaklasimi bu proje icin en uygun referanstir:
- **Katmanli sistem:** Otomatik ML modeli + insan moderator
- **Labeled dataset:** Moderatorler ihlalleri etiketler, bu veri modeli egitmek icin kullanilir
- **Skor sistemi:** Her ilana "ihlal skoru" atanir, yuksek skorlular insan incelemesine gider
- **Surekli guncelleme:** Yeni bypass taktikleri ogrenilince model yeniden egitilir
- **Sonuc:** 2024'te yanlis pozitif oranini %70 azaltti, %25 daha az ilan kaldirildi

### Sahibinden / Letgo
- Regex tabanli telefon tespiti (temel seviye)
- Kullanici sikayet sistemi (insan moderasyonu agirlikli)
- Tekrarda hesap askiya alma

### Facebook Marketplace
- NLP + goruntu analizi (OCR ile fotograftaki telefon tespiti)
- Otomatik uyari mesaji, tekrarda ilan kaldirilmasi

---

## 3. Bypass Taktikleri: Kullanicilar Ne Yapar?

Moderasyon sistemi tasarlarken once saldiri vektorlerini anlamak gerekir:

### 3.1 Standart Formatlar (Regex ile yakalanir)
- `05321234567`
- `0532 123 45 67`
- `+90 532 123 45 67`
- `0532.123.45.67`
- `(0532) 123-45-67`

### 3.2 Turkce Yazi ile Bypass (NLP gerektirir)
- `"sifir bes uc iki bir iki uc dort bes alti yedi"`
- `"s-i-f-i-r b-e-s..."`
- `"0 bes 32 yuz yirmi uc kirk bes altmis yedi"`

### 3.3 Unicode Homoglyph Saldirisi (Normalizasyon gerektirir)
Kiril alfabesindeki "а" (U+0430) Latin "a" (U+0061) ile gorusel olarak ayni gorunur.
- `"05З2 1234567"` — Z yerine Kiril Z
- `"mаil@gmail.com"` — Kiril "а" kullanilmis

### 3.4 Sembolik Obfuskasyon
- `"05*32*123*45*67"` — yildiz ile ayrilmis
- `"05[32]123-45-67"` — kose parantez
- `"beni ara: sifir bes uc iki..."` — cumle icine gomulmus

### 3.5 Gorsel Bypass (Kapsam Disi - v2)
- Fotografa yazilmis telefon numarasi (OCR gerektirir)
- Ekran goruntusu olarak yuklenmis numara

---

## 4. Mimari: Uc Katmanli Savunma

```
Kullanici ilan gonderir
        |
        v
+---------------------------+
|  KATMAN 1: Anlik Regex    |  <- API Controller, <5ms, sync
|  + Unicode normalizasyon  |
|  + Homoglyph temizleme    |
+----------+----------------+
           | Temiz gecerse
           v
+---------------------------+
|  KATMAN 2: Turkce NLP     |  <- Worker servisi, async, ~200ms
|  Yazi ile yazilmis bypass |
|  "sifir bes uc iki..."    |
+----------+----------------+
           | Ihlal tespit edilirse
           v
+---------------------------+
|  KATMAN 3: Strike Sistemi |  <- DB'ye ihlal kaydi
|  5  -> 1 hafta ban        |
|  8  -> 1 ay ban           |
|  11 -> kalici ban         |
+----------+----------------+
           | Ban aktifse
           v
+---------------------------+
|  BAN MIDDLEWARE           |  <- Her istekte kontrol
|  403 + Ban bilgi sayfasi  |
+---------------------------+
```

---

## 5. Katman 1: Anlik Regex + Normalizasyon

### 5.1 On Islem Pipeline

Regex'ten once metin normalize edilir:

```
Ham metin
    |
    v
1. Unicode normalizasyonu (NFC)
2. Homoglyph temizleme (Kiril/Yunan -> Latin)
3. Regex taramasi
```

### 5.2 Homoglyph Temizleme

```csharp
private static readonly Dictionary<char, char> HomoglyphMap = new()
{
    ['\u0430'] = 'a',  // Kiril a
    ['\u0435'] = 'e',  // Kiril e
    ['\u043E'] = 'o',  // Kiril o
    ['\u0440'] = 'p',  // Kiril r
    ['\u0441'] = 'c',  // Kiril s
    ['\u0445'] = 'x',  // Kiril x
    ['\u03BF'] = 'o',  // Yunan omicron
    ['\u03B1'] = 'a',  // Yunan alpha
};

public static string CleanHomoglyphs(string text)
{
    var sb = new StringBuilder(text.Length);
    foreach (var c in text)
        sb.Append(HomoglyphMap.TryGetValue(c, out var clean) ? clean : c);
    return sb.ToString();
}
```

### 5.3 Turkce Telefon Regex Desenleri

```csharp
private static readonly Regex[] PhonePatterns =
[
    // Standart Turk mobil: 05XX ile baslayan, tum format varyasyonlari
    new(@"(\+90|0090|0)[\s\-\.\(\)\*\[\]\/\\]?[5][0-9][\s\-\.\(\)\*\[\]\/\\]?\d{2}[\s\-\.\(\)\*\[\]\/\\]?\d{3}[\s\-\.\(\)\*\[\]\/\\]?\d{2}[\s\-\.\(\)\*\[\]\/\\]?\d{2}",
        RegexOptions.Compiled),

    // Sadece rakamlardan olusan 11 haneli (05 ile baslayan)
    new(@"\b0[5][0-9]\d{8}\b", RegexOptions.Compiled),

    // Bosluklu/ayracli 10 haneli (5XX ile baslayan, basta 0 yok)
    new(@"\b[5][0-9]\d[\s\-\.\(\)]\d{3}[\s\-\.\(\)]\d{2}[\s\-\.\(\)]\d{2}\b",
        RegexOptions.Compiled),
];
```

### 5.4 Email ve Link Desenleri

```csharp
// Email - standart ve obfuskasyonlu
private static readonly Regex EmailPattern = new(
    @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

// Harici link
private static readonly Regex LinkPattern = new(
    @"(https?://[^\s]+|\bwww\.[a-zA-Z0-9\-]+\.[a-zA-Z]{2,})",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

// Sosyal medya handle (@kullanici)
private static readonly Regex SocialHandlePattern = new(
    @"(?<![a-zA-Z])@[a-zA-Z0-9_]{3,}",
    RegexOptions.Compiled);
```

---

## 6. Katman 2: Turkce NLP Normalizasyonu

### 6.1 Turkce Rakam Sozlugu

```csharp
private static readonly Dictionary<string, string> TurkishNumbers = new()
{
    ["sifir"] = "0", ["bir"] = "1", ["iki"] = "2", ["uc"] = "3",
    ["dort"] = "4", ["bes"] = "5", ["alti"] = "6", ["yedi"] = "7",
    ["sekiz"] = "8", ["dokuz"] = "9",
    // Onluklar (telefon numarasinda nadiren kullanilir ama olabilir)
    ["on"] = "10", ["yirmi"] = "20", ["otuz"] = "30", ["kirk"] = "40",
    ["elli"] = "50", ["altmis"] = "60", ["yetmis"] = "70",
    ["seksen"] = "80", ["doksan"] = "90",
};
```

### 6.2 Islem Akisi

```
Ilan olusturuldu (Status=PendingModeration)
        |
        v  RabbitMQ -> ListingCreatedConsumer
        |
        +-- TurkishNumberNormalizer.Normalize(text)
        +-- ContentModerationService.Check(normalizedText)
        |
        +-- Temiz  -> Status=Active
        +-- Ihlal  -> Status=Rejected + StrikeService.AddStrike(userId)
                                      + Email bildirimi
```

### 6.3 Neden ML.NET Degil?

ML.NET Binary Classification icin minimum 500-1000 etiketli ornek gerekir. Platformun baslangic asamasinda bu veri yok. Kural tabanli normalizasyon + Regex su an icin %90+ dogruluk saglar ve sifir dis bagimlilik ile calisir.

**Gelecek:** Yeterli ihlal verisi birikince (tahminen 6-12 ay sonra) ML.NET modeli eklenebilir. ViolationLog tablosu bu veriyi zaten biriktirecek.

---

## 7. Strike ve Ban Sistemi

### 7.1 Veritabani Degisiklikleri

User entity'sine eklenecek alanlar:

```csharp
public int ViolationCount { get; set; } = 0;
public DateTime? BannedUntil { get; set; }  // DateTime.MaxValue = kalici ban
public DateTime? LastViolationAt { get; set; }
public string? BanReason { get; set; }
```

Yeni tablo: ViolationLog

```csharp
public class ViolationLog
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ListingId { get; set; }
    public string ListingTitle { get; set; } = "";
    public ViolationType Type { get; set; }
    public string DetectedContent { get; set; } = "";  // Maskelenmis
    public ModerationLayer DetectedBy { get; set; }    // Regex, NLP, Admin
    public bool IsManual { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 7.2 Ceza Cetveli

| Ihlal Sayisi | Ceza       | Sure          |
|---|---|---|
| 1-4          | Uyari      | Ban yok       |
| 5            | 1. Ban     | 7 gun         |
| 6-7          | Uyari      | Ban bitti     |
| 8            | 2. Ban     | 30 gun        |
| 9-10         | Uyari      | Ban bitti     |
| 11+          | Kalici Ban | DateTime.Max  |

### 7.3 StrikeService

```csharp
private static TimeSpan? CalculateBan(int count) => count switch
{
    >= 11 => TimeSpan.MaxValue,
    8     => TimeSpan.FromDays(30),
    5     => TimeSpan.FromDays(7),
    _     => null
};
```

---

## 8. Ban Middleware

```csharp
// Sira onemli: Authentication'dan SONRA, Authorization'dan ONCE
app.UseAuthentication();
app.UseMiddleware<BanCheckMiddleware>();
app.UseAuthorization();
```

Ban kontrolu Redis cache ile optimize edilir:
- Cache key: `ban:{userId}`
- TTL: BannedUntil - DateTime.UtcNow
- Her istekte DB'ye gitmek yerine cache'den kontrol

---

## 9. Admin Paneli

Yeni endpoint'ler:
```
GET  /api/admin/violations
GET  /api/admin/violations/{userId}
POST /api/admin/users/{userId}/ban
POST /api/admin/users/{userId}/unban
POST /api/admin/users/{userId}/reset-violations
```

---

## 10. Uygulama Sirasi

| Adim | Is                                    | Sure    |
|---|---|---|
| 1    | User entity + migration               | 30 dk   |
| 2    | ViolationLog entity + migration       | 20 dk   |
| 3    | ContentModerationService (Regex)      | 1 saat  |
| 4    | TurkishNumberNormalizer               | 45 dk   |
| 5    | StrikeService                         | 1 saat  |
| 6    | BanCheckMiddleware + Redis cache      | 1 saat  |
| 7    | ListingsController entegrasyonu       | 30 dk   |
| 8    | Worker consumer guncelleme            | 1 saat  |
| 9    | Admin endpoint'leri                   | 1.5 saat|
| 10   | UI - Ban sayfasi                      | 1 saat  |
| **Toplam** |                               | ~9 saat |

---

## 11. Test Senaryolari

```csharp
[Theory]
[InlineData("0532 123 45 67", true)]
[InlineData("+90 532 123 45 67", true)]
[InlineData("test@gmail.com", true)]
[InlineData("www.google.com", true)]
[InlineData("Matematik dersi veriyorum", false)]
public void Check_ShouldDetectViolation(string content, bool expected) { }

[Theory]
[InlineData("sifir bes uc iki", "0532")]
[InlineData("sifir bes", "05")]
public void Normalize_ShouldConvertTurkishNumbers(string input, string expected) { }
```

---

## 12. Gelecek Gelistirmeler (v2)

- **ML.NET Binary Classifier:** 500+ ihlal ornegi birikince
- **Gorsel Moderasyon:** Fotograftaki telefon tespiti (OCR)
- **Rate Limiting:** Ayni IP'den cok sayida ihlal -> IP ban
- **Kullanici Itiraz Sistemi:** Yanlis tespit icin itiraz formu
- **Admin Bildirim:** Slack/Telegram webhook

---

## 13. Neden Bu Yaklasimi Sectik: Kapsamli Karar Analizi

Bu bolum, diger alternatifleri neden reddettigimizi ve sectigimiz yaklasimin neden dogru oldugunu aciklar.

### 13.1 Tespit Teknolojisi Karsilastirmasi

**Secenek A: Saf Regex**
- Artilari: Sifir maliyet, <1ms gecikme, sifir bagimlilik
- Eksileri: Yazili bypass'lari yakalamiyor ("sifir bes..."), Unicode homoglyph'leri yakalamiyor
- Sonuc: YETERSIZ. Sahibinden'in ilk nesil sistemi buydu ve kullanicilar hizla bypass yollarini buldu.

**Secenek B: Regex + Unicode Normalizasyon + Turkce NLP (SECTIGIMIZ)**
- Artilari: Sifir maliyet, <10ms gecikme, sifir dis bagimlilik, %90+ dogruluk, Turkce'ye ozel
- Eksileri: Gorsel bypass'lari yakalamiyor (v2'ye birakildi)
- Sonuc: DOGRU SECIM. Platformun mevcut stack'ine tam uyumlu, baslangic icin yeterli.

**Secenek C: ML.NET Binary Classification**
- Artilari: Yuksek dogruluk potansiyeli, bypass'lara karsi dayanikli
- Eksileri: Minimum 500-1000 etiketli Turkce ornek gerektirir (elimizde yok), model egitimi karmasik, false positive riski yuksek (Etsy'nin deneyimi: yanlis etiketleme buyuk sorun), baslangic icin overkill
- Sonuc: REDDEDILDI. Veri olmadan model olmaz. ViolationLog ile veri birikince v2'de eklenecek.

**Secenek D: OpenAI Moderation API**
- Artilari: Kolay entegrasyon, genel icerik moderasyonu
- Eksileri: Turkce telefon bypass'larina karsi zayif (Ingilizce odakli egitilmis), her ilan icin API maliyeti (1000 ilan/gun = ~$0.50-2/gun, yillik $180-730), veri gizliligi sorunu (ilan icerigi OpenAI'a gidiyor), internet baglantisi gerektiriyor
- Sonuc: REDDEDILDI. Maliyet + Turkce zayifligi + gizlilik.

**Secenek E: Azure Content Moderator / Sightengine**
- Artilari: Guclu, hazir API
- Eksileri: Ucretli ($1-5/1000 istek), dis bagimlilik, Turkce PII icin optimize degil, KVKK acisindan veri transferi sorunu
- Sonuc: REDDEDILDI. Gereksiz maliyet ve bagimlilik.

**Secenek F: LLM Tabanli (GPT-4, Claude)**
- Artilari: En yuksek dogruluk, nuans anlayisi
- Eksileri: Cok pahali (her ilan icin ~$0.01-0.05), yuksek gecikme (1-3 saniye), overkill
- Sonuc: REDDEDILDI. Bir ilan platformu icin LLM kullanmak top ile sinek avlamak gibi.

### 13.2 Ban Sistemi Karsilastirmasi

**Secenek A: Anlik Hesap Kapatma**
- Tek ihlalde hesap kapatmak kullanicilari kacirir, yanlis pozitif durumunda telafisi zor
- Sonuc: REDDEDILDI.

**Secenek B: Sadece Uyari**
- Uyari gonderip hic ban uygulamamak etkisiz, kullanicilar umursamaz
- Sonuc: REDDEDILDI.

**Secenek C: Kademeli Strike Sistemi (SECTIGIMIZ)**
- Airbnb, Reddit, Twitter'in kullandigi standart yaklasim
- Kullaniciya duzeltme sansi verir, yanlis pozitif durumunda telafi edilebilir
- Admin mudahalesine izin verir
- Sonuc: DOGRU SECIM.

### 13.3 Neden Async Worker?

Ilan gonderildiginde kullanici aninda yanit alir (Status=PendingModeration). Worker arka planda NLP analizini yapar. Bu yaklasim:
- Kullaniciyi bekletmez (UX iyilestirmesi)
- API'nin yanit suresini etkilemez
- Worker hata verse bile ilan kaybolmaz, tekrar islenir
- RabbitMQ zaten mevcut altyapida var, ek maliyet yok

Alternatif (sync analiz): API'de NLP yapmak 200-500ms gecikme ekler, her ilan gonderiminde kullanici bekler. Kabul edilemez.

### 13.4 Neden Redis Cache?

Ban kontrolu her HTTP isteğinde yapilir. Gunluk 10.000 istek varsayimiyla, her seferinde DB'ye gitmek:
- 10.000 ekstra DB sorgusu/gun
- Her sorgu ~2-5ms = gunluk 20-50 saniye toplam DB yuku

Redis ile:
- Ilk kontrol DB'den, sonrasi cache'den
- Cache miss orani dusuk (ban durumu sik degismez)
- TTL otomatik yonetim (ban bitince cache expire olur)

Redis zaten mevcut altyapida var (docker-compose.yml'de tanimli).

### 13.5 Ozet: Neden Bu Sistem?

Tek cumleyle: **Mevcut altyapiya sifir ek maliyet ve bagimlilikla entegre olan, Turkce'ye ozel bypass taktiklerini kapsayan, buyume ile birlikte ML.NET'e evrilebilecek pragmatik bir cozum.**

Etsy'nin deneyimi gosteriyor ki ML ile baslayan sistemler bile insan moderasyonu olmadan basarisiz oluyor. Biz once saglamli bir kural tabanli sistem kuruyoruz, veri birikince zekaya geciyoruz. Bu, buyuk platformlarin da izledigi yol.
