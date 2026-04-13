# OzelDers - UI Sanat Felsefesi ve Tasarım Kılavuzu (Faz 6)

Bu döküman, projede oluşturulacak Blazor Web arayüzünün (UI) genel ruhunu, estetiğini ve kullanılacak ana görsel kuralları belirler. "Özel Ders" kavramının barındırdığı **"Güven/Disiplin"** ve **"Gelişim/Heyecan"** duygularını dengeli bir şekilde yansıtmak üzerine tasarlanmıştır.

## 1. Sanat Felsefesi: "Modern ve Erişilebilir (Friendly & Premium)"
Eğitim, soğuk veya sıkıcı bir deneyim olmamalı; tam tersine davetkâr ve ilham verici olmalıdır.
*   **Genel Hissiyat:** Ferah (çokça beyaz/boşluk alanı), dinamik ve premium. İnsanların saatlerce yorulmadan gezinebileceği, temiz ve modern bir startup havası.
*   **Geometri ve Şekiller (Shapes):** Keskin ve sivri köşelerden **uzak durulacak**. Kartlar, butonlar ve görseller **yumuşak köşeli (Soft-Rounded)** olacak (Örn: `border-radius: 16px` veya `24px`). Yuvarlak hatlar psikolojik olarak "arkadaş canlısı ve güvenli" bir his verir.
*   **Görsellerin Tarzı (Art Style):** 
    *   Sıradan, düz stock fotoğraflar yerine; asıl odak noktası obje/insan olan, **arka planı temizlenmiş (cut-out) gerçek insan/öğretmen fotoğrafları**.
    *   Bu fotoğrafların arkasında hafif asimetrik, renkli soyut şekiller (blob'lar) veya Glassmorphism (buzlu cam) paneller kullanılacak (2.5D derinlik hissi).
    *   İkonlar veya destekleyici grafikler için **Modern 3D Clay (Oyun hamuru benzeri mat 3D) illüstrasyonlar**. Bu tarz şu an web'de "Wow" efekti yaratan en güncel UI trendidir.

## 2. Renk Teorisi: "60-30-10 Kuralı"
Birbirini destekleyen ve göz yormayan, ancak aksiyona yönlendiren bir renk paleti. Öğrenme kavramı için hem güven veren soğuk tonlar hem de enerjik sıcak tonlar harmanlandı.

### 🎨 Renk Paleti Önerisi: "Köprü (The Bridge)"
*   **%60 Ana Tema Rengi (Background & Whitespace):** 
    *   *Kar Beyazı ve İnci Grisi (`#FFFFFF` ve `#F8F9FA`)*
    *   Platformun arka planını kaplayacak, içeriklerin (ilanların) nefes almasını sağlayacak geniş ve ferah, göz yormayan açık zemin tonları.
*   **%30 İkincil Renk (Typography, Shapes & Depth):** 
    *   *Gece Mavisi / Koyu Indigo (`#1E293B` veya `#2A2356`)*
    *   Siyah renk kullanmak okumayı zorlaştırır ve keskindir. Bunun yerine yazılarda, footer (alt bilgi) kısmında ve ana gölgelemelerde tok bir "Gece Mavisi" kullanacağız. Bu renk eğitime dair **"Güven, Akademik Duruş ve Disiplin"** hissini verir.
*   **%10 Vurgu/Aksiyon Rengi (CTA & Highlights):** 
    *   *Canlı Mercan / Büyüleyici Şeftali (`#FA626B` veya `#FF7E67`)*
    *   İnsan gözü doğası gereği kırmızı/turuncu spektrumuna anında çekilir. Sitedeki "Kayıt Ol", "Mesaj Gönder", "Ödeme Yap" butonlarında, hover (üzerine gelme) efektlerinde ve Vitrin yıldızlarında kullanılacak. Mavi ile inanılmaz bir kontrast oluşturur ve **"İlham, Heyecan, Harekete Geçme"** hissi yaratır.

*(Alternatif: Eğer kullanıcı daha kurumsal hissetsin istenir ise %10 Vurgu rengi Limon Sarısı / Altın veya Mint Yeşili de yapılabilir).*

## 3. Tipografi ve Hiyerarşi
Sitenin kimliğini yansıtan şey fontudur. Klasik ve sıkıcı sistem fontları yerine:
*   **Başlıklar (Headings):** *Outfit* veya *Plus Jakarta Sans* (Modern, geometrik, okunaklı, dostane).
*   **Gövde Metinleri (Body Text):** *Inter* (Ufak yazılarda bile dünyadaki en okunabilir modern ekran fontu).

## 4. Etkileşim ve Animasyonlar (Micro-animations)
Sıradan ve ölü bir tasarımdan kaçınmak için sayfa "canlı" hissettirmeli:
*   Öğretmen kartlarının üzerine gelindiğinde (Hover) kartın hafifçe **kalkması (yükselmesi)** ve derin, yumuşak bir gölge (`box-shadow`) bırakması.
*   Butonlara tıklandığında veya fare üzerine geldiğinde hafifçe büyüme/küçülme (Scale efekti).
*   Sayfa aşağı kaydırıldığında (On Scroll) menünün veya arka plandaki soyut şekillerin **Yumuşak Parallax** efekiyle (farklı hızda) hareket etmesi.

## 5. UI Bileşenlerinin (Components) Felsefesi
*   **Glassmorphism (Buzlu Cam):** Ana menüde (Navbar) veya bazı kartlarda arka planı hafifçe bulanıklaştırarak arkadaki rengin sızmasına izin veren materyalist tasarım kullanılacak. Çok abartılmadan, sadece yüksek vurgu isteyen alanlarda.
*   **Geniş Boşluklar (Spacious Design):** Elemanlar (yazılar, butonlar, resimler) arasına bilerek daha fazla boşluk (`padding` ve `margin`) eklenecek. Çok sıkışık bir pazar yeri tasarımı yerine "Premium Eğitmen Platformu" imajı verilecek.

## 6. Mobil ve Platform Standartları (UX)
MAUI Blazor Hybrid mimarisinde native hissi vermek için şu kurallar esastır:
*   **Touch Targets:** Tüm butonlar ve etkileşimli alanlar minimum **48x48px** boyutunda olmalıdır.
*   **Güvenli Alanlar (Safe Areas):** Cihaz çentikleri ve alt navigasyon çubukları için `env(safe-area-inset-top/bottom)` değerleri global CSS ile yönetilmelidir.
*   **Global Geri Butonu:** Her alt sayfada sol üstte sabit, yüksek kontrastlı ve her dilde/kültürde geçerli bir geri oku (←) bulunmalıdır. Bu buton "Glassmorphism" efektiyle her arka planda belirgin kılınacaktır.
*   **Input Focus:** iOS'ta otomatik yakınlaştırmayı engellemek için tüm inputlar odaktayken minimum `16px` font boyutunda kalmalıdır.

## 7. Kategori Bazlı Renk Psikolojisi (Dinamik Temalar)
Sitenin ana renkleri korunurken, keşif (Discovery) sayfasında kategori bazlı "vurgu renkleri" kullanılacaktır:
*   **YKS & Eğitim:** `#1E293B` (Serious Blue) - Disiplin ve güven odaklı.
*   **Müzik & Sanat:** `#8B5CF6` (Sanatsal Mor) - Yaratıcılık odaklı.
*   **Yazılım & Teknoloji:** `#10B981` (Tech Green) - İleri teknoloji ve modernite odaklı.
*   **Spor & Aktivite:** `#F59E0B` (Energetic Amber) - Hareket ve enerji odaklı.

---
**Özet:** Keskin olmayan, 3D materyal ikonlarla desteklenmiş, arka planların ferah olduğu, Gece Mavisi ile güven verip Canlı Mercan kırmızılarıyla kullanıcıyı harekete geçiren "Apple/Modern Startup" pürüzsüzlüğünde bir tasarım inşa edeceğiz. Mobil tarafta ise devasa tıklama alanları ve tutarlı navigasyon ile "Native Uygulama" hissini kusursuzca vereceğiz.
