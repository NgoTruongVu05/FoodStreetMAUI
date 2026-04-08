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
    private async void OnPoiTapped(object sender, TappedEventArgs e)
    {
        // Khi ấn vào một POI, mở modal chi tiết
        if (sender is BindableObject b && b.BindingContext is Models.PointOfInterest poi)
        {
            await ShowPoiDetailAsync(poi);
        }
    }

    // Public API để trang khác (MainPage) có thể yêu cầu hiển thị chi tiết của POI
    public async System.Threading.Tasks.Task ShowPoiDetailAsync(Models.PointOfInterest poi)
    {
        var detail = new PoiDetailPage();
        detail.SetPoi(poi, (BindingContext as MainViewModel)?.CurrentLang ?? "vi");
        await Navigation.PushModalAsync(detail);
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