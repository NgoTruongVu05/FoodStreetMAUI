using FoodStreetMAUI.Services;
using FoodStreetMAUI.Views;

namespace FoodStreetMAUI;

public partial class App : Application
{
    private readonly MainPage _mainPage;
    private readonly HeartbeatService _heartbeat;

    public App(MainPage mainPage, HeartbeatService heartbeat)
    {
        InitializeComponent();
        _mainPage = mainPage;
        _heartbeat = heartbeat;
        _heartbeat.Start();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new NavigationPage(_mainPage)
        {
            BarBackgroundColor = Color.FromArgb("#13131C"),
            BarTextColor = Color.FromArgb("#F0A030"),
        });

        window.Created += (_, _) => _heartbeat.Start();
        window.Activated += (_, _) => _heartbeat.Start();
        window.Resumed += (_, _) => _heartbeat.Start();

        window.Deactivated += (_, _) => _heartbeat.Stop();
        window.Stopped += (_, _) => _heartbeat.Stop();
        window.Destroying += (_, _) => _heartbeat.Stop();

        return window;
    }
}
