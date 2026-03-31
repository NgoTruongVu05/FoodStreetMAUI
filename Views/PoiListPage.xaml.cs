using FoodStreetMAUI.ViewModels;
using Microsoft.Maui.Controls;

namespace FoodStreetMAUI.Views;

public partial class PoiListPage : ContentPage
{
    public PoiListPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}