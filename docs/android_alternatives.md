# Android Test Alternatifleri - Disk Alanı Sınırlı

Disk alanı sınırlı olduğu için Android Studio ve emulator kurulumu yerine alternatif çözümler.

## 🚀 Hızlı Çözümler (Disk Alanı Gerektirmez)

### 1. Online Android Emulator'lar

#### A. Appetize.io (Önerilen)
- **URL:** [https://appetize.io/](https://appetize.io/)
- **Ücretsiz:** 100 dakika/ay
- **Kullanım:**
  1. APK dosyasını upload et
  2. Tarayıcıda Android emulator çalışır
  3. Gerçek cihaz gibi test edilebilir

#### B. BrowserStack App Live
- **URL:** [https://www.browserstack.com/app-live](https://www.browserstack.com/app-live/)
- **Ücretsiz:** 30 dakika trial
- **Avantaj:** Gerçek cihazlarda test

#### C. LambdaTest Real Device Cloud
- **URL:** [https://www.lambdatest.com/mobile-app-testing](https://www.lambdatest.com/mobile-app-testing)
- **Ücretsiz:** 60 dakika/ay

### 2. Gerçek Android Cihaz Kullanımı

#### USB Debugging ile Test
```bash
# Android cihazı USB ile bağla
# Geliştirici seçeneklerini aç
# USB debugging'i etkinleştir

# Cihazı kontrol et
adb devices

# MAUI uygulamasını direkt cihaza yükle
dotnet build src/OzelDers.App -f net10.0-android
dotnet run --project src/OzelDers.App -f net10.0-android
```

## 🔧 Minimal Android SDK Kurulumu (Sadece Build İçin)

Eğer sadece APK build etmek istiyorsan, minimal SDK kurulumu:

### 1. Saddle Command Line Tools
```powershell
# Minimal Android SDK (sadece 200MB)
$sdkUrl = "https://dl.google.com/android/repository/commandlinetools-win-11076708_latest.zip"
$sdkPath = "C:\android-sdk-minimal"

# İndir ve çıkart
Invoke-WebRequest -Uri $sdkUrl -OutFile "$env:TEMP\android-tools.zip"
Expand-Archive "$env:TEMP\android-tools.zip" -DestinationPath $sdkPath

# Environment variables
$env:ANDROID_HOME = $sdkPath
$env:PATH = "$env:PATH;$sdkPath\cmdline-tools\bin"
```

### 2. Sadece Build Dependencies
```bash
# Minimal paketler (toplam ~500MB)
sdkmanager "platform-tools" "platforms;android-33" "build-tools;33.0.0"
```

## 🐳 Docker ile Android Build

Disk alanını korumak için Docker container'da build:

### Dockerfile.android-build
```dockerfile
FROM openjdk:17-jdk-slim

# Android SDK minimal kurulum
ENV ANDROID_HOME=/opt/android-sdk
RUN mkdir -p ${ANDROID_HOME}/cmdline-tools && \
    cd ${ANDROID_HOME}/cmdline-tools && \
    wget https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip && \
    unzip commandlinetools-linux-*_latest.zip && \
    mv cmdline-tools latest

# Sadece build için gerekli paketler
RUN yes | ${ANDROID_HOME}/cmdline-tools/latest/bin/sdkmanager --licenses && \
    ${ANDROID_HOME}/cmdline-tools/latest/bin/sdkmanager "platform-tools" "platforms;android-33" "build-tools;33.0.0"

# .NET SDK
RUN wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-8.0

WORKDIR /app
COPY . .

# APK build
RUN dotnet workload install maui-android && \
    dotnet build src/OzelDers.App -f net10.0-android -c Release

CMD ["cp", "/app/src/OzelDers.App/bin/Release/net10.0-android/com.companyname.ozelders.app-Signed.apk", "/output/"]
```

### Build Script
```bash
# Docker ile APK build (container silinir, disk alanı korunur)
docker build -f Dockerfile.android-build -t android-builder .
docker run --rm -v ${PWD}/output:/output android-builder
```

## 📱 MAUI Android Sorunları ve Çözümleri

### Sorun 1: SharedUI ASP.NET Core Bağımlılığı
**Hata:** `Microsoft.AspNetCore.App için android-x64 runtime paketi yok`

**Çözüm A: Koşullu Derleme**
`src/OzelDers.SharedUI/OzelDers.SharedUI.csproj` dosyasında:

```xml
<ItemGroup Condition="!$(TargetFramework.Contains('android')) AND !$(TargetFramework.Contains('ios'))">
  <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="8.0.0" />
  <PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="8.0.0" />
</ItemGroup>

<ItemGroup Condition="$(TargetFramework.Contains('android')) OR $(TargetFramework.Contains('ios'))">
  <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="8.0.0" />
</ItemGroup>
```

**Çözüm B: Mobil-Özel UI Projesi**
```bash
# Yeni mobil UI projesi
dotnet new classlib -n OzelDers.MobileUI -f net8.0
dotnet add src/OzelDers.MobileUI package Microsoft.AspNetCore.Components.WebView.Maui

# MAUI projesinde referans değiştir
# SharedUI yerine MobileUI kullan
```

### Sorun 2: Java/Android SDK Bulunamıyor
**Çözüm:** Visual Studio'nun kendi Android SDK'sını kullan:

```xml
<!-- OzelDers.App.csproj -->
<PropertyGroup Condition="$(TargetFramework.Contains('android'))">
  <AndroidSdkDirectory>C:\Program Files (x86)\Android\android-sdk</AndroidSdkDirectory>
  <JavaSdkDirectory>C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Microsoft\Microsoft.NET.Sdk.Android\tools\jdk</JavaSdkDirectory>
</PropertyGroup>
```

## 🎯 Önerilen Yaklaşım

**Disk alanı sınırlı olduğu için:**

1. **Önce:** SharedUI projesini mobil uyumlu hale getir (koşullu derleme)
2. **Sonra:** Docker ile APK build et
3. **Test:** Appetize.io ile online test et
4. **Gerçek test:** USB ile Android cihazda test et

Bu yaklaşım disk alanı kullanmadan Android uygulamasını test etmeyi sağlar.

## 📋 Hızlı Başlangıç Checklist

- [ ] SharedUI projesinde koşullu derleme ekle
- [ ] Docker ile Android build test et
- [ ] APK dosyasını Appetize.io'ya upload et
- [ ] Online emulator'da test et
- [ ] Gerçek Android cihazda USB ile test et

Bu adımları takip ederek disk alanı sınırlaması olmadan Android uygulamasını test edebilirsin!