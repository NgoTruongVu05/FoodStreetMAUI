using System;

namespace FoodStreetMAUI.Services
{
    public static class DeepLinkDispatcher
    {
        public static event EventHandler<Uri>? UriReceived;

        public static void Dispatch(Uri uri)
        {
            try
            {
                UriReceived?.Invoke(null, uri);
            }
            catch
            {
            }
        }
    }
}
