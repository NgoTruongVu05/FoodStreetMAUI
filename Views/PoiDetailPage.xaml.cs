using FoodStreetMAUI.Models;
using FoodStreetMAUI.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;

namespace FoodStreetMAUI.Views
{
    public partial class PoiDetailPage : ContentPage
    {
        private PointOfInterest? _poi;

        public PoiDetailPage()
        {
            InitializeComponent();
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
            var lat = _poi.Location.Latitude;
            var lng = _poi.Location.Longitude;
            var url = $"https://www.google.com/maps?q={lat},{lng}";
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
