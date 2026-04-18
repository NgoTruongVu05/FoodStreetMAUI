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
        private readonly UiTextService _uiTextService;
        private readonly string _languageCode;
        private readonly Label _hintLabel;
        private readonly Button _closeButton;

        public QrScanPage(string? languageCode = null)
        {
            _uiTextService = new UiTextService();
            _languageCode = string.IsNullOrWhiteSpace(languageCode)
                ? (System.Globalization.CultureInfo.CurrentUICulture.Name ?? "vi")
                : languageCode;

            Title = "";
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

            _closeButton = new Button
            {
                Text = "",
                Margin = new Thickness(0, 12, 0, 0),
                BackgroundColor = Color.FromArgb("#2A2A3A"),
                TextColor = Color.FromArgb("#CCCCCC"),
                CornerRadius = 20,
                HeightRequest = 44
            };
            _closeButton.Clicked += OnCloseClicked;

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

            _hintLabel = new Label
            {
                Text = "",
                FontSize = 14,
                TextColor = Color.FromArgb("#E0E0F0"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 8, 0, 12)
            };

            root.Add(_hintLabel);

            root.Add(frame);
            Grid.SetRow(frame, 1);

            root.Add(_closeButton);
            Grid.SetRow(_closeButton, 2);

            Content = root;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                var t = await _uiTextService.LoadOrCreateQrScanPageTextsAsync(_languageCode);
                Title = t.ScanTitle;
                _hintLabel.Text = t.ScanHint;
                _closeButton.Text = t.CloseButton;
            }
            catch
            {
                Title = "Quét QR";
                _hintLabel.Text = "Đưa mã QR vào khung để quét";
                _closeButton.Text = "Đóng";
            }

            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status != PermissionStatus.Granted)
            {
                string title;
                string message;
                try
                {
                    var t = await _uiTextService.LoadOrCreateQrScanPageTextsAsync(_languageCode);
                    title = t.CameraPermissionTitle;
                    message = t.CameraPermissionMessage;
                }
                catch
                {
                    title = "Quyền camera";
                    message = "Bạn cần cấp quyền camera để quét QR.";
                }

                await DisplayAlert(title, message, "OK");
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
