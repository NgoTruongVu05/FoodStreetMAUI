using FoodStreetMAUI.Views;
namespace FoodStreetMAUI;

public partial class App : Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(_mainPage)
        {
            BarBackgroundColor = Color.FromArgb("#13131C"),
            BarTextColor = Color.FromArgb("#F0A030"),
        });
    }
}
