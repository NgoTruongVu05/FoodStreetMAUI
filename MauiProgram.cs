using FoodStreetMAUI.Services;
using FoodStreetMAUI.ViewModels;
using FoodStreetMAUI.Views;
using Microsoft.Extensions.Logging;

namespace FoodStreetMAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        // Services (Singleton — shared across app lifetime)
        builder.Services.AddSingleton<GpsService>();
        builder.Services.AddSingleton<GeofenceService>();
        builder.Services.AddSingleton<AudioService>();
        builder.Services.AddSingleton<DataService>();

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
