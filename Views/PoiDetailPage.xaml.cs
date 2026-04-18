using FoodStreetMAUI.Models;
using FoodStreetMAUI.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Globalization;

namespace FoodStreetMAUI.Views
{
    public partial class PoiDetailPage : ContentPage
    {
        private PointOfInterest? _poi;
        private MainViewModel? _vm;

        public PoiDetailPage(MainViewModel? vm = null)
        {
            InitializeComponent();
            _vm = vm;
            if (vm != null)
            {
                BindingContext = vm;
            }
        }

        public void SetPoi(PointOfInterest poi, string lang)
        {
            _poi = poi;
            var content = poi.GetContent(lang);
            TitleLabel.Text = string.IsNullOrWhiteSpace(content?.Title) ? poi.DisplayName : $"{poi.Emoji} {content!.Title}";
            DescLabel.Text = string.IsNullOrWhiteSpace(content?.Description) ? string.Empty : content!.Description;

            if (!string.IsNullOrWhiteSpace(poi.ImageUrl))
            {
                PoiImage.Source = poi.ImageUrl;
                PoiImage.IsVisible = true;
                NoImageLabel.IsVisible = false;
            }
            else
            {
                PoiImage.IsVisible = false;
                NoImageLabel.IsVisible = true;
            }
        }

        private async void OnOpenMapsClicked(object sender, EventArgs e)
        {
            if (_poi == null) return;

            var url = !string.IsNullOrWhiteSpace(_poi.MapLink)
                ? _poi.MapLink!
                : $"https://www.google.com/maps?q={_poi.Location.Latitude.ToString(CultureInfo.InvariantCulture)},{_poi.Location.Longitude.ToString(CultureInfo.InvariantCulture)}";

            try
            {
                await Launcher.OpenAsync(url);
            }
            catch { }
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
