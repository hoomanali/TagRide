using Google.Maps;
using TagRides.Shared.Geo;

namespace TagRides.Server.Utility
{
    public static class GeoCoordinatesUtility
    {
        public static LatLng ToGoogleLocation(this GeoCoordinates coords)
        {
            return new LatLng(coords.Latitude, coords.Longitude);
        }

        public static GeoCoordinates ToGeo(this LatLng location)
        {
            return new GeoCoordinates(location.Latitude, location.Longitude);
        }
    }
}
