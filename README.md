# Phố Ẩm Thực – Food Street Auto Guide
## Ứng dụng thuyết minh tự động đa ngôn ngữ · .NET 10 MAUI · OOP

---

## Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                       │
│  Views/MainPage.xaml      Controls/AudioPlayerControl.xaml      │
│  Controls/PoiCardControl  Controls/SystemStatusControl          │
├─────────────────────────────────────────────────────────────────┤
│                        VIEWMODEL LAYER  (MVVM)                  │
│  MainViewModel            AudioPlayerViewModel    PoiViewModel  │
│  ← CommunityToolkit.Mvvm source-gen (INotifyPropertyChanged)   │
├─────────────────────────────────────────────────────────────────┤
│                        SERVICE LAYER  (DI Singleton)            │
│  TourOrchestrator  ←── GPS ──► GeofenceService ──► AudioService│
│  IGpsService            IGeofenceService        IAudioService   │
├─────────────────────────────────────────────────────────────────┤
│                        DATA LAYER                               │
│  IPoiRepository / SamplePoiRepository  (JSON-ready)            │
├─────────────────────────────────────────────────────────────────┤
│                        MODELS (Domain)                          │
│  GeoLocation  PointOfInterest  GeofenceZone  AudioTrack         │
│  LocalizedContentMap  GpsTrackingConfig  AppLanguage            │
└─────────────────────────────────────────────────────────────────┘
```

---

## Luồng hoạt động chính

```
User bước đi
    │
    ▼
GpsService.LocationUpdates  (Observable stream, polling 2s FG / 10s BG)
    │  lọc nhiễu: bỏ qua nếu di chuyển < MinDistance
    ▼
TourOrchestrator.OnLocationReceived()
    │
    ├─► GeofenceService.UpdateUserLocation(location)
    │       │  duyệt tất cả POI, tính Haversine distance
    │       │  đánh giá: Outside / Nearby / Inside
    │       │  nếu state thay đổi → fire GeofenceStateChanged
    │       └─ nếu Inside + debounce OK → fire PoiEntered
    │
    └─► PoiEntered event
            │
            ├── Debounce check (cooldown per POI, default 120s)
            ├── Lấy LocalizedContent theo ngôn ngữ hiện tại
            ├── Nếu có file Studio MP3 → AudioService.PlayStudioFileAsync()
            └── Fallback / TTS mode    → AudioService.PlayTtsAsync()
```

---

## Cấu trúc thư mục

```
FoodStreetGuide/
├── Models/
│   ├── GeoLocation.cs          # Tọa độ + Haversine calculator
│   ├── LocalizedContent.cs     # Nội dung đa ngôn ngữ + map
│   ├── PointOfInterest.cs      # POI + GeofenceZone + state
│   └── AudioTrack.cs           # Track model + GPS configs
│
├── Services/
│   ├── IGpsService.cs          # Interface GPS abstraction
│   ├── GpsService.cs           # MAUI Essentials implementation
│   ├── GeofenceService.cs      # Haversine engine + event dispatch
│   ├── IAudioService.cs        # Interface audio abstraction
│   ├── AudioService.cs         # Plugin.Maui.Audio + TTS
│   └── TourOrchestrator.cs     # Điều phối GPS → Geofence → Audio
│
├── ViewModels/
│   ├── BaseViewModel.cs             # ObservableObject + RunSafeAsync
│   ├── MainViewModel.cs             # Top-level VM
│   ├── MainViewModel.SetTab.cs      # Partial: tab/POI navigation
│   ├── AudioPlayerViewModel.cs      # Player state + commands
│   ├── AudioPlayerViewModel.Navigation.cs  # Skip events
│   └── PoiViewModel.cs              # UI wrapper per POI
│
├── Views/
│   ├── MainPage.xaml / .cs
│
├── Controls/
│   ├── AudioPlayerControl.xaml / .cs   # Waveform animation
│   ├── PoiCardControl.xaml / .cs       # POI list item
│   └── SystemStatusControl.xaml / .cs  # GPS/debug panel
│
├── Data/
│   └── PoiRepository.cs    # 6 POI mẫu phố ẩm thực TP.HCM
│
├── Helpers/
│   ├── Converters.cs            # IValueConverter cho XAML
│   └── AppLifecycleHandler.cs   # Foreground/Background switch
│
├── Platforms/
│   ├── Android/
│   │   ├── AndroidManifest.xml          # Location permissions
│   │   └── GpsBackgroundService.cs      # Foreground Service
│   └── iOS/
│       └── Info.plist                   # NSLocation descriptions
│
└── MauiProgram.cs      # DI container, lifecycle wiring
```

---

## Các OOP patterns được áp dụng

| Pattern | Nơi áp dụng |
|---------|------------|
| **MVVM** | ViewModels ↔ Views via data binding |
| **Dependency Injection** | MauiProgram.cs, constructor injection |
| **Repository Pattern** | IPoiRepository / SamplePoiRepository |
| **Observer / Reactive** | `IObservable<GeoLocation>` GPS stream |
| **Strategy** | GpsTrackingConfig (Foreground / Background / PowerSave) |
| **Facade** | TourOrchestrator ẩn GPS+Geofence+Audio |
| **Decorator** | LocalizedContentMap.Add() fluent builder |
| **Factory Method** | GpsTrackingConfig.Foreground / .Background static factories |
| **Partial Class** | MainViewModel tách file theo concern |

---

## Chạy dự án

```bash
# Android
dotnet build -f net10.0-android
dotnet run  -f net10.0-android

