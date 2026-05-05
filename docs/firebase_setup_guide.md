# Firebase Proje Kurulum Rehberi

## 1. Firebase Projesi Oluşturma

### Firebase Console'da Proje Oluşturma

1. [Firebase Console](https://console.firebase.google.com/) adresine gidin
2. "Add project" butonuna tıklayın
3. Proje adını girin: `ozelders-app`
4. Google Analytics'i etkinleştirin (isteğe bağlı)
5. Projeyi oluşturun

## 2. Firebase Cloud Messaging (FCM) Kurulumu

### Server Key Alma

1. Firebase Console'da projenizi açın
2. ⚙️ Settings > Project settings
3. "Cloud Messaging" sekmesine gidin
4. "Server key" değerini kopyalayın

### Web App Ekleme

1. Project Overview'da "Web" ikonuna tıklayın
2. App nickname: `ozelders-web`
3. Firebase Hosting'i etkinleştirin (isteğe bağlı)
4. Configuration objesini kopyalayın

### Android App Ekleme

1. Project Overview'da "Android" ikonuna tıklayın
2. Android package name: `com.companyname.ozelders.app`
3. `google-services.json` dosyasını indirin
4. Dosyayı `src/OzelDers.App/Platforms/Android/` klasörüne koyun

### iOS App Ekleme (İsteğe Bağlı)

1. Project Overview'da "iOS" ikonuna tıklayın
2. iOS bundle ID: `com.companyname.ozelders.app`
3. `GoogleService-Info.plist` dosyasını indirin
4. Dosyayı `src/OzelDers.App/Platforms/iOS/` klasörüne koyun

## 3. Uygulama Konfigürasyonu

### appsettings.Development.json Güncelleme

```json
{
  "Firebase": {
    "ServerKey": "YOUR_FIREBASE_SERVER_KEY_HERE",
    "ProjectId": "ozelders-app",
    "Enabled": true
  }
}
```

### appsettings.json Güncelleme (Production)

```json
{
  "Firebase": {
    "ServerKey": "PRODUCTION_FIREBASE_SERVER_KEY",
    "ProjectId": "ozelders-app", 
    "Enabled": true
  }
}
```

## 4. MAUI Uygulaması FCM Entegrasyonu

### Android Konfigürasyonu

`src/OzelDers.App/Platforms/Android/AndroidManifest.xml` dosyasına ekleyin:

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="com.google.android.c2dm.permission.RECEIVE" />
<uses-permission android:name="android.permission.WAKE_LOCK" />

<application>
  <service android:name="com.google.firebase.messaging.FirebaseMessagingService"
           android:exported="false">
    <intent-filter>
      <action android:name="com.google.firebase.MESSAGING_EVENT" />
    </intent-filter>
  </service>
</application>
```

### NuGet Paketleri Ekleme

```bash
# MAUI projesine Firebase paketlerini ekle
dotnet add src/OzelDers.App package Plugin.Firebase
dotnet add src/OzelDers.App package Plugin.Firebase.CloudMessaging
```

### MauiProgram.cs Güncelleme

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Firebase ekleme
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android.OnCreate((activity, bundle) =>
            {
                CrossFirebase.Initialize(activity, bundle);
            }));
#endif
        });

        return builder.Build();
    }
}
```

## 5. Test Etme

### Admin Panel'den FCM Test

1. Uygulamayı çalıştırın
2. Admin paneline giriş yapın: `/admin/dashboard`
3. "FCM Test" bölümünde:
   - Token: Kullanıcının FCM token'ı
   - Title: "Test Bildirimi"
   - Body: "Bu bir test mesajıdır"
4. "Gönder" butonuna tıklayın

### API Endpoint ile Test

```bash
curl -X POST "https://localhost:7001/api/admin/test-fcm" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "token": "USER_FCM_TOKEN",
    "title": "Test Notification",
    "body": "This is a test message"
  }'
```

## 6. Kullanıcı FCM Token Kaydetme

### Web Uygulamasında

JavaScript ile FCM token alma:

```javascript
// firebase-config.js
import { initializeApp } from 'firebase/app';
import { getMessaging, getToken } from 'firebase/messaging';

const firebaseConfig = {
  // Firebase configuration object
};

const app = initializeApp(firebaseConfig);
const messaging = getMessaging(app);

// Token alma
getToken(messaging, { vapidKey: 'YOUR_VAPID_KEY' }).then((currentToken) => {
  if (currentToken) {
    // Token'ı backend'e gönder
    fetch('/api/users/fcm-token', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token: currentToken })
    });
  }
});
```

### MAUI Uygulamasında

```csharp
// FCM token alma ve kaydetme
var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
if (!string.IsNullOrEmpty(token))
{
    // Token'ı API'ye gönder
    await httpClient.PostAsJsonAsync("api/users/fcm-token", new { token });
}
```

## 7. Sorun Giderme

### Token Alınamıyor
- Firebase konfigürasyonunu kontrol edin
- Internet bağlantısını kontrol edin
- Google Play Services güncel olduğundan emin olun

### Bildirim Gelmiyor
- FCM token'ın doğru kaydedildiğini kontrol edin
- Firebase Console'da message delivery raporlarını kontrol edin
- Uygulama izinlerini kontrol edin

### Server Key Hatası
- Firebase Console'dan doğru server key'i aldığınızdan emin olun
- Key'in başında/sonunda boşluk olmadığından emin olun

## 8. Production Deployment

### Environment Variables

```bash
# Production ortamında environment variables kullanın
FIREBASE_SERVER_KEY=your_production_server_key
FIREBASE_PROJECT_ID=ozelders-app
```

### Docker Konfigürasyonu

```dockerfile
# Dockerfile'da environment variables
ENV Firebase__ServerKey=${FIREBASE_SERVER_KEY}
ENV Firebase__ProjectId=${FIREBASE_PROJECT_ID}
ENV Firebase__Enabled=true
```