using System;
using System.Threading.Tasks;
using FoodStreetMAUI.Models;
using FoodStreetMAUI.Services;
using FoodStreetMAUI.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace FoodStreetMAUI.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _vm;
        private readonly MapDrawable _mapDrawable;
        private System.Timers.Timer? _mapTimer;

        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;

            _mapDrawable = new MapDrawable(vm);
            mapView.Drawable = _mapDrawable;

            // Refresh map every 500ms
            _mapTimer = new System.Timers.Timer(500);
            _mapTimer.Elapsed += (s, e) =>
                MainThread.BeginInvokeOnMainThread(() => mapView.Invalidate());
            _mapTimer.Start();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadDataCommand.ExecuteAsync(null);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _mapTimer?.Stop();
        }
    }

    // ── Map Drawable ──────────────────────────────────────────────────────────
    public class MapDrawable : IDrawable
    {
        private readonly MainViewModel _vm;
        private float _userX = -1, _userY = -1;

        // Bounding box around Bùi Viện
        private const double MinLat = 10.7675, MaxLat = 10.7735;
        private const double MinLng = 106.6910, MaxLng = 106.6990;

        public MapDrawable(MainViewModel vm) => _vm = vm;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float w = dirtyRect.Width, h = dirtyRect.Height;
            if (w < 10 || h < 10) return;

            // Background
            canvas.FillColor = Color.FromArgb("#111820");
            canvas.FillRectangle(dirtyRect);

            // Grid
            canvas.StrokeColor = Color.FromArgb("#1E2D3D");
            canvas.StrokeSize = 0.5f;
            for (int i = 0; i <= 8; i++)
            {
                float x = 20 + i * (w - 40) / 8f;
                canvas.DrawLine(x, 20, x, h - 20);
            }
            for (int i = 0; i <= 6; i++)
            {
                float y = 20 + i * (h - 50) / 6f;
                canvas.DrawLine(20, y, w - 20, y);
            }

            // Simulated roads
            canvas.StrokeColor = Color.FromArgb("#1E3040");
            canvas.StrokeSize = 9f;
            var r1s = ToScreen(10.7700, 106.6915, w, h);
            var r1e = ToScreen(10.7700, 106.6988, w, h);
            canvas.DrawLine(r1s.X, r1s.Y, r1e.X, r1e.Y);
            canvas.StrokeSize = 6f;
            var r2s = ToScreen(10.7678, 106.6935, w, h);
            var r2e = ToScreen(10.7738, 106.6935, w, h);
            canvas.DrawLine(r2s.X, r2s.Y, r2e.X, r2e.Y);

            // POIs
            foreach (var poi in _vm.Pois)
            {
                var sc = ToScreen(poi.Location.Latitude, poi.Location.Longitude, w, h);
                float lngRange = (float)(MaxLng - MinLng);
                float approachPx = (float)(poi.ApproachRadius / 111320.0 / lngRange * (w - 40));
                float trigPx = (float)(poi.TriggerRadius / 111320.0 / lngRange * (w - 40));

                // Approach ring
                var approachFill = poi.Status == PoiStatus.Approaching
                    ? Color.FromRgba(240, 160, 48, 20)
                    : Color.FromRgba(80, 128, 192, 12);
                canvas.FillColor = approachFill;
                canvas.FillCircle(sc.X, sc.Y, approachPx);
                canvas.StrokeColor = poi.Status == PoiStatus.Approaching
                    ? Color.FromRgba(240, 160, 48, 80)
                    : Color.FromRgba(80, 128, 192, 40);
                canvas.StrokeSize = 0.8f;
                canvas.DrawCircle(sc.X, sc.Y, approachPx);

                // Trigger ring
                var trigFill = poi.Status == PoiStatus.Active
                    ? Color.FromRgba(58, 219, 118, 40)
                    : Color.FromRgba(80, 128, 192, 20);
                canvas.FillColor = trigFill;
                canvas.FillCircle(sc.X, sc.Y, trigPx);
                canvas.StrokeColor = poi.Status == PoiStatus.Active
                    ? Color.FromRgba(58, 219, 118, 150)
                    : Color.FromRgba(80, 128, 192, 60);
                canvas.StrokeSize = 1f;
                canvas.DrawCircle(sc.X, sc.Y, trigPx);

                // Dot
                var dotColor = poi.Status switch
                {
                    PoiStatus.Active => Color.FromArgb("#3ADB76"),
                    PoiStatus.Approaching => Color.FromArgb("#F0A030"),
                    _ => Color.FromArgb("#5080C0")
                };
                canvas.FillColor = dotColor;
                canvas.FillCircle(sc.X, sc.Y, 7f);
                canvas.StrokeColor = Color.FromRgba(255, 255, 255, 40);
                canvas.StrokeSize = 1f;
                canvas.DrawCircle(sc.X, sc.Y, 7f);

                // Label
                canvas.FontColor = dotColor;
                canvas.FontSize = 11f;
                canvas.DrawString($"{poi.Emoji} {poi.Name}", sc.X + 11, sc.Y - 5,
                    HorizontalAlignment.Left);
            }

            // User location
            if (_vm.CoordinateText != "---")
            {
                try
                {
                    var parts = _vm.CoordinateText.Split(',');
                    if (parts.Length == 2
                        && double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out double lat)
                        && double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out double lng))
                    {
                        var target = ToScreen(lat, lng, w, h);
                        // Smooth lerp
                        if (_userX < 0) { _userX = target.X; _userY = target.Y; }
                        _userX += (target.X - _userX) * 0.12f;
                        _userY += (target.Y - _userY) * 0.12f;

                        // Glow
                        canvas.FillColor = Color.FromRgba(0, 180, 255, 30);
                        canvas.FillCircle(_userX, _userY, 20f);
                        // Outer ring
                        canvas.StrokeColor = Color.FromRgba(0, 180, 255, 80);
                        canvas.StrokeSize = 1.5f;
                        canvas.DrawCircle(_userX, _userY, 14f);
                        // Inner dot
                        canvas.FillColor = Color.FromArgb("#00B4FF");
                        canvas.FillCircle(_userX, _userY, 7f);
                        canvas.StrokeColor = Colors.White;
                        canvas.StrokeSize = 1.5f;
                        canvas.DrawCircle(_userX, _userY, 7f);
                        // Label
                        canvas.FontColor = Color.FromArgb("#00B4FF");
                        canvas.FontSize = 10f;
                        canvas.DrawString("BẠN", _userX + 10, _userY - 5,
                            HorizontalAlignment.Left);
                    }
                }
                catch { }
            }

            // Legend
            canvas.FillColor = Color.FromRgba(13, 13, 22, 180);
            canvas.FillRoundedRectangle(w - 120, h - 65, 112, 58, 8);
            float ly = h - 58;
            DrawLegendDot(canvas, w - 112, ly, "#3ADB76", "Đang ở đây"); ly += 18;
            DrawLegendDot(canvas, w - 112, ly, "#F0A030", "Đang đến gần"); ly += 18;
            DrawLegendDot(canvas, w - 112, ly, "#5080C0", "Chưa kích hoạt");
        }

        private static void DrawLegendDot(ICanvas canvas, float x, float y,
                                           string hex, string label)
        {
            canvas.FillColor = Color.FromArgb(hex);
            canvas.FillCircle(x + 5, y + 4, 4f);
            canvas.FontColor = Color.FromArgb("#888888");
            canvas.FontSize = 9f;
            canvas.DrawString(label, x + 14, y, HorizontalAlignment.Left);
        }

        private static PointF ToScreen(double lat, double lng, float w, float h)
        {
            float x = (float)((lng - MinLng) / (MaxLng - MinLng) * (w - 40)) + 20;
            float y = (float)((1 - (lat - MinLat) / (MaxLat - MinLat)) * (h - 50)) + 25;
            return new PointF(x, y);
        }
    }
}
