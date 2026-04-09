using FoodStreetMAUI.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using FoodStreetMAUI.Models;
using System.Collections.Generic;
using System.Linq;
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

        private async void OnViewDetailsClicked(object sender, EventArgs e)
        {
            if (_vm.SelectedPoi == null) return;

            // Navigate to PoiListPage and ask it to show the detail modal for the selected POI
            var page = new PoiListPage(_vm);
            await Navigation.PushAsync(page);

            // Allow page to initialize then show detail
            await System.Threading.Tasks.Task.Delay(120);
            await page.ShowPoiDetailAsync(_vm.SelectedPoi);
        }

        private void SetupMap()
        {
            _map = new MapsuiMap();
            _map.Layers.Add(OpenStreetMap.CreateTileLayer());

            _markerLayer = new MemoryLayer { Name = "PoiUserMarkers" };
            TryEnableMapInfoLayer(_markerLayer);
            _map.Layers.Add(_markerLayer);

            _mapControl.Map = _map;
            _mapControl.Info += OnMapInfo;
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnMapTapped;
            _mapControl.GestureRecognizers.Add(tapGesture);
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

        private void OnCenterMapClicked(object sender, EventArgs e)
        {
            if (_map?.Navigator == null) return;

            var center = GetCenterMPoint();
            if (center == null) return;

            var idx = System.Math.Min(15, _map.Navigator.Resolutions.Count - 1);
            _map.Navigator.CenterOnAndZoomTo(center, _map.Navigator.Resolutions[idx], duration: 250);

            // If centering on user, mark as centered so tracking won't re-center repeatedly
            if (_vm.LastLatitude is double && _vm.LastLongitude is double)
            {
                _hasCenteredOnUser = true;
            }
        }

        private void OnPlaySelectedPoiAudioClicked(object sender, EventArgs e)
        {
            if (_vm.SelectedPoi != null)
            {
                _vm.PlayPoiAudio(_vm.SelectedPoi);
            }
        }

        private void OnLanguagePickerChanged(object sender, EventArgs e)
        {
            if (sender is not Picker picker || picker.SelectedItem is not LanguageItem language)
            {
                return;
            }

            _vm.SelectLanguageCommand.Execute(language.Code);
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

            // Determine nearest POI relative to last known user location (independent of trigger status)
            Guid? nearestPoiId = null;
            if (_vm.LastLatitude is double ula && _vm.LastLongitude is double ulo)
            {
                var nearest = _vm.Pois
                    .Select(p => (poi: p, d: (p.Location.Latitude - ula) * (p.Location.Latitude - ula) + (p.Location.Longitude - ulo) * (p.Location.Longitude - ulo)))
                    .OrderBy(x => x.d)
                    .FirstOrDefault();

                if (nearest.poi != null)
                    nearestPoiId = nearest.poi.Id;
            }
            else if (_vm.NearestPoiId.HasValue)
            {
                nearestPoiId = _vm.NearestPoiId.Value;
            }

            foreach (var poi in _vm.Pois)
            {
                var (px, py) = SphericalMercator.FromLonLat(poi.Location.Longitude, poi.Location.Latitude);

                // Color logic: red = nearest to GPS, yellow = approaching/active, green = normal
                MColor fillColor;
                bool isNearest = nearestPoiId.HasValue && poi.Id == nearestPoiId.Value;
                if (isNearest)
                {
                    fillColor = MColor.Red;
                }
                else if (poi.Status == PoiStatus.Active || poi.Status == PoiStatus.Approaching)
                {
                    fillColor = MColor.Yellow;
                }
                else
                {
                    fillColor = MColor.Green;
                }

                var f = new PointFeature(new MPoint(px, py)) { Styles = { PoiSymbolStyle(fillColor, isNearest) } };
                f["PoiId"] = poi.Id; // attach id
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

        private static IStyle PoiSymbolStyle(MColor fillColor, bool nearest)
        {
            return new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse, // hình tròn
                SymbolScale = nearest ? 1.3f : 0.9f,

                Fill = new MBrush
                {
                    Color = fillColor
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

        private void OnMapInfo(object? sender, MapInfoEventArgs e)
        {
            var feature = TryGetFeatureFromMapInfo(e);
            if (feature == null)
            {
                var worldPosition = TryGetWorldPositionFromMapInfo(e);
                if (worldPosition == null)
                {
                    return;
                }

                if (!TryGetPoiByWorldPosition(worldPosition, out var poiFromWorld))
                {
                    return;
                }

                ShowPoiModal(poiFromWorld);
                return;
            }

            var poiId = feature["PoiId"];
            if (poiId == null)
            {
                return;
            }

            if (!TryGetPoiById(poiId, out var poi))
            {
                return;
            }

            ShowPoiModal(poi);
        }

        private void OnMapTapped(object? sender, TappedEventArgs e)
        {
            var position = e.GetPosition(_mapControl);
            if (position == null)
            {
                return;
            }

            var viewport = _map?.Navigator?.Viewport;
            if (viewport == null)
            {
                return;
            }

            var worldPosition = ScreenToWorld(viewport.Value, position.Value.X, position.Value.Y);

            if (!TryGetPoiByWorldPosition(worldPosition, out var poi))
            {
                return;
            }

            ShowPoiModal(poi);
        }

        private void ShowPoiModal(Models.PointOfInterest poi)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var content = poi.GetContent(_vm.CurrentLang);
                _vm.SelectedPoi = poi;
                _vm.SelectedPoiTitle = string.IsNullOrWhiteSpace(content?.Title)
                    ? poi.DisplayName
                    : $"{poi.Emoji} {content!.Title}";
                _vm.SelectedPoiDesc = string.IsNullOrWhiteSpace(content?.Description)
                    ? string.Empty
                    : content!.Description;
                _vm.IsPoiModalVisible = true;
            });
        }

        private static MPoint ScreenToWorld(Viewport viewport, double screenX, double screenY)
        {
            var worldX = viewport.CenterX + (screenX - viewport.Width / 2) * viewport.Resolution;
            var worldY = viewport.CenterY - (screenY - viewport.Height / 2) * viewport.Resolution;
            return new MPoint(worldX, worldY);
        }

        private static IFeature? TryGetFeatureFromMapInfo(MapInfoEventArgs e)
        {
            var featureProperty = e.GetType().GetProperty("Feature");
            if (featureProperty?.GetValue(e) is IFeature directFeature)
            {
                return directFeature;
            }

            var mapInfoProperty = e.GetType().GetProperty("MapInfo");
            var mapInfo = mapInfoProperty?.GetValue(e);
            if (mapInfo == null)
            {
                return null;
            }

            var mapInfoFeatureProperty = mapInfo.GetType().GetProperty("Feature");
            return mapInfoFeatureProperty?.GetValue(mapInfo) as IFeature;
        }

        private static MPoint? TryGetWorldPositionFromMapInfo(MapInfoEventArgs e)
        {
            var worldPositionProperty = e.GetType().GetProperty("WorldPosition");
            var directWorldValue = worldPositionProperty?.GetValue(e);
            if (directWorldValue is MPoint directWorld)
            {
                return directWorld;
            }

            var mapInfoProperty = e.GetType().GetProperty("MapInfo");
            var mapInfo = mapInfoProperty?.GetValue(e);
            if (mapInfo == null)
            {
                return null;
            }

            var mapInfoWorldProperty = mapInfo.GetType().GetProperty("WorldPosition");
            var mapInfoWorldValue = mapInfoWorldProperty?.GetValue(mapInfo);
            if (mapInfoWorldValue is MPoint mapInfoWorld)
            {
                return mapInfoWorld;
            }

            return null;
        }

        private bool TryGetPoiById(object poiId, out Models.PointOfInterest poi)
        {
            if (poiId is Guid guid)
            {
                poi = _vm.Pois.FirstOrDefault(p => p.Id == guid);
                return poi != null;
            }

            if (Guid.TryParse(poiId.ToString(), out var parsed))
            {
                poi = _vm.Pois.FirstOrDefault(p => p.Id == parsed);
                return poi != null;
            }

            poi = null!;
            return false;
        }

        private bool TryGetPoiByWorldPosition(MPoint worldPosition, out Models.PointOfInterest poi)
        {
            var resolution = _map?.Navigator != null ? _map.Navigator.Viewport.Resolution : 0;
            var tolerance = resolution > 0 ? resolution * 20 : 50;

            Models.PointOfInterest? nearestPoi = null;
            var nearestDistance = double.MaxValue;

            foreach (var p in _vm.Pois)
            {
                var (x, y) = SphericalMercator.FromLonLat(p.Location.Longitude, p.Location.Latitude);
                var dist = Distance(worldPosition, new MPoint(x, y));
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestPoi = p;
                }
            }

            if (nearestPoi == null || nearestDistance > tolerance)
            {
                poi = null!;
                return false;
            }

            poi = nearestPoi;
            return true;
        }

        private static double Distance(MPoint a, MPoint b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }

        private static void TryEnableMapInfoLayer(ILayer layer)
        {
            var mapInfoProperty = layer.GetType().GetProperty("IsMapInfoLayer");
            if (mapInfoProperty?.CanWrite == true)
            {
                mapInfoProperty.SetValue(layer, true);
            }
        }

        public void Dispose()
        {
            _vm.NearestPoiOrLocationChanged -= _onNearestOrPoiChanged;
            if (_mapControl != null)
            {
                _mapControl.Info -= OnMapInfo;
                _mapControl.Dispose();
            }
        }
    }
}
