namespace FoodStreetMAUI.Services
{
    public partial class GpsService
    {
        public void TeleportTo(double lat, double lng)
        {
            var loc = new Models.GpsCoordinate(lat, lng, 3.0);
            PushLocation(loc);
        }
    }
}
