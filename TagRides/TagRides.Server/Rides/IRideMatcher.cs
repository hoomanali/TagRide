using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TagRides.Server.Requests;
using TagRides.Shared.Geo;
using TagRides.Shared.RideData;
using TagRides.Server.Centers;

namespace TagRides.Server.Rides
{
    using MatchableRideRequest = PendingRideRequestCenter.MatchableRideRequest;

    public interface IRideMatcher
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="offer"></param>
        /// <param name="origins"></param>
        /// <param name="destinations"></param>
        /// <returns>A list of matchableRideRequests in the order they will be picked up, and a rideInfo of the potential trip</returns>
        Task<(IList<MatchableRideRequest>, RideInfo)> GetRide(UserRideOffer offer, 
                                                              ConcurrentGeoQuadtree<MatchableRideRequest> origins, 
                                                              ConcurrentGeoQuadtree<MatchableRideRequest> destinations);

        /// <summary>
        /// Makes a ride with all <paramref name="requestsToMatch"/> in the pickup order of the list.
        /// </summary>
        /// <param name="requestsToMatch"></param>
        /// <returns></returns>
        Task<RideInfo> MakeBestRide(UserRideOffer offer, IEnumerable<UserRideRequest> requestsToMatch);
    }
}