# iOS (cần Mac + Xcode)
dotnet build -f net10.0-ios
dotnet run  -f net10.0-ios
```

### Yêu cầu
- .NET 10 SDK
- MAUI workload: `dotnet workload install maui --sdk-version 10.0.100`
- Android SDK 26+, iOS 16+

### Thêm audio files
Đặt file MP3 vào `Resources/Raw/audio/`:
```
audio/bun_bo_hue_vi.mp3
audio/bun_bo_hue_en.mp3
audio/bun_bo_hue_zh.mp3
audio/bun_bo_hue_ja.mp3
... (tương tự cho các POI khác)
```
Nếu file không tồn tại, app tự fallback sang TTS.

---

## Migration .NET 9 → .NET 10

### Thay đổi bắt buộc

| File | Thay đổi |
|------|----------|
| `FoodStreetGuide.csproj` | `net9.0-*` → `net10.0-*` |
| `FoodStreetGuide.csproj` | `iOSMinOSVersion` 15.0 → **16.0** (iOS 15 dropped in MAUI 10) |
| `FoodStreetGuide.csproj` | `LangVersion preview` thêm mới (C# 14 preview) |
| `MauiProgram.cs` | `UseMauiCommunityToolkit()` → truyền `options` lambda (CT.Maui 11 yêu cầu) |
| `MauiProgram.cs` | Lifecycle lambda gọn hơn — bỏ `.Let()` chain, dùng `GetOrchestrator()` helper |

### Package versions

| Package | .NET 9 | .NET 10 |
|---------|--------|---------|
| `Microsoft.Maui.Controls` | 9.0.0 | **10.0.0** |
| `CommunityToolkit.Maui` | 9.1.0 | **11.0.0** |
| `CommunityToolkit.Mvvm` | 8.3.2 | **8.4.0** |
| `Microsoft.Extensions.DependencyInjection` | 9.0.0 | **10.0.0** |
| `Plugin.Maui.Audio` | 3.0.0 | **3.1.0** |
| `Polly` | 8.4.1 | **8.5.0** |
| `Newtonsoft.Json` | 13.0.3 | 13.0.3 (không đổi) |

### Không cần thay đổi
- `App.xaml` / `App.xaml.cs` — API hoàn toàn tương thích
- Toàn bộ Services, ViewModels, Views, Models — không có breaking change
- `Plugin.Maui.Audio` 3.x API (`AudioManager.Current`) giữ nguyên
- Polly 8.x resilience pipeline API không thay đổi

### Cài workload .NET 10
```bash
dotnet workload install maui --sdk-version 10.0.100
dotnet workload update
```

### Build & run
```bash
# Android
dotnet build -f net10.0-android
dotnet run   -f net10.0-android

# iOS (cần Mac + Xcode 16+)
dotnet build -f net10.0-ios
dotnet run   -f net10.0-ios
```