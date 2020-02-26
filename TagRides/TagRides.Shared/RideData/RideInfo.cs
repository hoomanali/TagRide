using System;
using System.Collections.Generic;
using System.Text;
using TagRides.Shared.UserProfile;

using Newtonsoft.Json;

namespace TagRides.Shared.RideData
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct RideInfo
    {
        [JsonProperty]
        public readonly string DriverId;
        [JsonProperty]
        public readonly CarInfo Car;
        [JsonProperty]
        public readonly Route Route;

        public RideInfo(string driverId, CarInfo car, Route route)
        {
            DriverId = driverId;
            Car = car;
            Route = route;

            //TODO we are sharing the raw UserInfo of the driver with everyone.
            //     we should add some level of info protection
        }
    }
}
