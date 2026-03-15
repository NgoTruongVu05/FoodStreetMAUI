using System.Windows.Input;

namespace FoodStreetGuide.Controls;

public partial class PoiCardControl : ContentView
{
    // ── Bindable: SelectCommand từ parent CollectionView ─────────────────────
    public static readonly BindableProperty SelectCommandProperty =
        BindableProperty.Create(
            nameof(SelectCommand),
            typeof(ICommand),
            typeof(PoiCardControl));

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public PoiCardControl()
    {
        InitializeComponent();
    }
}
