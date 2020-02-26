using Plugin.Geolocator.Abstractions;
using TagRides.Shared.Geo;

namespace TagRides
{
    public static class GeolocatorExtensions
    {
        /// <summary>
        /// Converts a Geolocator Position object to a GeoCoordinates object.
        /// </summary>
        /// <returns>The geo coordinates.</returns>
        /// <param name="pos">Position.</param>
        public static GeoCoordinates ToGeoCoordinates(this Position pos)
        {
            return new GeoCoordinates(pos.Latitude, pos.Longitude);
        }

        public static TK.CustomMap.Position ToTKPosition(this GeoCoordinates coords)
        {
            return new TK.CustomMap.Position(coords.Latitude, coords.Longitude);
        }
    }
}