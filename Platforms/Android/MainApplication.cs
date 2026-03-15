using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using System;
using Log = Android.Util.Log;

namespace FoodStreetMAUI.Platforms.Android
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Log.Error("FoodStreet", "UnhandledException: " + e.ExceptionObject?.ToString());
            };
        }

        protected override MauiApp CreateMauiApp()
        {
            try
            {
                return MauiProgram.CreateMauiApp();
            }
            catch (Exception ex)
            {
                Log.Error("FoodStreet", "CreateMauiApp crash: " + ex.ToString());
                throw;
            }
        }
    }
}
