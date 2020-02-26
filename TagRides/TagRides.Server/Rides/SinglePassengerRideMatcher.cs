using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TagRides.Server.Centers;
using TagRides.Server.Requests;
using TagRides.Shared.Geo;
using TagRides.Shared.RideData;
using TagRides.Shared.Utilities;

namespace TagRides.Server.Rides
{
    using MatchableRideRequest = PendingRideRequestCenter.MatchableRideRequest;

    /// <summary>
    /// Returns a trip with at most one passenger.
    /// </summary>
    public class SinglePassengerRideMatcher : IRideMatcher
    {
        public async Task<(IList<MatchableRideRequest>, RideInfo)>
            GetRide(UserRideOffer offer,
                    ConcurrentGeoQuadtree<MatchableRideRequest> origins,
                    ConcurrentGeoQuadtree<MatchableRideRequest> destinations)
        {
            RouteInfo driverRoute = await GetRoute(offer);

            var originsTask = GetElementsInsideAsync(origins, NearRoute);
            var destinationsTask = GetElementsInsideAsync(destinations, NearRoute);

            // Only consider passengers whose origins and destinations are near
            // the driver's route.
            var potentialPassengers = new HashSet<MatchableRideRequest>(
                from element in await originsTask
                select element.Data);

            potentialPassengers.IntersectWith(
                from element in await destinationsTask
                select element.Data);

            // Find a passenger going in the same direction as the driver such that
            // picking up the passenger does not put the driver too far out of their way.
            foreach (var passenger in potentialPassengers.Where(GoingInDriversDirection))
            {
                RouteInfo routeWithPassenger = await GetRouteWithPassenger(offer, passenger);

                // Reject route if it's too far out of the way according to
                // the driver's settings.
                if (driverRoute.drivingTime.HasValue && routeWithPassenger.drivingTime.HasValue)
                {
                    TimeSpan originalTime = driverRoute.drivingTime.Value;
                    TimeSpan newTime = routeWithPassenger.drivingTime.Value;
                    TimeSpan maxTime = originalTime + TimeSpan.FromMinutes(offer.RideOffer.MaxTimeOutOfWay);

                    if (newTime > maxTime)
                    {
                        // Output debug info for demos.
                        Program.LogError($"Matched {offer.User.UserInfo.UserId} with {passenger.Request.User.UserInfo.UserId}" +
                             " but resulting route was too long." +
                            $" Original trip duration: {originalTime.Minutes} mins." +
                            $" Matched trip duration: {newTime.Minutes} mins." +
                            $" Driver's max time out of way: {offer.RideOffer.MaxTimeOutOfWay} mins.");
                        continue;
                    }
                }

                return RideWithPassenger(offer, passenger);
            }

            return EmptyRide(offer, driverRoute);


            /// <summary>
            /// Tests whether any point in the rect is close enough to <see cref="route"/>.
            /// </summary>
            bool NearRoute(Rect rect)
            {
                // Ignore passengers more than approximately 1km of the route.
                // TODO Take large max-time-out-of-way values into account when choosing max-dist-out-of-way.
                double maxDistMeters = 1000;
                double maxDistDegrees = offer.RideOffer.Trip.Source.DegreesUpperBound(maxDistMeters);

                GeoPolyline route = driverRoute.overviewPolyline;
                return route.RectWithinDistance(rect, maxDistDegrees);
            }

            /// <summary>
            /// Tests whether this passenger is going in the same direction as the driver.
            /// </summary>
            bool GoingInDriversDirection(MatchableRideRequest request)
            {
                var driverDest = offer.RideOffer.Trip.Destination;
                var driverOrig = offer.RideOffer.Trip.Source;
                var driverSeg = new GeoSegment(driverOrig, driverDest);

                var passDest = request.Request.RideRequest.Trip.Destination;
                var passOrig = request.Request.RideRequest.Trip.Source;
                var passSeg = new GeoSegment(passOrig, passDest);

                // Use GeoSegments so that this works near prime meridian.
                var passDiff = passSeg.Point2Representative - passSeg.Point1Representative;
                var driverDiff = driverSeg.Point2Representative - driverSeg.Point1Representative;

                // Compute the dot product of the vectors. This is a pretty rough
                // estimate and doesn't take into account the Earth's curvature.
                return passDiff.Dot(driverDiff) > 0;
            }
        }

        public Task<RideInfo> MakeBestRide(UserRideOffer offer, IEnumerable<UserRideRequest> requestsToMatch)
        {
            // TODO: Other methods return a detailed route polyline, this one does not.

            var passengers = new List<UserRideRequest>(requestsToMatch);
            if (passengers.Count > 1)
                throw new ArgumentException();

            Route.Stop[] stops;

            if (passengers.Count == 0)
            {
                stops = new[]
                {
                    new Route.Stop(offer.RideOffer.Trip.Source),
                    new Route.Stop(offer.RideOffer.Trip.Destination)
                };
            }
            else
            {
                var passengerInfo = passengers[0].User.UserInfo;
                var passengerOrig = passengers[0].RideRequest.Trip.Source;
                var passengerDest = passengers[0].RideRequest.Trip.Destination;

                stops = new[]
                {
                    new Route.Stop(offer.RideOffer.Trip.Source),
                    new Route.Stop(passengerOrig, passengerInfo, true),
                    new Route.Stop(passengerDest, passengerInfo),
                    new Route.Stop(offer.RideOffer.Trip.Destination)
                };
            }

            var route = new Route(stops);
            return Task.FromResult(
                new RideInfo(offer.User.UserInfo.UserId, offer.RideOffer.Car, route));
        }



