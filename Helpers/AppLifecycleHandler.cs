using FoodStreetGuide.Services;

namespace FoodStreetGuide.Helpers;

/// <summary>
/// MAUI lifecycle handler: tự động chuyển GPS Foreground ↔ Background
/// khi app bị minimize hoặc resume, nhằm tối ưu pin.
/// </summary>
public sealed class AppLifecycleHandler : ILifecycleEventService
{
    private readonly TourOrchestrator _orchestrator;

    public AppLifecycleHandler(TourOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public void OnSleep()   => _ = _orchestrator.OnAppBackgrounded();
    public void OnResume()  => _ = _orchestrator.OnAppForegrounded();
    public void OnStarted() { }
    public void OnStopped() { }
}

/// <summary>
/// Minimal interface đại diện cho app lifecycle events.
/// Triển khai thực tế qua MauiAppBuilder.ConfigureLifecycleEvents().
/// </summary>
public interface ILifecycleEventService
{
    void OnSleep();
    void OnResume();
    void OnStarted();
    void OnStopped();
}
