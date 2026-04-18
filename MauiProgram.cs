using FoodStreetMAUI.Services;
using FoodStreetMAUI.ViewModels;
using FoodStreetMAUI.Views;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using ZXing.Net.Maui.Controls;

namespace FoodStreetMAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            // Mapsui.Maui cần SkiaSharp (handler SKGLView trên Android)
            .UseSkiaSharp()
            .UseBarcodeReader();

        // Services (Singleton — shared across app lifetime)
        builder.Services.AddSingleton<GpsService>();
        builder.Services.AddSingleton<GeofenceService>();
        builder.Services.AddSingleton<AudioService>();
        builder.Services.AddSingleton<DataService>();
        builder.Services.AddSingleton<HeartbeatService>();
        builder.Services.AddSingleton<UiTextService>();

        // ViewModel + View
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
