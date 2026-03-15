using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

/// <summary>
/// Contract GPS tracking – abstraction cho DI và unit testing.
/// Triển khai khác nhau cho Android/iOS.
/// </summary>
public interface IGpsService : IAsyncDisposable
{
    /// <summary>Vị trí hiện tại của user.</summary>
    GeoLocation? CurrentLocation { get; }

    /// <summary>Cấu hình tracking đang dùng.</summary>
    GpsTrackingConfig Config { get; }

    /// <summary>GPS có đang chạy không.</summary>
    bool IsTracking { get; }

    /// <summary>Phát vị trí mới (foreground + background).</summary>
    IObservable<GeoLocation> LocationUpdates { get; }

    /// <summary>Bắt đầu tracking với config cho trước.</summary>
    Task StartAsync(GpsTrackingConfig config, CancellationToken ct = default);

    /// <summary>Dừng tracking, giải phóng tài nguyên GPS.</summary>
    Task StopAsync();

    /// <summary>Chuyển đổi giữa Foreground ↔ Background config.</summary>
    Task SwitchConfigAsync(GpsTrackingConfig newConfig);

    /// <summary>Yêu cầu permission GPS từ user (trả về true nếu được cấp).</summary>
    Task<bool> RequestPermissionsAsync();
}
