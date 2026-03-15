using System.Reactive.Linq;
using System.Reactive.Subjects;
using FoodStreetGuide.Models;
using Microsoft.Maui.Devices.Sensors;

namespace FoodStreetGuide.Services;

/// <summary>
/// GPS service dùng MAUI Essentials Geolocation.
/// Tự động chuyển Foreground ↔ Background khi app lifecycle thay đổi.
/// </summary>
public sealed class GpsService : IGpsService
{
    // ── Reactive stream ──────────────────────────────────────────────
    private readonly Subject<GeoLocation> _locationSubject = new();
    private CancellationTokenSource?      _cts;
    private Task?                         _trackingTask;

    // ── State ────────────────────────────────────────────────────────
    public GeoLocation?       CurrentLocation { get; private set; }
    public GpsTrackingConfig  Config          { get; private set; } = GpsTrackingConfig.Foreground;
    public bool               IsTracking      { get; private set; }

    public IObservable<GeoLocation> LocationUpdates =>
        _locationSubject.AsObservable();

    // ── Permission ───────────────────────────────────────────────────
    public async Task<bool> RequestPermissionsAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted) return false;

        // Yêu cầu thêm background (Android 10+)
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            status = await Permissions.RequestAsync<Permissions.LocationAlways>();
        }

        return status == PermissionStatus.Granted;
    }

    // ── Start / Stop ─────────────────────────────────────────────────
    public async Task StartAsync(GpsTrackingConfig config, CancellationToken ct = default)
    {
        if (IsTracking) await StopAsync();

        Config = config;
        _cts   = CancellationTokenSource.CreateLinkedTokenSource(ct);
        IsTracking = true;

        // Lấy vị trí đầu tiên ngay lập tức
        await FetchOnceAsync();

        // Bắt đầu polling loop
        _trackingTask = RunTrackingLoopAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        IsTracking = false;
        _cts?.Cancel();
        if (_trackingTask is not null)
        {
            try { await _trackingTask; }
            catch (OperationCanceledException) { }
        }
        _cts?.Dispose();
        _cts = null;
    }

    public async Task SwitchConfigAsync(GpsTrackingConfig newConfig)
    {
        if (!IsTracking) return;
        await StopAsync();
        await StartAsync(newConfig);
    }

    // ── Internal tracking loop ───────────────────────────────────────
    private async Task RunTrackingLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(Config.Interval, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested) break;
            await FetchOnceAsync(ct);
        }
    }

    private async Task FetchOnceAsync(CancellationToken ct = default)
    {
        try
        {
            var request = new GeolocationRequest(
                accuracy: MapAccuracy(Config.DesiredAccuracy),
                timeout: TimeSpan.FromSeconds(8));

            var loc = await Geolocation.GetLocationAsync(request, ct);
            if (loc is null) return;

            var geo = new GeoLocation(loc.Latitude, loc.Longitude, loc.Accuracy ?? 0);

            // Lọc nhiễu: bỏ qua nếu di chuyển < MinDistance
            if (CurrentLocation is not null &&
                CurrentLocation.DistanceTo(geo) < Config.MinDistance) return;

            CurrentLocation = geo;
            _locationSubject.OnNext(geo);
        }
        catch (FeatureNotEnabledException)
        {
            // GPS bị tắt trên thiết bị – không crash
        }
        catch (PermissionException)
        {
            // Permission bị thu hồi runtime
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GPS] Error: {ex.Message}");
        }
    }

    private static GeolocationAccuracy MapAccuracy(double meters) =>
        meters <= 5   ? GeolocationAccuracy.Best    :
        meters <= 10  ? GeolocationAccuracy.High    :
        meters <= 30  ? GeolocationAccuracy.Medium  :
        meters <= 100 ? GeolocationAccuracy.Low     :
                        GeolocationAccuracy.Lowest;

    // ── IAsyncDisposable ─────────────────────────────────────────────
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _locationSubject.OnCompleted();
        _locationSubject.Dispose();
    }
}
