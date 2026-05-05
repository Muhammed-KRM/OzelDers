# Android Emulator Kurulum Rehberi

## Gereksinimler

### 1. Java Development Kit (JDK) Kurulumu

Android geliştirme için Java gereklidir. Microsoft OpenJDK önerilir:

1. [Microsoft OpenJDK 17](https://learn.microsoft.com/en-us/java/openjdk/download) indirin
2. Kurulumu tamamlayın
3. JAVA_HOME environment variable'ını ayarlayın:
   ```
   JAVA_HOME=C:\Program Files\Microsoft\jdk-17.0.x-hotspot
   ```
4. PATH'e Java bin klasörünü ekleyin:
   ```
   PATH=%JAVA_HOME%\bin;%PATH%
   ```

### 2. Android SDK Kontrolü

Android SDK zaten kurulu: `C:\Program Files (x86)\Android\android-sdk`

ANDROID_HOME environment variable'ını ayarlayın:
```
ANDROID_HOME=C:\Program Files (x86)\Android\android-sdk
```

PATH'e Android tools'ları ekleyin:
```
PATH=%ANDROID_HOME%\platform-tools;%ANDROID_HOME%\cmdline-tools\latest\bin;%PATH%
```

## Android Virtual Device (AVD) Oluşturma

### 1. Sistem Görüntüsü İndirme

```bash
# Mevcut sistem görüntülerini listele
sdkmanager --list | findstr "system-images"

# Android 13 (API 33) sistem görüntüsünü indir
sdkmanager "system-images;android-33;google_apis;x86_64"
```

### 2. AVD Oluşturma

```bash
# AVD oluştur
avdmanager create avd -n "Pixel_7_API_33" -k "system-images;android-33;google_apis;x86_64" -d "pixel_7"

# Mevcut AVD'leri listele
avdmanager list avd
```

### 3. Emulator Başlatma

```bash
# Emulator'ü başlat
emulator -avd Pixel_7_API_33
```

## MAUI Android Uygulamasını Çalıştırma

### Sorun: ASP.NET Core Bağımlılığı

Mevcut MAUI uygulaması SharedUI projesini kullanıyor, ancak SharedUI ASP.NET Core bileşenlerini içeriyor. Bu mobil platformlarda çalışmaz.

### Çözüm Seçenekleri:

#### Seçenek 1: Mobil-Özel UI Projesi (Önerilen)
```bash
# Yeni mobil UI projesi oluştur
dotnet new classlib -n OzelDers.MobileUI -f net10.0

# Blazor Hybrid bileşenlerini ekle
dotnet add src/OzelDers.MobileUI package Microsoft.AspNetCore.Components.WebView.Maui
```

#### Seçenek 2: Koşullu Derleme
SharedUI projesinde platform-özel koşullar ekle:

```xml
<ItemGroup Condition="!$(TargetFramework.Contains('android')) AND !$(TargetFramework.Contains('ios'))">
  <PackageReference Include="Microsoft.AspNetCore.Components.Web" />
</ItemGroup>
```

## Test Etme

1. Emulator'ü başlatın
2. MAUI uygulamasını derleyin ve çalıştırın:
   ```bash
   dotnet build src/OzelDers.App -f net10.0-android
   dotnet run --project src/OzelDers.App -f net10.0-android
   ```

## Sorun Giderme

### Java Bulunamıyor Hatası
- JAVA_HOME doğru ayarlandığından emin olun
- `java -version` komutunu test edin

### AVD Oluşturulamıyor
- Intel HAXM veya Hyper-V etkin olduğundan emin olun
- Sistem görüntüsünün doğru indirildiğini kontrol edin

### Emulator Yavaş
- Hardware acceleration etkinleştirin
- RAM miktarını artırın (AVD ayarlarından)