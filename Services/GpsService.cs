using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoodStreetMAUI.Models;
using Microsoft.Maui.Devices.Sensors;

namespace FoodStreetMAUI.Services
{
    public class GpsUpdateEventArgs : EventArgs
    {
        public GpsCoordinate Location { get; }
        public GpsUpdateEventArgs(GpsCoordinate loc) => Location = loc;
    }

    public partial class GpsService : IDisposable
    {
        public event EventHandler<GpsUpdateEventArgs>? LocationUpdated;
        public event EventHandler<string>? StatusChanged;

        public GpsCoordinate? CurrentLocation { get; private set; }
        public bool IsTracking { get; private set; }
        public double TotalDistance { get; private set; }

        public int UpdateIntervalMs { get; set; } = 2000;

        private CancellationTokenSource? _cts;
        private GpsCoordinate? _lastLocation;

        // (simulation removed in production)

        // ── Start / Stop ──────────────────────────────────────────────────────
        public async Task StartTrackingAsync()
        {
            if (IsTracking) return;
            IsTracking = true;
            _cts = new CancellationTokenSource();
            await StartRealGpsAsync(_cts.Token);
        }

        public void StopTracking()
        {
            _cts?.Cancel();
            IsTracking = false;

            try { Geolocation.StopListeningForeground(); } catch { }

            StatusChanged?.Invoke(this, "⏹ GPS đã dừng");
        }

        // ── Real GPS ──────────────────────────────────────────────────────────
        private async Task StartRealGpsAsync(CancellationToken ct)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    StatusChanged?.Invoke(this, "❌ Chưa cấp quyền GPS");
                    IsTracking = false;
                    return;
                }
                //Flow: Di chuyển (thay đổi vị trí)
                Geolocation.LocationChanged -= OnLocationChanged;
                Geolocation.LocationChanged += OnLocationChanged;
                //Mỗi khi có vị trí mới từ GPS. sự kiện này sẽ gọi hàm OnLocationChanged để cập nhật vị trí hiện tại.
                var req = new GeolocationListeningRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromMilliseconds(UpdateIntervalMs));

                var started = await Geolocation.StartListeningForegroundAsync(req);

                if (!started)
                {
                    StatusChanged?.Invoke(this, "🛰️ GPS thực đang hoạt động (Polling)");

                    // Fallback to polling if continuous listening is not supported
                    _ = Task.Run(async () =>
                    {
                        while (IsTracking && !ct.IsCancellationRequested)
                        {
                            try
                            {
                                var loc = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5)), ct);
                                if (loc != null)
                                {
                                    PushLocation(new GpsCoordinate(loc.Latitude, loc.Longitude, loc.Accuracy ?? 10));
                                }
                            }
                            catch { }

                            await Task.Delay(UpdateIntervalMs, ct);
                        }
                    }, ct);
                }
                else
                {
                    StatusChanged?.Invoke(this, "🛰️ GPS thực đang hoạt động");

                    try
                    {
                        var lastLoc = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5)), ct);
                        if (lastLoc != null)
                        {
                            PushLocation(new GpsCoordinate(lastLoc.Latitude, lastLoc.Longitude, lastLoc.Accuracy ?? 10));
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"⚠️ GPS lỗi: {ex.Message}");
                IsTracking = false;
            }
        }

        private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
        {
            var loc = new GpsCoordinate(
                e.Location.Latitude,
                e.Location.Longitude,
                e.Location.Accuracy ?? 10);
            //Flow: Gửi tọa độ
            PushLocation(loc);
        }

        // simulation removed

        private void PushLocation(GpsCoordinate loc)
        {
            if (_lastLocation != null)
                TotalDistance += _lastLocation.DistanceTo(loc);
            CurrentLocation = loc;
            _lastLocation = loc;
            LocationUpdated?.Invoke(this, new GpsUpdateEventArgs(loc));
        }

        public void Dispose() => StopTracking();
    }
}
