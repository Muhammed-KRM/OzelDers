# Firebase Hızlı Kurulum - Adım Adım

Bu rehber Firebase projesini hızlıca kurmak için gerekli adımları içerir.

## 1. Firebase Console'da Proje Oluşturma

### Adım 1: Firebase Console'a Git
1. Tarayıcıda [https://console.firebase.google.com/](https://console.firebase.google.com/) adresine git
2. Google hesabınla giriş yap

### Adım 2: Yeni Proje Oluştur
1. "Add project" butonuna tıkla
2. Proje adı: `ozelders-app` yaz
3. "Continue" butonuna tıkla
4. Google Analytics'i etkinleştir (önerilen)
5. Analytics hesabı seç veya yeni oluştur
6. "Create project" butonuna tıkla
7. Proje oluşturulmasını bekle (1-2 dakika)

## 2. Cloud Messaging (FCM) Kurulumu

### Adım 3: Project Settings'e Git
1. Sol üstteki ⚙️ (Settings) ikonuna tıkla
2. "Project settings" seçeneğini seç
3. "Cloud Messaging" sekmesine tıkla

### Adım 4: Server Key'i Al
1. "Cloud Messaging API (Legacy)" bölümünü bul
2. Eğer devre dışıysa "Enable Cloud Messaging API" butonuna tıkla
3. "Server key" değerini kopyala (örnek: `AAAAxxxxxxx:APA91bH...`)

## 3. Web App Ekleme

### Adım 5: Web App Kaydı
1. Project Overview sayfasına dön
2. "Web" ikonuna (</>) tıkla
3. App nickname: `ozelders-web` yaz
4. "Also set up Firebase Hosting" kutusunu işaretle (isteğe bağlı)
5. "Register app" butonuna tıkla

### Adım 6: Firebase Config'i Al
1. Firebase SDK configuration objesini kopyala:
```javascript
const firebaseConfig = {
  apiKey: "AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
  authDomain: "ozelders-app.firebaseapp.com",
  projectId: "ozelders-app",
  storageBucket: "ozelders-app.appspot.com",
  messagingSenderId: "123456789012",
  appId: "1:123456789012:web:abcdef1234567890abcdef"
};
```

## 4. Uygulama Konfigürasyonu

### Adım 7: appsettings.Development.json Güncelle
`src/OzelDers.API/appsettings.Development.json` dosyasını aç ve Firebase bölümünü güncelle:

```json
{
  "Firebase": {
    "ServerKey": "BURAYA_SERVER_KEY_YAPISTIR",
    "ProjectId": "ozelders-app",
    "Enabled": true
  }
}
```

**Örnek:**
```json
{
  "Firebase": {
    "ServerKey": "AAAAxxxxxxx:APA91bH...",
    "ProjectId": "ozelders-app", 
    "Enabled": true
  }
}
```

## 5. Test Etme

### Adım 8: Uygulamayı Başlat
```bash
# Docker container'ları başlat (eğer çalışmıyorsa)
docker-compose up -d

# API'yi başlat
dotnet run --project src/OzelDers.API

# Web uygulamasını başlat (başka terminal'de)
dotnet run --project src/OzelDers.Web
```

### Adım 9: Admin Panelinde FCM Test
1. Tarayıcıda `https://localhost:7001` adresine git
2. Admin hesabıyla giriş yap:
   - Email: `admin@ozelders.com`
   - Şifre: `Admin123!`
3. Admin Dashboard'a git: `/admin/dashboard`
4. "FCM Test" bölümünü bul
5. Test değerleri gir:
   - **Token:** `test-token-123` (geçici test için)
   - **Title:** `Test Bildirimi`
   - **Body:** `Bu bir test mesajıdır`
6. "Gönder" butonuna tıkla

### Adım 10: API Endpoint Test
Postman veya curl ile test:

```bash
curl -X POST "https://localhost:7001/api/admin/test-fcm" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "token": "test-token-123",
    "title": "Test Notification", 
    "body": "This is a test message"
  }'
```

## 6. Gerçek FCM Token Alma (Web)

### JavaScript ile FCM Token
Web uygulamasında gerçek FCM token almak için:

1. `src/OzelDers.Web/wwwroot/js/firebase.js` dosyası oluştur:

```javascript
// Firebase konfigürasyonu (Adım 6'dan aldığın config)
const firebaseConfig = {
  apiKey: "AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
  authDomain: "ozelders-app.firebaseapp.com", 
  projectId: "ozelders-app",
  storageBucket: "ozelders-app.appspot.com",
  messagingSenderId: "123456789012",
  appId: "1:123456789012:web:abcdef1234567890abcdef"
};

// Firebase'i başlat
import { initializeApp } from 'firebase/app';
import { getMessaging, getToken } from 'firebase/messaging';

const app = initializeApp(firebaseConfig);
const messaging = getMessaging(app);

// FCM token al
getToken(messaging, { 
  vapidKey: 'YOUR_VAPID_KEY' // Firebase Console'dan alınacak
}).then((currentToken) => {
  if (currentToken) {
    console.log('FCM Token:', currentToken);
    
    // Token'ı backend'e gönder
    fetch('/api/users/fcm-token', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token: currentToken })
    });
  } else {
    console.log('FCM token alınamadı');
  }
}).catch((err) => {
  console.log('FCM token hatası:', err);
});
```

## 7. Sorun Giderme

### Firebase Server Key Bulunamıyor
- Firebase Console > Project Settings > Cloud Messaging
- "Cloud Messaging API (Legacy)" etkinleştir
- Server key'i kopyala

### FCM Test Başarısız
- Server key'in doğru kopyalandığından emin ol
- appsettings.json'da `Enabled: true` olduğunu kontrol et
- API'nin çalıştığından emin ol

### Token Geçersiz Hatası
- Gerçek FCM token kullan (JavaScript ile al)
- Token'ın süresi dolmamış olduğundan emin ol

## 8. Sonraki Adımlar

1. **VAPID Key Alma:** Firebase Console > Project Settings > Cloud Messaging > Web configuration
2. **Service Worker:** Push notification'lar için service worker ekle
3. **Android App:** Firebase Console'da Android app ekle
4. **Production:** Environment variables ile server key'i güvenli hale getir

Bu adımları tamamladıktan sonra Firebase FCM sistemi tam çalışır durumda olacak!