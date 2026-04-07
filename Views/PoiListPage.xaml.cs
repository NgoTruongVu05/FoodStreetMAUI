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

    private void OnPoiTapped(object sender, TappedEventArgs e)
    {
        // Lấy thẻ Border đã được click
        if (sender is Border border)
        {
            // Lấy Layout thực tế đang bọc Grid và Label
            if (border.Content is Layout layout)
            {
                // Lấy phần tử con cuối cùng trong Layout (chính là Label mô tả)
                var descriptionLabel = layout.Children.LastOrDefault() as Label;

                if (descriptionLabel != null)
                {
                    // Thay đổi trạng thái ẩn/hiện
                    descriptionLabel.IsVisible = !descriptionLabel.IsVisible;
                }
            }
        }
    }

    private void OnPlayAudioTapped(object sender, EventArgs e)
    {
        // Ngăn chặn sự kiện click lan ra Border cha (OnPoiTapped)
        // Lấy thông tin PointOfInterest được click
        if (sender is BindableObject bindable
            && bindable.BindingContext is Models.PointOfInterest poi
            && BindingContext is MainViewModel vm)
        {
            vm.PlayPoiAudio(poi);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is MainViewModel vm)
        {
            vm.StopAudio(); // gọi hàm dừng audio
        }
    }
}