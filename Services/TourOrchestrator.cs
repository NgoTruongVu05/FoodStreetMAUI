using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

/// <summary>
/// Orchestrator: dây nối GPS → Geofence → Audio.
/// Điều phối toàn bộ luồng thuyết minh tự động.
/// </summary>
public interface ITourOrchestrator : IAsyncDisposable
{
    bool           IsRunning       { get; }
    AppLanguage    CurrentLanguage { get; set; }
    bool           TtsEnabled      { get; set; }
    GeoLocation?   UserLocation    { get; }

    event EventHandler<PointOfInterest>? PoiTriggered;
    event EventHandler<GeoLocation>?     LocationUpdated;

    Task StartTourAsync(CancellationToken ct = default);
    Task StopTourAsync();
    Task SetLanguageAsync(AppLanguage language);
}

/// <summary>
/// Triển khai TourOrchestrator – đăng ký events, quản lý lifecycle.
/// </summary>
public sealed class TourOrchestrator : ITourOrchestrator
{
    private readonly IGpsService       _gps;
    private readonly IGeofenceService  _geofence;
    private readonly IAudioService     _audio;
    private IDisposable?               _locationSub;
    private CancellationTokenSource?   _cts;

    public bool         IsRunning       { get; private set; }
    public AppLanguage  CurrentLanguage { get; set; } = AppLanguage.Vietnamese;
    public bool         TtsEnabled      { get; set; } = false;
    public GeoLocation? UserLocation    { get; private set; }

    public event EventHandler<PointOfInterest>? PoiTriggered;
    public event EventHandler<GeoLocation>?     LocationUpdated;

    public TourOrchestrator(
        IGpsService      gps,
        IGeofenceService geofence,
        IAudioService    audio)
    {
        _gps      = gps;
        _geofence = geofence;
        _audio    = audio;
    }

    // ── Start / Stop ─────────────────────────────────────────────────────────
    public async Task StartTourAsync(CancellationToken ct = default)
    {
        if (IsRunning) return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // 1. Xin quyền GPS
        bool granted = await _gps.RequestPermissionsAsync();
        if (!granted) throw new UnauthorizedAccessException("GPS permission denied");

        // 2. Đăng ký sự kiện geofence
        _geofence.PoiEntered += OnPoiEntered;

        // 3. Subscribe GPS stream → cập nhật geofence
        _locationSub = _gps.LocationUpdates.Subscribe(OnLocationReceived);

        // 4. Bắt đầu GPS tracking
        await _gps.StartAsync(GpsTrackingConfig.Foreground, _cts.Token);

        IsRunning = true;
    }

    public async Task StopTourAsync()
    {
        if (!IsRunning) return;
        IsRunning = false;

        _geofence.PoiEntered -= OnPoiEntered;
        _locationSub?.Dispose();
        _locationSub = null;

        await _gps.StopAsync();
        await _audio.StopAsync();
        _cts?.Cancel();
        _cts?.Dispose();
    }

    // ── App Lifecycle ─────────────────────────────────────────────────────────
    public async Task OnAppBackgrounded()
    {
        if (_gps.IsTracking)
            await _gps.SwitchConfigAsync(GpsTrackingConfig.Background);
    }

    public async Task OnAppForegrounded()
    {
        if (_gps.IsTracking)
            await _gps.SwitchConfigAsync(GpsTrackingConfig.Foreground);
    }

    // ── Language ──────────────────────────────────────────────────────────────
    public Task SetLanguageAsync(AppLanguage language)
    {
        CurrentLanguage = language;
        return Task.CompletedTask;
    }

    // ── Event Handlers ────────────────────────────────────────────────────────
    private void OnLocationReceived(GeoLocation location)
    {
        UserLocation = location;
        _geofence.UpdateUserLocation(location);
        MainThread.BeginInvokeOnMainThread(() =>
            LocationUpdated?.Invoke(this, location));
    }

    private async void OnPoiEntered(object? sender, GeofenceTriggeredEventArgs e)
    {
        var poi = e.Poi;
        PoiTriggered?.Invoke(this, poi);

        var localized = poi.Content.GetOrDefault(CurrentLanguage);

        if (TtsEnabled || !localized.HasAudioFile)
        {
            // Dùng TTS
            var locale = CurrentLanguage switch
            {
                AppLanguage.English  => "en-US",
                AppLanguage.Chinese  => "zh-CN",
                AppLanguage.Japanese => "ja-JP",
                _                    => "vi-VN"
            };
            var track = new AudioTrack
            {
                Title      = localized.Title,
                PoiId      = poi.Id.ToString(),
                Source     = AudioSource.TextToSpeech,
                SpeechText = localized.Description,
                Locale     = locale
            };
            await _audio.PlayTtsAsync(track);
        }
        else
        {
            // Dùng Studio file
            var track = new AudioTrack
            {
                Title    = localized.Title,
                PoiId    = poi.Id.ToString(),
                Source   = AudioSource.StudioFile,
                FilePath = localized.AudioFile
            };
            await _audio.PlayStudioFileAsync(track);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopTourAsync();
        _locationSub?.Dispose();
    }
}
