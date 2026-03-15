# 🍜 Food Street Guide — .NET MAUI

App thuyết minh đa ngôn ngữ chạy trên **Android, iOS và Windows**.

## Cài đặt trước khi build

### 1. Cài .NET MAUI workload
```bash
dotnet workload install maui
```

### 2. Android (nếu build APK)
- Cài Android Studio: https://developer.android.com/studio
- Hoặc cài Android SDK riêng, set `ANDROID_HOME`

### 3. Build
```
Double-click BUILD.bat
→ Chọn [1] Android APK / [2] Windows / [3] Cả hai
```

---

## Cấu trúc

```
FoodStreetMAUI/
├── Models/         — GpsCoordinate, PointOfInterest, LocalizedContent
├── Services/
│   ├── GpsService.cs       — GPS thực (Geolocation API) + Mô phỏng
│   ├── GeofenceService.cs  — Haversine + debounce anti-spam
│   ├── AudioService.cs     — TTS (TextToSpeech MAUI) + file âm thanh
│   └── DataService.cs      — Load/Save JSON + 5 POI mẫu Bùi Viện
├── ViewModels/
│   └── MainViewModel.cs    — MVVM với CommunityToolkit
├── Views/
│   ├── MainPage.xaml       — UI: Language bar, Map, POI list, Log
│   └── MainPage.xaml.cs    — Map vẽ bằng IDrawable (MAUI Graphics)
├── Platforms/
│   └── Android/AndroidManifest.xml  — Quyền GPS, Audio
└── BUILD.bat
```

---

## Tính năng

- **GPS thực** trên Android/iOS (`Geolocation.StartListeningForegroundAsync`)
- **Mô phỏng** di chuyển dọc phố Bùi Viện để test
- **Geofence** 2 vòng: Approach (cảnh báo) + Enter (phát thuyết minh)
- **Anti-spam**: debounce per-POI + max daily triggers
- **TTS đa ngôn ngữ** 6 ngôn ngữ: VI, EN, ZH, JA, KO, FR
- **Phát file âm thanh** MP3/WAV (AudioManager MAUI)
- **Bản đồ GPS** vẽ real-time bằng IDrawable
- **Dark theme** toàn bộ

---

## Lưu ý

- GPS thực cần cấp quyền `ACCESS_FINE_LOCATION` khi app khởi động
- TTS phụ thuộc vào giọng đọc cài trên thiết bị
- Test nhanh: bật **Mô phỏng** → app tự di chuyển và trigger POI
