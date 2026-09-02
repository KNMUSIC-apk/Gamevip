# Build Guide — Tạo APK thật từ project Aria

Mình không thể build APK trong sandbox cloud (không có Unity Editor + Android SDK), nhưng toolkit này sẽ giúp bạn build trên máy local hoặc CI cực nhanh.

## Yêu cầu

- **Unity Hub** + **Unity 2022.3.20f1** (LTS)
- **Android Build Support** module (cài qua Hub)
- **JDK 11+** (Android Studio kèm theo)
- **Android SDK 33+** + **NDK r25+**
- Tổng cài ~10 GB

## Phương án 1 — Local build (1 lệnh)

```bash
# macOS / Linux
cd ProjectAria
export UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity"
./BuildTools/build-android.sh

# Windows
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe"
BuildTools\build-android.bat
```

APK sẽ ra ở `Builds/Android/ProjectAria.apk` (~50-150 MB tuỳ assets).

## Phương án 2 — GitHub Actions (free, không cần máy mạnh)

1. Push project lên GitHub
2. Vào **Settings → Secrets and variables → Actions** thêm:
   - `UNITY_EMAIL` — email Unity ID
   - `UNITY_PASSWORD` — password Unity ID
   - `UNITY_LICENSE` — nội dung file `.ulf` license (lấy từ `%APPDATA%\Unity\Unity_lic.ulf` trên Windows hoặc `~/Library/Application Support/Unity/Unity_lic.ulf` trên Mac)
3. Push code → workflow chạy tự động
4. Download APK từ **Actions → Run → Artifacts → ProjectAria-<sha>**

Lần build đầu ~25 phút (download Unity image + import packages). Lần sau ~5-8 phút (cache Library).

## Phương án 3 — Unity Editor thủ công

1. Mở project trong Unity
2. **File → Build Settings → Android → Switch Platform**
3. **Player Settings** → set Company Name, Package Name, etc.
4. **Build** → chọn folder output → Unity tạo APK

## 📋 Build Configuration Reference

Mọi config build đã được hardcode trong `Assets/Editor/BuildScript.cs`:

| Setting | Value |
|---|---|
| Scripting Backend | IL2CPP |
| Target Architecture | ARM64 |
| Min SDK | 26 (Android 8.0) |
| Target SDK | Auto (latest) |
| Graphics API | Vulkan |
| Stripping | High |
| Strip Engine Code | Yes |
| Bundle ID | com.studio.projectaria |
| Version | 0.1.0 (build 1) |

Override qua env vars:
```bash
ARIA_OUTPUT_PATH=Builds/Custom/My.apk \
ARIA_KEYSTORE_PASS=mypassword \
ARIA_KEY_ALIAS=mykey \
./build-android.sh
```

## 🔑 Keystore

Lần build đầu script tự tạo dev keystore (`user.keystore` ở root project). **KHÔNG commit keystore này** — thêm vào `.gitignore`:

```
# .gitignore
user.keystore
*.keystore
keystore.properties
Builds/
Library/
Temp/
```

Production: dùng keystore thật, set env vars:
```bash
ARIA_KEYSTORE_NAME=production.keystore
ARIA_KEYSTORE_PASS=$ECRET
ARIA_KEY_ALIAS=projectaria-prod
ARIA_KEY_PASS=$ECRET
```

## 🐛 Troubleshooting

### "Unable to find Android SDK"
- Mở Unity → **Edit → Preferences → External Tools → Android SDK**: trỏ tới Android Studio SDK
- Hoặc cài Android Studio + tick "Android SDK" trong Hub

### "Build failed: IL2CPP error"
- Cài **Android NDK** qua Hub: **Add Modules → Android Build Support → NDK**

### "Failed to sign APK"
- Chạy `BuildScript.GenerateKeystore` thủ công từ Unity menu hoặc CLI

### "Gradle build failed"
- Xóa `Library/` + `Temp/`, build lại
- Hoặc bump Gradle trong `gradle/wrapper/gradle-wrapper.properties`

### APK quá lớn (>200 MB)
- Bật **Asset Bundles** hoặc **Addressables** (đã setup sẵn)
- Compress textures → ASTC 6x6
- Strip Mono assemblies: **Player Settings → Managed Stripping = High**

## 📱 Test APK trên thiết bị

```bash
# Cài qua adb
adb install -r Builds/Android/ProjectAria.apk

# Xem log
adb logcat -s Unity ActivityManager

# Profile performance
adb shell am start -n com.studio.projectaria/com.unity3d.player.UnityPlayerActivity
```

## 📦 Phân phối

- **Google Play**: build App Bundle (AAB) thay vì APK — sửa `BuildPlayerOptions.target` → `BuildTarget.Android` + `EditorUserBuildSettings.buildAppBundle = true`
- **TapTap / Itch.io**: APK đủ
- **Sideload test**: APK + cho phép "Install from unknown sources"

## ⏱️ Thời gian build ước tính

| Asset count | Cold build | Warm build |
|---|---|---|
| Trống (chỉ code) | 5-8 phút | 1-2 phút |
| + 100 models | 10-15 phút | 3-5 phút |
| + 1000 assets | 20-30 phút | 5-10 phút |

Project Aria hiện tại chỉ có code → build APK đầu tiên ~6 phút sau khi Unity import packages.
