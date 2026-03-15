namespace FoodStreetGuide.Models;

/// <summary>Trạng thái của user so với vùng Geofence một POI.</summary>
public enum GeofenceState
{
    Outside  = 0,   // ngoài vùng hoàn toàn
    Nearby   = 1,   // đang tiến gần (trong 2× radius)
    Inside   = 2    // trong vùng kích hoạt chính
}

/// <summary>Mức độ ưu tiên khi nhiều POI cùng kích hoạt đồng thời.</summary>
public enum PoiPriority
{
    Low    = 0,
    Normal = 1,
    High   = 2,
    Hero   = 3  // điểm nổi bật, luôn được ưu tiên
}

/// <summary>
/// Định nghĩa vùng Geofence của một POI: tâm, bán kính, và ngưỡng "gần".
/// </summary>
public sealed class GeofenceZone
{
    public GeoLocation Center        { get; init; }
    public double TriggerRadius      { get; init; }   // mét – kích hoạt audio
    public double NearbyRadius       { get; init; }   // mét – báo hiệu sắp đến (2× trigger)
    public double DebounceSeconds    { get; init; } = 60; // chống spam re-trigger

    public GeofenceZone(GeoLocation center, double triggerRadius)
    {
        Center        = center;
        TriggerRadius = triggerRadius;
        NearbyRadius  = triggerRadius * 2.0;
    }

    /// <summary>Xác định trạng thái geofence dựa trên khoảng cách thực.</summary>
    public GeofenceState Evaluate(GeoLocation userLocation)
    {
        double dist = Center.DistanceTo(userLocation);
        if (dist <= TriggerRadius) return GeofenceState.Inside;
        if (dist <= NearbyRadius)  return GeofenceState.Nearby;
        return GeofenceState.Outside;
    }

    public double DistanceFrom(GeoLocation userLocation) =>
        Center.DistanceTo(userLocation);
}

/// <summary>
/// Point of Interest – đơn vị cơ bản của hệ thống thuyết minh.
/// Bất biến sau khi khởi tạo (immutable record-style).
/// </summary>
public sealed class PointOfInterest
{
    public Guid   Id          { get; init; } = Guid.NewGuid();
    public string Emoji       { get; init; } = "🍜";
    public PoiPriority Priority { get; init; } = PoiPriority.Normal;

    public GeofenceZone          Zone    { get; init; }
    public LocalizedContentMap   Content { get; init; }

    // Runtime state (mutable, tracked bởi GeofenceService)
    public GeofenceState CurrentState    { get; internal set; } = GeofenceState.Outside;
    public DateTime?     LastTriggeredAt { get; internal set; }
    public double        DistanceMeters  { get; internal set; } = double.MaxValue;

    public bool CanTrigger(double debounceSeconds) =>
        LastTriggeredAt is null ||
        (DateTime.UtcNow - LastTriggeredAt.Value).TotalSeconds >= debounceSeconds;

    public PointOfInterest(GeofenceZone zone, LocalizedContentMap content)
    {
        Zone    = zone;
        Content = content;
    }
}
