using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodStreetGuide.Data;
using FoodStreetGuide.Models;
using FoodStreetGuide.Services;

namespace FoodStreetGuide.ViewModels;

/// <summary>
/// ViewModel chính – quản lý tour, ngôn ngữ, danh sách POI và tab hiện tại.
/// </summary>
public sealed partial class MainViewModel : BaseViewModel
{
    private readonly ITourOrchestrator  _orchestrator;
    private readonly IGeofenceService   _geofence;
    private readonly IAudioService      _audio;
    private readonly IPoiRepository     _repository;

    public AudioPlayerViewModel Player { get; }

    public MainViewModel(
        ITourOrchestrator  orchestrator,
        IGeofenceService   geofence,
        IAudioService      audio,
        IPoiRepository     repository,
        AudioPlayerViewModel player)
    {
        _orchestrator = orchestrator;
        _geofence     = geofence;
        _audio        = audio;
        _repository   = repository;
        Player        = player;

        _orchestrator.PoiTriggered     += OnPoiTriggered;
        _orchestrator.LocationUpdated  += OnLocationUpdated;
    }

    // ── Observable state ──────────────────────────────────────────────────────
    [ObservableProperty] private bool         _isTourRunning;
    [ObservableProperty] private string       _coordinateText    = "—";
    [ObservableProperty] private string       _gpsAccuracyText   = "—";
    [ObservableProperty] private string       _gpsModeText       = "—";
    [ObservableProperty] private string       _gpsIntervalText   = "—";
    [ObservableProperty] private AppLanguage  _selectedLanguage  = AppLanguage.Vietnamese;
    [ObservableProperty] private int          _triggeredCount;
    [ObservableProperty] private PoiViewModel? _activePoi;
    [ObservableProperty] private int          _activeTabIndex;   // 0=Guide, 1=POI, 2=System

    public ObservableCollection<PoiViewModel> Pois { get; } = new();

    // ── Language options for picker ───────────────────────────────────────────
    public List<LanguageOption> Languages { get; } = new()
    {
        new("🇻🇳 Tiếng Việt", AppLanguage.Vietnamese),
        new("🇬🇧 English",     AppLanguage.English),
        new("🇨🇳 中文",         AppLanguage.Chinese),
        new("🇯🇵 日本語",        AppLanguage.Japanese)
    };

    // ── Commands ──────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task InitAsync()
    {
        await RunSafeAsync(async () =>
        {
            var rawPois = await _repository.GetAllAsync();
            _geofence.LoadPois(rawPois);

            foreach (var poi in rawPois)
            {
                var vm = new PoiViewModel(poi, SelectedLanguage);
                vm.Refresh(SelectedLanguage);
                Pois.Add(vm);
            }

            await _orchestrator.StartTourAsync();
            IsTourRunning  = true;
            GpsModeText    = "Foreground";
            GpsIntervalText= "2s";
        }, "Khởi động tour");
    }

    [RelayCommand]
    private async Task ToggleTourAsync()
    {
        await RunSafeAsync(async () =>
        {
            if (IsTourRunning)
            {
                await _orchestrator.StopTourAsync();
                IsTourRunning = false;
                GpsModeText   = "Dừng";
            }
            else
            {
                await _orchestrator.StartTourAsync();
                IsTourRunning   = true;
                GpsModeText     = "Foreground";
                GpsIntervalText = "2s";
            }
        });
    }

    [RelayCommand]
    private async Task SetLanguageAsync(AppLanguage language)
    {
        SelectedLanguage = language;
        await _orchestrator.SetLanguageAsync(language);
        RefreshAllPoiVms();
        if (ActivePoi is not null) ActivePoi.Refresh(language);
    }

    [RelayCommand]
    private void SelectPoi(PoiViewModel vm)
    {
        ActivePoi   = vm;
        ActiveTabIndex = 0; // về tab Guide
        Player.LoadTrack(vm.Model, SelectedLanguage);
    }

    // ── Event Handlers ────────────────────────────────────────────────────────
    private void OnPoiTriggered(object? sender, PointOfInterest poi)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TriggeredCount++;
            var vm = Pois.FirstOrDefault(v => v.Id == poi.Id);
            if (vm is null) return;

            vm.Refresh(SelectedLanguage);
            ActivePoi = vm;
            Player.LoadTrack(poi, SelectedLanguage);
            ActiveTabIndex = 0;
        });
    }

    private void OnLocationUpdated(object? sender, GeoLocation loc)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CoordinateText  = $"{loc.Latitude:F4}° N · {loc.Longitude:F4}° E";
            GpsAccuracyText = $"±{loc.Accuracy:F0}m";
            RefreshAllPoiVms();
        });
    }

    private void RefreshAllPoiVms()
    {
        foreach (var vm in Pois) vm.Refresh(SelectedLanguage);
    }
}

/// <summary>Helper record cho Language picker.</summary>
public record LanguageOption(string Label, AppLanguage Language);
