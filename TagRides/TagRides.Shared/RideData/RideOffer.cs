using TagRides.Shared.UserProfile;
using Newtonsoft.Json;

namespace TagRides.Shared.RideData
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RideOffer : RequestBase
    {
        [JsonProperty]
        public readonly Trip Trip;
        /// <summary>
        /// In minutes
        /// </summary>
        [JsonProperty]
        public readonly double MaxTimeOutOfWay;
        [JsonProperty]
        public readonly CarInfo Car;
        [JsonProperty]
        public readonly int AvailableSeats;

        public RideOffer(Trip trip, double maxTimeOutOfWay, CarInfo car, int availableSeats, RequestGameElements gameElements)
            : base(gameElements)
        {
            Trip = trip;
            MaxTimeOutOfWay = maxTimeOutOfWay;
            Car = car;
            AvailableSeats = availableSeats;
        }
    }
}
