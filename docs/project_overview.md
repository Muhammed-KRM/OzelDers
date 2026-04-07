# 🎓 Özel Ders İlan Platformu — Proje Özeti ve Vizyon

## 🌟 Proje Özeti (Elevator Pitch)
Bu platform, bilgi birikimini paylaşmak isteyen eğitmenler ile kişisel gelişimine yatırım yapmak isteyen öğrencileri modern bir **İki Yönlü Pazaryerinde (Two-Way Marketplace)** buluşturur. Herhangi bir rol kısıtlaması olmadan, her kullanıcı "Ders Vermek İstiyorum" veya "Ders Almak İstiyorum" şeklinde ilan açabilir. **"Lead & Showcase" (İletişim ve Görünürlük)** modelini benimseyen sistem, esnek "Jeton Sistemi" sayesinde kullanıcıların birbirleriyle direkt etkileşime girmesini sağlar.

---

## 🎯 Projenin Amacı
Projenin temel amacı, Türkiye'deki özel ders ekosistemini dijitalleştirerek herkesin kolayca hizmet sunabildiği ve hizmet bulabildiği bir ağ kurmaktır.
*   **İki Yönlü İlanlar:** Öğretmenler yeteneklerini sergileyebilir, öğrenciler ise ihtiyaçlarını ("YKS Mentörü Arıyorum") ilan edebilir.
*   **Hızlı Eşleşme:** Kullanıcılar ilanlara ücretsiz başvurabileceği gibi, kendi ilanlarını "Direkt Mesaj" ile beğendikleri profillere "Teklif" olarak da sunabilirler.
*   **İşletme İçin:** Ders başına komisyonla uğraşmadan, her iki tarafın (öğrenci/öğretmen) kullandığı esnek jeton ve vitrin satışları üzerinden büyüyen bir gelir modeli.

---

## 💎 Temel İş Modeli (İki Yönlü Jeton Sistemi)
Platform, komisyon almaz. Bunun yerine dinamik bir Jeton ve Vitrin sisteminden gelir elde eder:

1. **Normal Senaryo (İlan Üzerinden):** Bir kullanıcı (A) bir ilan açar. Diğer kullanıcı (B) bu ilanı görüp **ücretsiz** mesaj atar. A kişisi, gelen bu mesajı okuyabilmek ve cevaplayabilmek için kendi **jetonunu** harcar.
2. **Direkt Teklif Senaryosu (Reklam):** Bir kullanıcı (A) ilanını açmıştır. Kullanıcı (B)'nin ilanı yoktur ama A'nın profiline girer ve kendi ilanını (Örn: "Ben YKS Mentörüyüm") B'nin mesaj kutusuna **jeton harcayarak** direkt teklif olarak gönderir. B kişisi bu mesaja (başlatan taraf jeton harcadığı için) **ücretsiz** cevap verebilir.
3. **Vitrin (Doping) Sistemi:** Kullanıcılar, ilanlarını (öğretmen veya öğrenci ilanı fark etmeksizin) ana sayfada en üste taşımak için paket satın alırlar.

---

## 📱 Uygulama Akışı ve Menü Yapısı (Tek Tip Kullanıcı)

### 1. Ana Sayfa (Vitrin ve Keşif)
Kullanıcının ilk karşılaştığı, güven veren ve harekete geçiren bölümdür.
*   **Hero Section:** "Ne öğrenmek istersin?" arama çubuğu (branş ve şehir bazlı).
*   **Popüler Branşlar:** Matematik, İngilizce, Piyano, Yazılım vb. hızlı erişim kartları.
*   **Öne Çıkanlar:** Vitrin paketi satın almış öğretmenlerin şık kartları.
*   **İstatistikler:** "10.000+ Eğitmen", "50.000+ Başarılı Ders" gibi güven veren sayaçlar.

### 2. Arama ve Filtreleme (Hızlı Sonuç)
*   **Gelişmiş Filtreler:** Fiyata göre, ders tipine (online/yüz yüze), öğretmenin tecrübesine ve puana göre daraltma.
*   **Harita Görünümü:** (Opsiyonel) Öğretmenlerin konuma göre harita üzerinde gösterilmesi.

### 3. İlan Detay (Öğretmen veya Öğrenci İlanı)
*   **Profil ve İlan Kartı:** Fotoğraf, isim, branş, ihtiyaç duyulan veya verilen hizmet.
*   **İki Farklı Buton:** 
    1. İlana Başvur (Öcretsiz) 
    2. İlanını Öner / Teklif Ver (Jeton Harcar, direkt DM'e düşer).

### 4. Kullanıcı Paneli (Unified Dashboard)
Herkesin tek bir hesabı vardır (Öğretmen/Öğrenci ayrımı yoktur).
*   **İlanlarım:** "Ders Veriyorum" ve "Ders Arıyorum" tiplerindeki tüm ilanların yönetimi.
*   **Mesajlarım:**
    *   *Teklif Gelenler:* İlana gelen (Jetonla açılacak) mesajlar.
    *   *Direkt Gelenler:* Başkasının jetonla gönderdiği (Ücretsiz açılacak) mesajlar.
*   **Jeton ve Cüzdan:** Bakiye takibi ve jeton satın alma ekranı.
*   **Profil Ayarları:** Temel bilgiler, IBAN ve onay belgeleri doğrulaması.

### 6. Admin Paneli (Kontrol Kulesi)
*   **İlan Onayı:** Yeni ilanları KVKK ve site kurallarına göre denetleme.
*   **Kullanıcı Yönetimi:** Üyeleri görüntüleme, askıya alma veya destek verme.
*   **Finansal Raporlar:** Günlük satışlar ve toplam ciro takibi.

---

## 🛠️ Teknik Nedenler (Neden Bu Altyapı?)
*   **Blazor & MAUI:** Teknoloji birliği sağlar. Web siteni Google bulabilir (**SEO**), mobil uygulamanı ise kullanıcı cebinde taşır.
*   **PostgreSQL & Elasticsearch:** Verilerin güvenli saklanması (Postgre) ve devasa ilan yığınları arasında ışık hızında arama (Elastic) dengesini kurar.
*   **RabbitMQ:** E-posta bildirimi ve fotoğraf işleme gibi yan işlerin ana siteyi asla yavaşlatmamasını sağlar.

---

## 🎯 Sonuç
Bu uygulama, sadece bir ilan sitesi değil; eğitimde fırsat eşitliği sağlayan ve kaliteli eğitmeni görünür kılan dijital bir köprüdür. Tamamen ölçeklenebilir ve modern bir mimariyle, yarın 100.000 kullanıcıya ulaştığında bile aynı performansla çalışacak şekilde tasarlanmıştır.
