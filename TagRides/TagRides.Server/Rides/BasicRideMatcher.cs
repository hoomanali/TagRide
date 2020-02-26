using System.Linq;
using System.Collections.Generic;
using TagRides.Shared.Geo;
using TagRides.Shared.RideData;
using TagRides.Shared.Utilities;
using System.Threading.Tasks;
using TagRides.Server.Centers;
using TagRides.Server.Requests;
using TagRides.Shared.UserProfile;

namespace TagRides.Server.Rides
{
    using RequestElement = ConcurrentGeoQuadtree<PendingRideRequestCenter.MatchableRideRequest>.IElement;

    /// <summary>
    /// A dumb implementation of ride matching.
    /// </summary>
    public class BasicRideMatcher : IRideMatcher
    {
        public async Task<(IList<PendingRideRequestCenter.MatchableRideRequest>, RideInfo)> GetRide(
            UserRideOffer offer, 
            ConcurrentGeoQuadtree<PendingRideRequestCenter.MatchableRideRequest> origins, 
            ConcurrentGeoQuadtree<PendingRideRequestCenter.MatchableRideRequest> destinations)
        {
            if (origins.Count == 0)
            {
                return (
                    new List<PendingRideRequestCenter.MatchableRideRequest>(), 
                    new RideInfo(offer.User.UserInfo.UserId, offer.RideOffer.Car, new Route(new Route.Stop[]
                    {
                        new Route.Stop(offer.RideOffer.Trip.Source),
                        new Route.Stop(offer.RideOffer.Trip.Destination)
                    }))
                    );
            }

            RequestElement element = origins.GetElementsInside((r) => true).First();
            UserInfo passenger = element.Data.Request.User.UserInfo;
            GeoCoordinates passengerPickUp = element.Coordinates;
            GeoCoordinates passengerDropOff = element.Data.DestinationElement.Coordinates;

            return (
                new List<PendingRideRequestCenter.MatchableRideRequest>
                {
                    element.Data
                },
                new RideInfo(offer.User.UserInfo.UserId, offer.RideOffer.Car, new Route(new Route.Stop[]
                {
                    new Route.Stop(offer.RideOffer.Trip.Source),
                    new Route.Stop(passengerPickUp, passenger, true),
                    new Route.Stop(passengerDropOff, passenger),
                    new Route.Stop(offer.RideOffer.Trip.Destination)
                }))
                );
        }

        public Task<RideInfo> MakeBestRide(UserRideOffer offer, IEnumerable<UserRideRequest> requestsToMatch)
        {
            Route.Stop[] stops = new Route.Stop[requestsToMatch.Count() * 2 + 2];

            stops[0] = new Route.Stop(offer.RideOffer.Trip.Source);

            int i = 1;
            foreach (UserRideRequest r in requestsToMatch)
            {
                stops[i++] = new Route.Stop(r.RideRequest.Trip.Source, r.User.UserInfo, true);
                stops[i++] = new Route.Stop(r.RideRequest.Trip.Destination, r.User.UserInfo, false);
            }

            stops[i] = new Route.Stop(offer.RideOffer.Trip.Destination);

            return Task.FromResult(new RideInfo(offer.User.UserInfo.UserId, offer.RideOffer.Car, new Route(stops)));
        }
    }
}
