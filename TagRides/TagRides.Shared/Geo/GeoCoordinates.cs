using System;
using Newtonsoft.Json;
using TagRides.Shared.Utilities;

namespace TagRides.Shared.Geo
{
    /// <summary>
    /// Simple container for lat/long pairs.
    /// </summary>
    [JsonObject]
    public readonly struct GeoCoordinates
    {
        /// <summary>
        /// The latitude, in WGS84 coordinates. Range -90 to 90.
        /// </summary>
        [JsonProperty]
        public readonly double Latitude;

        /// <summary>
        /// The longitude, in WGS84 coordinates. Range -180 to 180.
        /// </summary>
        [JsonProperty]
        public readonly double Longitude;

        public GeoCoordinates(double latitude, double longitude)
        {
            Latitude = MathUtils.ModRange(latitude, -90, 90);
            Longitude = MathUtils.ModRange(longitude, -180, 180);
        }

        /// <summary>
        /// Given a small distance in units of sqrt(lat^2 + lng^2) (where lat
        /// and lng are in degrees) returns an upper bound to what this distance
        /// could be in meters at this point on Earth.
        /// </summary>
        public double MetersUpperBound(double latLngDist)
        {
            double latLngDistRad = latLngDist * DegToRad;
            return 1000 * EarthRadiusKmUpper * latLngDistRad;
        }

        /// <summary>
        /// Given a small distance in units of sqrt(lat^2 + lng^2) (where lat
        /// and lng are in degrees) returns an upper bound to what this distance
        /// could be in meters at this point on Earth.
        /// </summary>
        public double MetersLowerBound(double latLngDist)
        {
            double latLngDistRad = latLngDist * DegToRad;
            return 1000 * EarthRadiusKmLower * latLngDistRad * Math.Cos(Latitude * DegToRad);
        }

        /// <summary>
        /// Given a small distance in meters returns the radius of a circle on
        /// the map (in lat/lng coordinates) that contains all points within
        /// this distance of these coordinates.
        /// </summary>
        public double DegreesUpperBound(double meters)
        {
            return RadToDeg * meters / (1000 * EarthRadiusKmLower * Math.Cos(Latitude * DegToRad));
        }

        /// <summary>
        /// Given a small distance in meters returns the radius of a circle on
        /// the map (in lat/lng coordinates) such that all points farther than
        /// the given distance from these coordinates are outside it.
        /// </summary>
        public double DegreesLowerBound(double meters)
        {
            return RadToDeg * meters / (1000 * EarthRadiusKmUpper);
        }

        // Visual Studio warns that GeoCoordinates overrides Equals() but does not
        // override GetHashCode().
        public override int GetHashCode()
        {
            throw new NotSupportedException("Hash code is not supported for GeoCoordinates.");
        }

        public override bool Equals(object obj)
        {
            if (obj is GeoCoordinates coords)
                return CoordsApproxEqual(Latitude, coords.Latitude)
                    && CoordsApproxEqual(Longitude, coords.Longitude);
            return false;
        }

        public override string ToString()
        {
            return $"(lat: {Latitude}, long: {Longitude})";
        }

        static bool CoordsApproxEqual(double a, double b)
        {
            // In WGS84 coordinates, this is accurate down to about a centimeter.
            return Math.Abs(a - b) < 1e-7;
        }

        /// <summary>
        /// Upper bound for the Earth's radius. Approximately the radius near
        /// the equator.
        /// </summary>
        const double EarthRadiusKmUpper = 6378;

        /// <summary>
        /// Lower bound for the Earth's radius. Approximately the radius at
        /// the poles.
        /// </summary>
        const double EarthRadiusKmLower = 6357;

        const double DegToRad = Math.PI / 180;
        const double RadToDeg = 180 / Math.PI;
    }
}
