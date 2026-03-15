using CommunityToolkit.Mvvm.ComponentModel;
using FoodStreetGuide.Models;

namespace FoodStreetGuide.ViewModels;

/// <summary>
/// UI wrapper cho PointOfInterest – cung cấp các property binding-ready.
/// </summary>
public sealed partial class PoiViewModel : ObservableObject
{
    private readonly PointOfInterest _model;
    private AppLanguage _currentLanguage;

    public PoiViewModel(PointOfInterest model, AppLanguage language = AppLanguage.Vietnamese)
    {
        _model           = model;
        _currentLanguage = language;
    }

    public Guid   Id       => _model.Id;
    public string Emoji    => _model.Emoji;
    public double Lat      => _model.Zone.Center.Latitude;
    public double Lng      => _model.Zone.Center.Longitude;
    public double Radius   => _model.Zone.TriggerRadius;

    [ObservableProperty] private GeofenceState _state = GeofenceState.Outside;
    [ObservableProperty] private double        _distanceMeters = double.MaxValue;
    [ObservableProperty] private string        _title       = string.Empty;
    [ObservableProperty] private string        _description = string.Empty;
    [ObservableProperty] private string        _meta        = string.Empty;
    [ObservableProperty] private bool          _isNearby;
    [ObservableProperty] private bool          _isInside;
    [ObservableProperty] private bool          _canTrigger;

    public string DistanceLabel =>
        DistanceMeters >= double.MaxValue ? "—" :
        DistanceMeters < 1000 ? $"{(int)DistanceMeters}m" :
        $"{DistanceMeters / 1000:F1}km";

    public string StateLabel => State switch
    {
        GeofenceState.Inside  => _currentLanguage switch
        {
            AppLanguage.English  => "Inside zone",
            AppLanguage.Chinese  => "区域内",
            AppLanguage.Japanese => "範囲内",
            _                    => "Trong vùng"
        },
        GeofenceState.Nearby  => _currentLanguage switch
        {
            AppLanguage.English  => "Approaching",
            AppLanguage.Chinese  => "接近中",
            AppLanguage.Japanese => "接近中",
            _                    => "Đang đến gần"
        },
        _ => _currentLanguage switch
        {
            AppLanguage.English  => "Out of range",
            AppLanguage.Chinese  => "范围外",
            AppLanguage.Japanese => "範囲外",
            _                    => "Ngoài vùng"
        }
    };

    public Color StateBadgeColor => State switch
    {
        GeofenceState.Inside => Color.FromArgb("#1B4332"),
        GeofenceState.Nearby => Color.FromArgb("#1E3A5F"),
        _                    => Color.FromArgb("#2D2D2D")
    };

    public Color StateBadgeTextColor => State switch
    {
        GeofenceState.Inside => Color.FromArgb("#4ADE80"),
        GeofenceState.Nearby => Color.FromArgb("#60A5FA"),
        _                    => Color.FromArgb("#888780")
    };

    /// <summary>Refresh tất cả UI khi ngôn ngữ hoặc model state thay đổi.</summary>
    public void Refresh(AppLanguage language)
    {
        _currentLanguage = language;
        var content      = _model.Content.GetOrDefault(language);

        Title          = content.Title;
        Description    = content.Description;
        State          = _model.CurrentState;
        DistanceMeters = _model.DistanceMeters;
        IsNearby       = _model.CurrentState == GeofenceState.Nearby;
        IsInside       = _model.CurrentState == GeofenceState.Inside;
        CanTrigger     = _model.CanTrigger(_model.Zone.DebounceSeconds);

        OnPropertyChanged(nameof(DistanceLabel));
        OnPropertyChanged(nameof(StateLabel));
        OnPropertyChanged(nameof(StateBadgeColor));
        OnPropertyChanged(nameof(StateBadgeTextColor));
    }

    public PointOfInterest Model => _model;
}
