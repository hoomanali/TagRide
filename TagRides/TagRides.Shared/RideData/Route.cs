using System;
using System.Collections.Generic;
using System.Linq;
using TagRides.Shared.Geo;
using TagRides.Shared.UserProfile;
using Newtonsoft.Json;

namespace TagRides.Shared.RideData
{
    [JsonObject(MemberSerialization.OptIn)]
    public readonly struct Route
    {
        [JsonObject]
        public readonly struct Stop
        {
            [JsonProperty]
            public readonly GeoCoordinates Location;
            [JsonProperty]
            public readonly UserInfo Passenger;
            [JsonProperty]
            public readonly bool IsPickup;

            public Stop(GeoCoordinates location, UserInfo passenger = null, bool isPickup = false)
            {
                Location = location;
                Passenger = passenger;
                IsPickup = isPickup;
            }
        }

        /// <summary>
        /// The stops along the route including the driver's own source and destination.
        /// </summary>
        [JsonProperty]
        public readonly Stop[] Stops;

        /// <summary>
        /// Creates a new route with the given stops.
        /// </summary>
        /// <param name="stops">Stops along the route including the driver's own source and destination.</param>
        public Route(IEnumerable<Stop> stops)
        {
            Stops = stops.ToArray();

            int dropOffs = 0;
            HashSet<UserInfo> passengers = new HashSet<UserInfo>();
            foreach(var stop in Stops)
            {
                if (stop.Passenger != null)
                {
                    if (stop.IsPickup && !passengers.Add(stop.Passenger))
                        throw new Exception("Picking up same passenger twice");
                    else if (!stop.IsPickup && !passengers.Contains(stop.Passenger))
                        throw new Exception("Dropping off passenger before pickup");
                    else if (!stop.IsPickup)
                        ++dropOffs;
                }
            }

            if (dropOffs != passengers.Count)
                throw new Exception("Different amount of dropoffs than passengers.");
        }
        
        public IEnumerable<UserInfo> Passengers =>
            Stops
            .Where((stop) => stop.IsPickup && stop.Passenger != null)
            .Select((stop) => stop.Passenger);
        
        public IEnumerable<(UserInfo, GeoCoordinates)> PickUps =>
            Stops
            .Where((stop) => stop.IsPickup)
            .Select((stop) => (stop.Passenger, stop.Location));
    }
}
