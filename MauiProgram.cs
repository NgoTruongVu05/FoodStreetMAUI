using FoodStreetGuide.Data;
using FoodStreetGuide.Services;
using FoodStreetGuide.ViewModels;
using FoodStreetGuide.Views;
using Plugin.Maui.Audio;

// CommunityToolkit.Maui bị bỏ vì chưa tương thích workload 10.0.20.
// Thay thế: dùng MAUI built-in Toast/Alert trực tiếp từ MainThread.
// Khi SDK 10.0.300 ra, thêm lại: using CommunityToolkit.Maui;

namespace FoodStreetGuide;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            // .UseMauiCommunityToolkit() — bỏ, CT.Maui chưa support workload 10.0.20
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",  "OpenSansRegular");
                fonts.AddFont("OpenSans-SemiBold.ttf", "OpenSansSemiBold");
            })
            .ConfigureLifecycleEvents(lifecycle =>
            {
#if ANDROID
                lifecycle.AddAndroid(android => android
                    .OnStop (_ => GetOrchestrator()?.OnAppBackgrounded())
                    .OnStart(_ => GetOrchestrator()?.OnAppForegrounded())
                );
#elif IOS
                lifecycle.AddiOS(ios => ios
                    .WillResignActive(_ => GetOrchestrator()?.OnAppBackgrounded())
                    .OnActivated     (_ => GetOrchestrator()?.OnAppForegrounded())
                );
#endif
            });

        // ── Services ──────────────────────────────────────────────────────────
        builder.Services.AddSingleton<IGpsService,       GpsService>();
        builder.Services.AddSingleton<IGeofenceService,  GeofenceService>();
        builder.Services.AddSingleton<IAudioService,     AudioService>();
        builder.Services.AddSingleton<ITourOrchestrator, TourOrchestrator>();
        builder.Services.AddSingleton<IPoiRepository,    SamplePoiRepository>();
        builder.Services.AddSingleton(AudioManager.Current);

        // ── ViewModels ────────────────────────────────────────────────────────
        builder.Services.AddTransient<AudioPlayerViewModel>();
        builder.Services.AddTransient<MainViewModel>();

        // ── Views ─────────────────────────────────────────────────────────────
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<App>();

        return builder.Build();
    }

    private static TourOrchestrator? GetOrchestrator() =>
        IPlatformApplication.Current?.Services
            .GetService<ITourOrchestrator>() as TourOrchestrator;
}

internal static class ObjectExtensions
{
    public static T Let<T>(this T self, Action<T> action)
    {
        action(self);
        return self;
    }
}