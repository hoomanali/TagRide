using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace TagRides.Shared.RideData
{
    /// <summary>
    /// A collection of information neccessary for requesting a ride
    /// Has no association with any network functionality; an
    /// IRideRequester is used to send out the request
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class RideRequest : RequestBase
    {
        [JsonProperty]
        public readonly Trip Trip;

        public RideRequest(Trip trip, RequestGameElements gameElements)
            : base(gameElements)
        {
            Trip = trip;
        }
    }
}
