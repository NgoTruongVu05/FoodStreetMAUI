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
        public bool IsSimulating { get; private set; }
        public double TotalDistance { get; private set; }

        public int UpdateIntervalMs { get; set; } = 2000;

        private CancellationTokenSource? _cts;
        private GpsCoordinate? _lastLocation;

        // ── Simulation path: Bùi Viện, HCMC ─────────────────────────────────
        private readonly List<GpsCoordinate> _simPath = new()
        {
            new(10.76926, 106.69204), new(10.76910, 106.69220),
            new(10.76895, 106.69235), new(10.76880, 106.69248),
            new(10.76865, 106.69262), new(10.76850, 106.69275),
            new(10.76835, 106.69288), new(10.76818, 106.69302),
            new(10.76805, 106.69315), new(10.76790, 106.69328),
        };
        private int _simIndex;
        private int _simDir = 1;

        // ── Start / Stop ──────────────────────────────────────────────────────
        public async Task StartTrackingAsync(bool simulate = false)
        {
            if (IsTracking) return;
            IsTracking = true;
            IsSimulating = simulate;
            _cts = new CancellationTokenSource();

            if (simulate)
            {
                _ = SimulationLoopAsync(_cts.Token);
                StatusChanged?.Invoke(this, "🎮 Mô phỏng GPS");
            }
            else
            {
                await StartRealGpsAsync(_cts.Token);
            }
        }

        public void StopTracking()
        {
            _cts?.Cancel();
            IsTracking = false;

            if (!IsSimulating)
            {
                try { Geolocation.StopListeningForeground(); } catch { }
            }

            StatusChanged?.Invoke(this, "⏹ GPS đã dừng");
        }

        // ── Real GPS ──────────────────────────────────────────────────────────
        private async Task StartRealGpsAsync(CancellationToken ct)
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    StatusChanged?.Invoke(this, "❌ Chưa cấp quyền GPS");
                    IsTracking = false;
                    return;
                }

                var req = new GeolocationListeningRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromMilliseconds(UpdateIntervalMs));

                Geolocation.LocationChanged += OnLocationChanged;
                var started = await Geolocation.StartListeningForegroundAsync(req);

                if (!started)
                {
                    StatusChanged?.Invoke(this, "❌ Không khởi động được GPS");
                    IsTracking = false;
                }
                else
                {
                    StatusChanged?.Invoke(this, "🛰️ GPS thực đang hoạt động");
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
            PushLocation(loc);
        }

        // ── Simulation ────────────────────────────────────────────────────────
        private async Task SimulationLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var pt = _simPath[_simIndex];
                    var rng = Random.Shared;
                    var jitter = 0.000006;
                    var loc = new GpsCoordinate(
                        pt.Latitude + (rng.NextDouble() - 0.5) * jitter,
                        pt.Longitude + (rng.NextDouble() - 0.5) * jitter,
                        3.0 + rng.NextDouble() * 3);

                    PushLocation(loc);

                    _simIndex += _simDir;
                    if (_simIndex >= _simPath.Count) { _simDir = -1; _simIndex = _simPath.Count - 2; }
                    if (_simIndex < 0) { _simDir = 1; _simIndex = 1; }

                    await Task.Delay(UpdateIntervalMs, ct);
                }
                catch (TaskCanceledException) { break; }
                catch { /* swallow */ }
            }
        }

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