        static Task<HashSet<ConcurrentGeoQuadtree<MatchableRideRequest>.IElement>>
            GetElementsInsideAsync(
                ConcurrentGeoQuadtree<MatchableRideRequest> quadtree,
                Predicate<Rect> area)
        {
            return Task.Run(() => quadtree.GetElementsInside(area));
        }

        static Task<RouteInfo> GetRouteWithPassenger(UserRideOffer offer, MatchableRideRequest passenger)
        {
            try
            {
                return DirectionsService.ComputeApproximateDrivingInfo(
                            Program.GoogleApiKey,
                            offer.RideOffer.Trip.Source,
                            offer.RideOffer.Trip.Destination,
                            passenger.Request.RideRequest.Trip.Source,
                            passenger.Request.RideRequest.Trip.Destination);
            }
            catch (ApplicationException ex)
            {
                Program.LogError($"Failure in {nameof(SinglePassengerRideMatcher)}: {ex.Message}");

                return Task.FromResult(new RouteInfo(new GeoPolyline(new[]
                {
                    offer.RideOffer.Trip.Source,
                    passenger.Request.RideRequest.Trip.Source,
                    passenger.Request.RideRequest.Trip.Destination,
                    offer.RideOffer.Trip.Destination
                })));
            }
        }

        static Task<RouteInfo> GetRoute(UserRideOffer offer)
        {
            return GetRouteBetween(offer.RideOffer.Trip.Source, offer.RideOffer.Trip.Destination);
        }

        static Task<RouteInfo> GetRouteBetween(GeoCoordinates origin, GeoCoordinates destination)
        {
            try
            {
                return DirectionsService.ComputeApproximateDrivingInfo(
                            Program.GoogleApiKey,
                            origin, destination);
            }
            catch (ApplicationException ex)
            {
                Program.LogError($"Failure in {nameof(SinglePassengerRideMatcher)}: {ex.Message}");

                return Task.FromResult(new RouteInfo(new GeoPolyline(new[]
                {
                    origin,
                    destination
                })));
            }
        }

        static (IList<MatchableRideRequest>, RideInfo) EmptyRide(
            UserRideOffer offer, RouteInfo originalDriverRoute)
        {
            IEnumerable<Route.Stop> stops =
                originalDriverRoute.overviewPolyline.Points
                .Select(pt => new Route.Stop(pt));

            return (
                new List<MatchableRideRequest>(),
                new RideInfo(offer.User.UserInfo.UserId, offer.RideOffer.Car,
                    new Route(stops)));
        }

        static (IList<MatchableRideRequest>, RideInfo) RideWithPassenger(UserRideOffer offer, MatchableRideRequest request)
        {
            GeoCoordinates offerOrig = offer.RideOffer.Trip.Source;
            GeoCoordinates offerDest = offer.RideOffer.Trip.Destination;
            GeoCoordinates requestOrig = request.Request.RideRequest.Trip.Source;
            GeoCoordinates requestDest = request.Request.RideRequest.Trip.Destination;

            Task<RouteInfo> leg1Task = Task.Run(() => GetRouteBetween(offerOrig, requestOrig));
            Task<RouteInfo> leg2Task = Task.Run(() => GetRouteBetween(requestOrig, requestDest));
            Task<RouteInfo> leg3Task = Task.Run(() => GetRouteBetween(requestDest, offerDest));

            IReadOnlyList<GeoCoordinates> leg1Points = leg1Task.Result.overviewPolyline.Points;
            IReadOnlyList<GeoCoordinates> leg2Points = leg2Task.Result.overviewPolyline.Points;
            IReadOnlyList<GeoCoordinates> leg3Points = leg3Task.Result.overviewPolyline.Points;

            IEnumerable<Route.Stop> stops =
                leg1Points.SkipLast(1).Select(pt => new Route.Stop(pt)).Concat(
                leg2Points.Take(1).Select(pt => new Route.Stop(pt, request.Request.User.UserInfo, true))).Concat(
                leg2Points.Skip(1).SkipLast(1).Select(pt => new Route.Stop(pt))).Concat(
                leg3Points.Take(1).Select(pt => new Route.Stop(pt, request.Request.User.UserInfo, false))).Concat(
                leg3Points.Skip(1).Select(pt => new Route.Stop(pt)));

            return (
                new List<MatchableRideRequest> { request },
                new RideInfo(offer.User.UserInfo.UserId, offer.RideOffer.Car,
                    new Route(stops)));
        }
    }
}
