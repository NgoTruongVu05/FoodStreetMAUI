using FoodStreetMAUI.ViewModels;
using Microsoft.Maui.Controls;

namespace FoodStreetMAUI.Views;

public partial class PoiListPage : ContentPage
{
    private MainViewModel? _vm;

    public PoiListPage(MainViewModel? vm = null)
    {
        InitializeComponent();
        _vm = vm;
        if (vm != null)
        {
            BindingContext = vm;
            // Cập nhật page title từ UiTexts
            Title = vm.UiTexts.PoiListPageTitle;
            // Subscribe để cập nhật title khi UiTexts thay đổi
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.UiTexts))
                {
                    Title = vm.UiTexts.PoiListPageTitle;
                }
            };
        }
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
        //Gán nội dung POI vào POIDetail và Set biến isPoiModalVisible = true để hiển thị modal
        var vm = BindingContext as MainViewModel;
        var detail = new PoiDetailPage(vm);
        detail.SetPoi(poi, (BindingContext as MainViewModel)?.CurrentLang ?? "vi");
        await Navigation.PushModalAsync(detail);
    }

    private void OnPlayAudioTapped(object sender, EventArgs e)
    {
        // Lấy thông tin Poi được click
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

        // Khi quay lại MainPage (PopAsync), không ép dừng audio để tránh hiệu ứng phụ
        // (ví dụ: reset trạng thái UI/ngôn ngữ do các handler liên quan audio).
        // Chỉ dừng audio khi trang thật sự bị remove khỏi stack hoặc app chuyển trạng thái.
        if (BindingContext is MainViewModel vm && Navigation?.NavigationStack?.Contains(this) == false)
        {
            vm.StopAudio();
        }
    }
}