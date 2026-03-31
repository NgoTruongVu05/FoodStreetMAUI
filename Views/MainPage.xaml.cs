using FoodStreetMAUI.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using MapsuiMap = Mapsui.Map;
using MBrush = Mapsui.Styles.Brush;
using MColor = Mapsui.Styles.Color;

namespace FoodStreetMAUI.Views
{
    public partial class MainPage : ContentPage, IDisposable
    {
        private readonly MainViewModel _vm;
        private readonly MapControl _mapControl;
        private readonly EventHandler _onNearestOrPoiChanged;
        private MapsuiMap? _map;
        private MemoryLayer? _markerLayer;
        private bool _viewportInitialized;
        private bool _hasCenteredOnUser;

        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;

            _onNearestOrPoiChanged = (_, __) => MainThread.BeginInvokeOnMainThread(RefreshMarkerLayer);

            _mapControl = new MapControl
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
            };
            MapHost.Children.Add(_mapControl);

            SetupMap();

            _vm.NearestPoiOrLocationChanged += _onNearestOrPoiChanged;
        }

        private void SetupMap()
        {
            _map = new MapsuiMap();
            _map.Layers.Add(OpenStreetMap.CreateTileLayer());

            _markerLayer = new MemoryLayer { Name = "PoiUserMarkers" };
            _map.Layers.Add(_markerLayer);

            _mapControl.Map = _map;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadDataCommand.ExecuteAsync(null);
            RefreshMarkerLayer();
        }

        private async void OnOpenPoiListClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PoiListPage(_vm));
        }

        private MPoint? GetCenterMPoint()
        {
            if (_vm.LastLatitude is double la && _vm.LastLongitude is double lo)
            {
                var (x, y) = SphericalMercator.FromLonLat(lo, la);
                return new MPoint(x, y);
            }
            if (_vm.Pois.Count > 0)
            {
                var p = _vm.Pois[0];
                var (x, y) = SphericalMercator.FromLonLat(p.Location.Longitude, p.Location.Latitude);
                return new MPoint(x, y);
            }
            return null;
        }

        private void RefreshMarkerLayer()
        {
            if (_markerLayer == null) return;

            var list = new List<IFeature>();
            var nearestId = _vm.NearestPoiId;

            foreach (var poi in _vm.Pois)
            {
                var isNearest = nearestId.HasValue && poi.Id == nearestId.Value;
                var (px, py) = SphericalMercator.FromLonLat(poi.Location.Longitude, poi.Location.Latitude);
                var f = new PointFeature(new MPoint(px, py)) { Styles = { PoiSymbolStyle(isNearest) } };
                list.Add(f);
            }

            if (_vm.LastLatitude is double la && _vm.LastLongitude is double lo)
            {
                var (ux, uy) = SphericalMercator.FromLonLat(lo, la);
                list.Add(new PointFeature(new MPoint(ux, uy)) { Styles = { UserSymbolStyle() } });

                if (_vm.IsTracking && !_hasCenteredOnUser && _map?.Navigator != null && _map.Navigator.Resolutions.Count > 0)
                {
                    var i = System.Math.Min(16, _map.Navigator.Resolutions.Count - 1);
                    _map.Navigator.CenterOnAndZoomTo(new MPoint(ux, uy), _map.Navigator.Resolutions[i], duration: 0);
                    _hasCenteredOnUser = true;
                }
            }

            if (!_vm.IsTracking)
            {
                _hasCenteredOnUser = false;
            }

            _markerLayer.Features = list;
            _mapControl.RefreshGraphics();

            // Home có thể chạy trước khi LoadData xong — căn lần đầu khi đã có POI / GPS
            if (!_viewportInitialized && _vm.Pois.Count > 0 && _map?.Navigator != null
                && _map.Navigator.Resolutions.Count > 0)
            {
                var c = GetCenterMPoint();
                if (c != null)
                {
                    var i = System.Math.Min(15, _map.Navigator.Resolutions.Count - 1);
                    _map.Navigator.CenterOnAndZoomTo(c, _map.Navigator.Resolutions[i], duration: 0);
                    _viewportInitialized = true;
                }
            }
        }

        private static IStyle PoiSymbolStyle(bool nearest)
        {
            return new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse, // hình tròn
                SymbolScale = nearest ? 1.3f : 0.9f,

                Fill = new MBrush
                {
                    Color = nearest
                ? MColor.Red     // POI gần nhất
                : MColor.Green   // POI thường
                },

                Outline = new Pen
                {
                    Color = MColor.White,
                    Width = 2
                }
            };
        }

        private static IStyle UserSymbolStyle()
            => new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 1.0f,
                Fill = new MBrush { Color = MColor.Blue },
                Outline = new Pen { Color = MColor.White, Width = 2 },
            };

        public void Dispose()
        {
            _vm.NearestPoiOrLocationChanged -= _onNearestOrPoiChanged;
            _mapControl.Dispose();
        }
    }
}
