using System;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodStreetMAUI.Models;
using FoodStreetMAUI.Services;
using Microsoft.Maui.ApplicationModel;

namespace FoodStreetMAUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly GpsService _gps;
        private readonly GeofenceService _geo;
        private readonly AudioService _audio;
        private readonly DataService _data;

        [ObservableProperty] string currentLang = "vi";
        [ObservableProperty] string gpsStatusText = "GPS chua khoi dong";
        [ObservableProperty] string coordinateText = "---";
        [ObservableProperty] string accuracyText = "";
        [ObservableProperty] bool isTracking = false;
        [ObservableProperty] bool isSimulating = false;
        [ObservableProperty] bool isMuted = false;
        [ObservableProperty] float volume = 0.8f;
        [ObservableProperty] string nowPlayingTitle = "Cho kich hoat POI...";
        [ObservableProperty] string nowPlayingDesc = "";
        [ObservableProperty] bool isPlayingAudio = false;
        [ObservableProperty] int visitedCount = 0;
        [ObservableProperty] string distanceText = "0 m";
        [ObservableProperty] string sessionTimeText = "00:00";
        [ObservableProperty] string startButtonText = "Bật GPS";
        [ObservableProperty] string startButtonColor = "#1a3a1e";
        [ObservableProperty] double? lastLatitude;
        [ObservableProperty] double? lastLongitude;
        [ObservableProperty] Guid? nearestPoiId;
        [ObservableProperty] double nearestPoiDistanceMeters;
        [ObservableProperty] string nearestPoiSummary = "";
        [ObservableProperty] bool showNearestPoiBanner;

        public ObservableCollection<PointOfInterest> Pois { get; } = new();
        public ObservableCollection<string> LogLines { get; } = new();
        public ObservableCollection<LanguageItem> Languages { get; } = new()
        {
            new("vi", "VN Viet"),
            new("en", "US English"),
            new("zh", "CN Zhongwen"),
            new("ja", "JP Nihongo"),
            new("ko", "KR Hanguk"),
            new("fr", "FR Francais"),
        };

        private DateTime _sessionStart;
        private System.Timers.Timer? _sessionTimer;

        public MainViewModel(GpsService gps, GeofenceService geo,
                             AudioService audio, DataService data)
        {
            _gps = gps;
            _geo = geo;
            _audio = audio;
            _data = data;

            _gps.LocationUpdated += OnLocationUpdated;
            _gps.StatusChanged += OnGpsStatusChanged;
            _geo.GeofenceTriggered += OnGeofenceTriggered;
            _geo.LogMessage += (s, m) => AddLog(m);
            _audio.StatusChanged += (s, m) => AddLog(m);
            Pois.CollectionChanged += OnPoisCollectionChanged;
        }

        public event EventHandler? NearestPoiOrLocationChanged;

        private void OnPoisCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => NearestPoiOrLocationChanged?.Invoke(this, EventArgs.Empty);

        [RelayCommand]
        async Task LoadDataAsync()
        {
            try
            {
                var pois = await _data.LoadPoisAsync();
                Pois.Clear();
                _geo.ClearAll();
                foreach (var p in pois)
                {
                    Pois.Add(p);
                    _geo.AddPoi(p);
                }
                AddLog("Da tai " + pois.Count + " diem thuyet minh");
                NearestPoiOrLocationChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                AddLog("Loi tai du lieu: " + ex.Message);
            }
        }

        [RelayCommand]
        async Task ToggleGpsAsync()
        {
            try
            {
                if (IsTracking)
                {
                    _gps.StopTracking();
                    _sessionTimer?.Stop();
                    IsTracking = false;
                    StartButtonText = "Bật GPS";
                    StartButtonColor = "#1a3a1e";
                }
                else
                {
                    _sessionStart = DateTime.Now;
                    StartSessionTimer();
                    await _gps.StartTrackingAsync(IsSimulating);
                    IsTracking = true;
                    StartButtonText = "Tắt GPS";
                    StartButtonColor = "#3a1a1a";
                    AddLog(IsSimulating ? "Mo phong tour Bui Vien..." : "GPS thuc dang hoat dong...");
                }
            }
            catch (Exception ex)
            {
                AddLog("Loi GPS: " + ex.Message);
            }
        }

        [RelayCommand]
        void SelectLanguage(string lang)
        {
            CurrentLang = lang;
            AddLog("Ngon ngu: " + lang.ToUpper());
        }

        [RelayCommand]
        void StopAudio()
        {
            _audio.StopAll();
            IsPlayingAudio = false;
            NowPlayingTitle = "Cho kich hoat POI...";
            NowPlayingDesc = "";
        }

        [RelayCommand]
        async Task TeleportToFirstPoiAsync()
        {
            try
            {
                if (Pois.Count == 0) return;
                if (!IsTracking) await ToggleGpsAsync();
                var poi = Pois[0];
                _gps.TeleportTo(poi.Location.Latitude, poi.Location.Longitude);
                AddLog("Teleport den: " + poi.Name);
            }
            catch (Exception ex)
            {
                AddLog("Loi teleport: " + ex.Message);
            }
        }

        private void OnLocationUpdated(object? sender, GpsUpdateEventArgs e)
        {
            // Cập nhật geofence NGAY (không cần main thread)
            try { _geo.UpdateLocation(e.Location); } catch { }

            var loc = e.Location;
            var nearby = _geo.GetNearby(loc, maxDist: 1_000_000);
            Guid? newNearestId = null;
            double newNearestDist = 0;
            if (nearby.Count > 0)
            {
                newNearestId = nearby[0].poi.Id;
                newNearestDist = nearby[0].dist;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    LastLatitude = loc.Latitude;
                    LastLongitude = loc.Longitude;
                    CoordinateText = loc.Latitude.ToString("F5") + ", " + loc.Longitude.ToString("F5");
                    AccuracyText = "+-" + loc.Accuracy.ToString("F1") + " m";
                    DistanceText = _gps.TotalDistance < 1000
                        ? ((int)_gps.TotalDistance) + " m"
                        : (_gps.TotalDistance / 1000).ToString("F2") + " km";

                    if (NearestPoiId != newNearestId
                        || Math.Abs(NearestPoiDistanceMeters - newNearestDist) > 0.5)
                    {
                        NearestPoiId = newNearestId;
                        NearestPoiDistanceMeters = newNearestDist;
                        if (newNearestId.HasValue && nearby.Count > 0)
                        {
                            var p = nearby[0].poi;
                            NearestPoiSummary = p.DisplayName + " · " + (int)newNearestDist + " m";
                            ShowNearestPoiBanner = true;
                        }
                        else
                        {
                            NearestPoiSummary = "";
                            ShowNearestPoiBanner = false;
                        }

                    }

                    NearestPoiOrLocationChanged?.Invoke(this, EventArgs.Empty);
                }
                catch { }
            });
        }

        private void OnGpsStatusChanged(object? sender, string msg)
            => MainThread.BeginInvokeOnMainThread(() =>
            {
                GpsStatusText = msg;
                AddLog(msg);
            });

        private void OnGeofenceTriggered(object? sender, GeofenceEventArgs e)
            => MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var content = e.Poi.GetContent(CurrentLang);
                    if (content == null) return;

                    AddLog((e.EventType == "Enter" ? "DA DEN: " : "GAN DEN: ") + e.Poi.Name + " (" + (int)e.Distance + "m)");

                    NowPlayingTitle = e.Poi.Emoji + " " + content.Title;
                    NowPlayingDesc = content.Description.Length > 100
                        ? content.Description[..100] + "..."
                        : content.Description;
                    IsPlayingAudio = true;

                    _audio.Volume = Volume;
                    _audio.PlayContent(content, priority: e.EventType == "Enter");
                    VisitedCount++;
                }
                catch (Exception ex)
                {
                    AddLog("Loi geofence: " + ex.Message);
                }
            });

        private void StartSessionTimer()
        {
            _sessionTimer = new System.Timers.Timer(1000);
            _sessionTimer.Elapsed += (s, e) =>
            {
                var elapsed = DateTime.Now - _sessionStart;
                MainThread.BeginInvokeOnMainThread(() =>
                    SessionTimeText = ((int)elapsed.TotalMinutes).ToString("D2") + ":" + elapsed.Seconds.ToString("D2"));
            };
            _sessionTimer.Start();
        }

        private void AddLog(string msg)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var time = DateTime.Now.ToString("HH:mm:ss");
                    LogLines.Insert(0, "[" + time + "] " + msg);
                    if (LogLines.Count > 100) LogLines.RemoveAt(LogLines.Count - 1);
                }
                catch { }
            });
        }

        public void Cleanup()
        {
            Pois.CollectionChanged -= OnPoisCollectionChanged;
            try { _gps.Dispose(); } catch { }
            try { _audio.Dispose(); } catch { }
            try { _sessionTimer?.Stop(); } catch { }
        }
    }

    public class LanguageItem
    {
        public string Code { get; }
        public string Label { get; }
        public LanguageItem(string code, string label) { Code = code; Label = label; }
    }
}
