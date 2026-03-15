using Android.App;
using Android.OS;
using Android.Runtime;
using Microsoft.Maui;
using Log = Android.Util.Log;

namespace FoodStreetMAUI.Platforms.Android
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        ConfigurationChanges =
            global::Android.Content.PM.ConfigChanges.ScreenSize |
            global::Android.Content.PM.ConfigChanges.Orientation |
            global::Android.Content.PM.ConfigChanges.UiMode |
            global::Android.Content.PM.ConfigChanges.ScreenLayout |
            global::Android.Content.PM.ConfigChanges.SmallestScreenSize |
            global::Android.Content.PM.ConfigChanges.Density
    )]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);
            }
            catch (System.Exception ex)
            {
                Log.Error("FoodStreet", "OnCreate crash: " + ex.ToString());
                throw;
            }
        }
    }
}
