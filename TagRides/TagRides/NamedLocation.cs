using System;
using TagRides.Shared.Geo;

namespace TagRides
{
    /// <summary>
    /// Geocoordinates with a label.
    /// </summary>
    public class NamedLocation
    {
        public string Name { get; }
        public GeoCoordinates Coordinates { get; }

        public NamedLocation(string name, GeoCoordinates coordinates)
        {
            Name = name;
            Coordinates = coordinates;
        }
    }
}
