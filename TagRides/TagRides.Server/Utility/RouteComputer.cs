using System.Collections.Generic;
using System.Linq;
using Google.Maps;
using Google.Maps.Direction;
using TagRides.Server.Exceptions;
using TagRides.Shared.Geo;

namespace TagRides.Server.Utility
{
    /// <summary>
    /// Provides access to user data that is only available when a user is
    /// online or has been online recently, such as location information,
    /// current ride request, etc.
    /// </summary>
    public static class RouteComputer
    {
        /// <summary>
        /// Computes a driving route between two points. This may return null if
        /// a route could not be found.
        /// </summary>
        /// <exception cref="ApiAccessException">Thrown if underlying API (e.g. Google Maps) returns an error.</exception>
        /// <returns>The route.</returns>
        /// <param name="origin">Origin.</param>
        /// <param name="destination">Destination.</param>
        static GeoPolyline ComputeRoute(GeoCoordinates origin, GeoCoordinates destination)
        {
            var request = new DirectionRequest
            {
                Origin = origin.ToGoogleLocation(),
                Destination = destination.ToGoogleLocation()
            };

            var response = new DirectionService().GetResponse(request);

            if (response.ErrorMessage != null)
            {
                throw new ApiAccessException("Google Directions API failed with: " + response.ErrorMessage);
            }

            if (response.Routes.Length == 0)
            {
                // This can happen if a route is not found.
                return null;
            }

            return response.Routes[0].OverviewPolyline.ToGeo();
        }

        /// <summary>
        /// Computes a series of driving routes from the origin, through each waypoint (in order),
        /// and to the destination. The first polyline corresponds to the route from the origin to the
        /// first waypoint, and the last polyline corresponds to the route from the last waypoint
        /// to the destination. If <paramref name="wayPoints"/> is empty, this is equivalent
        /// to <see cref="ComputeRoute(GeoCoordinates, GeoCoordinates)"/>. Will return null
        /// if no route is found.
        /// </summary>
        /// <exception cref="ApiAccessException">Thrown if underlying API (e.g. Google Maps) returns an error.</exception>
        /// <returns>A list of routes making up the journey from the origin through the waypoints to the destination. Returns
        /// null if no route is found.</returns>
        /// <param name="origin">Origin.</param>
        /// <param name="destination">Destination.</param>
        /// <param name="wayPoints">Way points.</param>
        static IEnumerable<GeoPolyline> ComputeRouteWithWaypoints(GeoCoordinates origin, GeoCoordinates destination,
            IEnumerable<GeoCoordinates> wayPoints)
        {
            List<GeoCoordinates> allWaypoints = wayPoints.ToList();
            if (allWaypoints.Count == 0)
            {
                var route = ComputeRoute(origin, destination);

                if (route == null)
                    return null;

                return new List<GeoPolyline> { route };
            }

            var request = new DirectionRequest
            {
                Origin = origin.ToGoogleLocation(),
                Destination = destination.ToGoogleLocation(),
                Waypoints = allWaypoints.Select((pt) => (Location)pt.ToGoogleLocation()).ToList()
            };

            var response = new DirectionService().GetResponse(request);

            if (response.ErrorMessage != null)
            {
                throw new ApiAccessException("Google Directions API failed with: " + response.ErrorMessage);
            }

            if (response.Routes.Length == 0)
            {
                return null;
            }

            return response.Routes[0].Legs
                    .Select((leg) => leg.Steps)
                    .Select((stepArray) => GeoPolyline.Join(stepArray.Select((step) => step.Polyline.ToGeo())));
        }
    }
}
