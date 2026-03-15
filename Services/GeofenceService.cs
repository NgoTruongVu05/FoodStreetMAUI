using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

// ── Event args ───────────────────────────────────────────────────────────────

public sealed class GeofenceTriggeredEventArgs : EventArgs
{
    public PointOfInterest Poi      { get; init; }
    public GeofenceState   NewState { get; init; }
    public GeofenceState   OldState { get; init; }
    public double          Distance { get; init; }

    public GeofenceTriggeredEventArgs(PointOfInterest poi,
        GeofenceState newState, GeofenceState oldState, double distance)
    {
        Poi      = poi;
        NewState = newState;
        OldState = oldState;
        Distance = distance;
    }
}

// ── Interface ────────────────────────────────────────────────────────────────

public interface IGeofenceService
{
    IReadOnlyList<PointOfInterest> AllPois { get; }

    /// <summary>Kích hoạt khi user vào/gần/rời vùng POI.</summary>
    event EventHandler<GeofenceTriggeredEventArgs> GeofenceStateChanged;

    /// <summary>Kích hoạt chỉ khi user bước vào vùng trigger (Inside).</summary>
    event EventHandler<GeofenceTriggeredEventArgs> PoiEntered;

    void LoadPois(IEnumerable<PointOfInterest> pois);
    void UpdateUserLocation(GeoLocation location);
    PointOfInterest? GetNearestActivePoint();
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>
/// Geofence engine: đánh giá trạng thái tất cả POI mỗi lần GPS update.
/// Debounce chống spam, ưu tiên POI theo Priority khi nhiều điểm cùng trigger.
/// </summary>
public sealed class GeofenceService : IGeofenceService
{
    private readonly List<PointOfInterest> _pois = new();
    private GeoLocation? _lastLocation;

    public IReadOnlyList<PointOfInterest> AllPois => _pois.AsReadOnly();

    public event EventHandler<GeofenceTriggeredEventArgs>? GeofenceStateChanged;
    public event EventHandler<GeofenceTriggeredEventArgs>? PoiEntered;

    public void LoadPois(IEnumerable<PointOfInterest> pois)
    {
        _pois.Clear();
        _pois.AddRange(pois);
    }

    public void UpdateUserLocation(GeoLocation location)
    {
        _lastLocation = location;

        foreach (var poi in _pois)
        {
            var oldState = poi.CurrentState;
            var newState = poi.Zone.Evaluate(location);
            var distance = poi.Zone.DistanceFrom(location);

            poi.DistanceMeters = distance;

            if (newState == oldState) continue;

            poi.CurrentState = newState;

            var args = new GeofenceTriggeredEventArgs(poi, newState, oldState, distance);
            GeofenceStateChanged?.Invoke(this, args);

            // Trigger audio chỉ khi vào vùng Inside + debounce ok
            if (newState == GeofenceState.Inside &&
                poi.CanTrigger(poi.Zone.DebounceSeconds))
            {
                poi.LastTriggeredAt = DateTime.UtcNow;
                PoiEntered?.Invoke(this, args);
            }
        }
    }

    /// <summary>
    /// Trả về POI gần nhất đang ở trạng thái Inside hoặc Nearby,
    /// ưu tiên theo Priority rồi đến khoảng cách.
    /// </summary>
    public PointOfInterest? GetNearestActivePoint()
    {
        return _pois
            .Where(p => p.CurrentState != GeofenceState.Outside)
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.DistanceMeters)
            .FirstOrDefault();
    }
}
