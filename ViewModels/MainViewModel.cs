using System;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
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
        private readonly LanguageService _languageService;

        [ObservableProperty] string currentLang = "vi";
        [ObservableProperty] string gpsStatusText = "GPS chưa khởi động";
        [ObservableProperty] string coordinateText = "---";
        [ObservableProperty] string accuracyText = "";
        [ObservableProperty] bool isTracking = false;
        [ObservableProperty] bool isSimulating = false;
        [ObservableProperty] bool isMuted = false;
        [ObservableProperty] float volume = 0.8f;
        [ObservableProperty] string nowPlayingTitle = "Chờ kích hoạt POI...";
        [ObservableProperty] string nowPlayingDesc = "";
        [ObservableProperty] string selectedPoiTitle = "";
        [ObservableProperty] string selectedPoiDesc = "";
        [ObservableProperty] PointOfInterest? selectedPoi;
        [ObservableProperty] bool isPlayingAudio = false;
        [ObservableProperty] bool isPoiModalVisible = false;
        [ObservableProperty] LanguageItem selectedLanguage;
        bool isNowPlayingModalVisible = false;
        public bool IsNowPlayingModalVisible
        {
            get => isNowPlayingModalVisible;
            set => SetProperty(ref isNowPlayingModalVisible, value);
        }
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
        public ObservableCollection<LanguageItem> Languages { get; } = new();

        private DateTime _sessionStart;
        private System.Timers.Timer? _sessionTimer;
        private readonly object _locSync = new();
        private GpsCoordinate? _latestLocation;

        public MainViewModel(GpsService gps, GeofenceService geo,
                             AudioService audio, DataService data)
        {
            _gps = gps;
            _geo = geo;
            _audio = audio;
            _data = data;
            _languageService = new LanguageService();

            _gps.LocationUpdated += OnLocationUpdated;
            _gps.StatusChanged += OnGpsStatusChanged;
            _geo.GeofenceTriggered += OnGeofenceTriggered;
            _geo.LogMessage += (s, m) => AddLog(m);
            _audio.StatusChanged += (s, m) =>
            {
                AddLog(m);
                if (m == "Phat xong")
                {
                    IsPlayingAudio = false;
                }
            };
            Pois.CollectionChanged += OnPoisCollectionChanged;
            SelectedLanguage = new LanguageItem(string.Empty, string.Empty);
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
                AddLog("Đã tải " + pois.Count + " điểm thuyết minh");
                await LoadLanguagesAsync();
                NearestPoiOrLocationChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                AddLog("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        private async Task LoadLanguagesAsync()
        {
            try
            {
                var languages = await _languageService.GetLanguagesAsync();
                if (languages.Count == 0)
                {
                    return;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Languages.Clear();
                    foreach (var language in languages)
                    {
                        if (string.IsNullOrWhiteSpace(language?.Code))
                        {
                            continue;
                        }

                        // Show only the language name in the picker (do not display the code).
                        // If Name is missing, fall back to Code to avoid an empty entry.
                        var label = string.IsNullOrWhiteSpace(language.Name)
                            ? language.Code
                            : language.Name;

                        Languages.Add(new LanguageItem(language.Code, label));
                    }

                    if (Languages.Count > 0)
                    {
                        SelectedLanguage = Languages.FirstOrDefault(l => l.Code == CurrentLang) ?? Languages[0];
                    }
                });
            }
            catch (Exception ex)
            {
                AddLog("Lỗi tải ngôn ngữ: " + ex.Message);
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
                    AddLog(IsSimulating ? "Mô phỏng tour Bui Vien..." : "GPS thực đang hoạt động...");
                }
            }
            catch (Exception ex)
            {
                AddLog("Lỗi GPS: " + ex.Message);
            }
        }

        [RelayCommand]
        void SelectLanguage(string lang)
        {
            CurrentLang = string.IsNullOrWhiteSpace(lang) ? "vi" : lang;
            AddLog("Ngôn ngữ: " + CurrentLang.ToUpper());
            // Nếu đang phát audio thì dừng và xóa thông tin hiển thị
            try
            {
                _audio.StopAll();
            }
            catch { }
            IsPlayingAudio = false;
            NowPlayingTitle = "Chờ kích hoạt POI...";
            NowPlayingDesc = "";
            SelectedPoi = null;
            IsNowPlayingModalVisible = false;
            IsPoiModalVisible = false;
        }

        [RelayCommand]
        public void StopAudio()
        {
            _audio.StopAll();
            IsPlayingAudio = false;
            NowPlayingTitle = "Chờ kích hoạt POI...";
            NowPlayingDesc = "";
        }

        [RelayCommand]
        void ShowFullDescription()
        {
            if (!string.IsNullOrEmpty(NowPlayingDesc))
            {
                IsNowPlayingModalVisible = true;
            }
        }

        [RelayCommand]
        void ClosePoiModal()
        {
            IsPoiModalVisible = false;
        }

        [RelayCommand]
        void CloseNowPlayingModal()
        {
            IsNowPlayingModalVisible = false;
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
                AddLog("Teleport đến: " + poi.Name);
            }
            catch (Exception ex)
            {
                AddLog("Lỗi teleport: " + ex.Message);
            }
        }

        private void OnLocationUpdated(object? sender, GpsUpdateEventArgs e)
        {
            lock (_locSync) { _latestLocation = e.Location; }
            // Cập nhật geofence NGAY (không cần main thread)
            try { _geo.UpdateLocation(e.Location); }
            catch (Exception ex) { AddLog("Loi geofence update: " + ex.Message); }

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
                            NearestPoiSummary = newNearestDist < 1000
                                ? $"{p.DisplayName} · {(int)newNearestDist} m"
                                : $"{p.DisplayName} · {(newNearestDist / 1000).ToString("F2")} km";
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

                    AddLog((e.EventType == "Enter" || e.EventType == "EnterFollow" ? "DA DEN: " : "GAN DEN: ") + e.Poi.Name + " (" + (int)e.Distance + "m)");

                    // For the first Enter we want to immediately update UI and play with priority.
                    if (e.EventType == "Enter")
                    {
                        SelectedPoi = e.Poi;
                        NowPlayingTitle = e.Poi.Emoji + " " + content.Title;
                        NowPlayingDesc = content.Description;
                        IsPlayingAudio = true;

                        _audio.Volume = Volume;
                        _audio.PlayContent(
                            content,
                            priority: false,
                            tag: e.Poi.Id.ToString());
                        VisitedCount++;
                    }
                    else if (e.EventType == "Approach")
                    {
                        // Queue approach audio; when it starts, update the NowPlaying UI to reflect this poi
                        _audio.Volume = Volume;
                        _audio.PlayContent(
                            content,
                            priority: false,
                            onStart: () =>
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    SelectedPoi = e.Poi;
                                    NowPlayingTitle = e.Poi.Emoji + " " + content.Title;
                                    NowPlayingDesc = content.Description;
                                    IsPlayingAudio = true;
                                });
                            },
                            tag: e.Poi.Id.ToString(),
                            shouldStart: () => IsPoiStillRelevant(e.Poi));
                    }
                }
                catch (Exception ex)
                {
                    AddLog("Loi geofence: " + ex.Message);
                }
            });

        private bool IsPoiStillRelevant(PointOfInterest poi)
        {
            GpsCoordinate? loc;
            lock (_locSync) { loc = _latestLocation; }
            if (loc == null) return true;
            var dist = loc.DistanceTo(poi.Location);
            var keepRadius = Math.Max(poi.TriggerRadius * 1.3, poi.ApproachRadius * 1.1);
            return dist <= keepRadius;
        }

        public void PlayPoiAudio(PointOfInterest poi)
        {
            SelectedPoi = poi;

            var content = poi.GetContent(CurrentLang);
            if (content == null) return;

            NowPlayingTitle = poi.Emoji + " " + content.Title;
            NowPlayingDesc = content.Description;
            IsPlayingAudio = true;

            _audio.Volume = Volume;
            _audio.PlayContent(content, priority: true, tag: poi.Id.ToString());
        }

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

        partial void OnSelectedLanguageChanged(LanguageItem value)
        {
            if (value != null)
            {
                CurrentLang = string.IsNullOrWhiteSpace(value.Code) ? "vi" : value.Code;
                AddLog("Ngôn ngữ: " + CurrentLang.ToUpper());
            }

            try
            {
                _audio.StopAll();
            }
            catch { }
            IsPlayingAudio = false;
            NowPlayingTitle = "Chờ kích hoạt POI...";
            NowPlayingDesc = "";
            SelectedPoi = null;
            IsNowPlayingModalVisible = false;
            IsPoiModalVisible = false;
        }
    }

    public class LanguageItem
    {
        public string Code { get; }
        public string Label { get; }
        public LanguageItem(string code, string label) { Code = code; Label = label; }
    }
}
