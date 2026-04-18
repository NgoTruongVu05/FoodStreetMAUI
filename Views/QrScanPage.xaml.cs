using FoodStreetMAUI.Services;
using Microsoft.Maui.ApplicationModel;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace FoodStreetMAUI.Views
{
    public class QrScanPage : ContentPage
    {
        private bool _handled;
        private readonly CameraBarcodeReaderView _qrReader;

        public QrScanPage()
        {
            Title = "Quét QR";
            BackgroundColor = Color.FromArgb("#0F0F16");

            _qrReader = new CameraBarcodeReaderView
            {
                IsDetecting = true
            };
            _qrReader.BarcodesDetected += OnBarcodesDetected;

            var frame = new Border
            {
                Stroke = Color.FromArgb("#F0A030"),
                StrokeThickness = 1,
                Padding = 2,
                Content = _qrReader
            };

            var closeButton = new Button
            {
                Text = "Đóng",
                Margin = new Thickness(0, 12, 0, 0),
                BackgroundColor = Color.FromArgb("#2A2A3A"),
                TextColor = Color.FromArgb("#CCCCCC"),
                CornerRadius = 20,
                HeightRequest = 44
            };
            closeButton.Clicked += OnCloseClicked;

            var root = new Grid
            {
                Padding = 12,
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                }
            };

            root.Add(new Label
            {
                Text = "Đưa mã QR vào khung để quét",
                FontSize = 14,
                TextColor = Color.FromArgb("#E0E0F0"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 8, 0, 12)
            });

            root.Add(frame);
            Grid.SetRow(frame, 1);

            root.Add(closeButton);
            Grid.SetRow(closeButton, 2);

            Content = root;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Quyền camera", "Bạn cần cấp quyền camera để quét QR.", "OK");
                await Navigation.PopAsync();
                return;
            }

            _qrReader.IsDetecting = true;
        }

        protected override void OnDisappearing()
        {
            _qrReader.IsDetecting = false;
            base.OnDisappearing();
        }

        private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
        {
            if (_handled) return;

            var value = e.Results?.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(value)) return;

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return;
            }

            if (!string.Equals(uri.Scheme, "poiapp", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _handled = true;
            _qrReader.IsDetecting = false;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PopAsync();
                DeepLinkDispatcher.Dispatch(uri);
            });
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
