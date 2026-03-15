namespace FoodStreetGuide.Models;

/// <summary>
/// Đại diện cho một tọa độ địa lý (lat/lng) và metadata độ chính xác.
/// </summary>
public sealed class GeoLocation
{
    public double Latitude  { get; init; }
    public double Longitude { get; init; }
    public double Accuracy  { get; init; }   // mét
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public GeoLocation(double latitude, double longitude, double accuracy = 0)
    {
        Latitude  = latitude;
        Longitude = longitude;
        Accuracy  = accuracy;
    }

    /// <summary>
    /// Tính khoảng cách Haversine (mét) giữa hai điểm.
    /// </summary>
    public double DistanceTo(GeoLocation other)
    {
        const double R = 6_371_000; // bán kính Trái Đất, mét
        double dLat = ToRad(other.Latitude  - Latitude);
        double dLon = ToRad(other.Longitude - Longitude);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(ToRad(Latitude)) * Math.Cos(ToRad(other.Latitude))
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    public override string ToString() =>
        $"{Latitude:F6}° N · {Longitude:F6}° E  (±{Accuracy:F0}m)";
}
