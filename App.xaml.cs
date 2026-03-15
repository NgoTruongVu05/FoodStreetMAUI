using FoodStreetGuide.Services;
using FoodStreetGuide.Views;

namespace FoodStreetGuide;

public partial class App : Application
{
    private readonly ITourOrchestrator _orchestrator;

    // .NET 10 / MAUI 10: constructor injection is unchanged.
    public App(ITourOrchestrator orchestrator, MainPage mainPage)
    {
        InitializeComponent();
        _orchestrator = orchestrator;

        // NavigationPage shell — colors use the same Color.FromArgb API in .NET 10.
        MainPage = new NavigationPage(mainPage)
        {
            BarBackgroundColor = Color.FromArgb("#0A0A0F"),
            BarTextColor       = Colors.White
        };
    }

    // ── App lifecycle ─────────────────────────────────────────────────────────
    // OnSleep / OnResume are preserved in .NET 10 MAUI.
    // Platform-specific handlers in MauiProgram.cs (AddAndroid / AddiOS)
    // fire *before* these, so background GPS hand-off happens in both paths.

    protected override void OnSleep()
    {
        base.OnSleep();
        // Pattern-match cast — safe if DI wires a TourOrchestrator.
        if (_orchestrator is TourOrchestrator to)
            _ = to.OnAppBackgrounded();
    }

    protected override void OnResume()
    {
        base.OnResume();
        if (_orchestrator is TourOrchestrator to)
            _ = to.OnAppForegrounded();
    }
}