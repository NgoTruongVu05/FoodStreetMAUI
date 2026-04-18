using Android.App;
using Android.Content;
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
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "poiapp",
        AutoVerify = false
    )]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);

                TryHandleIntent(Intent);
            }
            catch (System.Exception ex)
            {
                Log.Error("FoodStreet", "OnCreate crash: " + ex.ToString());
                throw;
            }
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            TryHandleIntent(intent);
        }

        private static void TryHandleIntent(Intent? intent)
        {
            try
            {
                var data = intent?.Data;
                if (data == null)
                {
                    return;
                }

                FoodStreetMAUI.Services.DeepLinkDispatcher.Dispatch(new System.Uri(data.ToString()!));
            }
            catch (System.Exception ex)
            {
                Log.Error("FoodStreet", "Deep link handle error: " + ex);
            }
        }
    }
}
