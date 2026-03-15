#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using FoodStreetGuide.Services;

namespace FoodStreetGuide.Platforms.Android;

/// <summary>
/// Android Foreground Service để giữ GPS tracking khi app ở background.
/// Hiển thị notification cố định (bắt buộc cho Android 9+).
/// </summary>
[Service(ForegroundServiceType = ForegroundService.TypeLocation)]
public class GpsBackgroundService : Service
{
    private const int    NotificationId      = 1001;
    private const string ChannelId           = "gps_tracking_channel";
    private const string ChannelName         = "GPS Tracking";

    private IGpsService? _gpsService;
    private IBinder?     _binder;

    public override IBinder? OnBind(Intent? intent)
    {
        _binder = new GpsServiceBinder(this);
        return _binder;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        var notification = BuildNotification();
        StartForeground(NotificationId, notification,
            ForegroundService.TypeLocation);

        // Lấy GpsService từ DI
        _gpsService = IPlatformApplication.Current?.Services
            .GetService<IGpsService>();

        return StartCommandResult.Sticky; // Restart nếu bị kill
    }

    public override void OnDestroy()
    {
        StopForeground(StopForegroundFlags.Remove);
        base.OnDestroy();
    }

    // ── Notification ──────────────────────────────────────────────────────────
    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var channel = new NotificationChannel(ChannelId, ChannelName,
            NotificationImportance.Low)
        {
            Description = "Phố Ẩm Thực đang theo dõi vị trí để kích hoạt thuyết minh tự động."
        };
        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification()
    {
        var intent      = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty);
        var pendingFlags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
        var pendingIntent= PendingIntent.GetActivity(this, 0, intent, pendingFlags);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Phố Ẩm Thực · Đang dẫn đường")
            .SetContentText("Đang theo dõi vị trí để tự động phát thuyết minh.")
            .SetSmallIcon(Resource.Drawable.abc_ic_menu_paste_mtrl_am_alpha)
            .SetContentIntent(pendingIntent)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityLow)
            .Build();
    }
}

// ── Binder để Activity giao tiếp với Service ──────────────────────────────────
public class GpsServiceBinder : Binder
{
    public GpsBackgroundService Service { get; }
    public GpsServiceBinder(GpsBackgroundService service) { Service = service; }
}
#endif
