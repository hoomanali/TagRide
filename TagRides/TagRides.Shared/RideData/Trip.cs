using System;
using TagRides.Shared.Geo;

namespace TagRides.Shared.RideData
{
    public class Trip
    {
        public DateTime DepartureTime { get; }
        public GeoCoordinates Source { get; }
        public GeoCoordinates Destination { get; }

        public Trip(DateTime departureTime, GeoCoordinates source, GeoCoordinates destination)
        {
            DepartureTime = departureTime;
            Source = source;
            Destination = destination;
        }
    }
}
