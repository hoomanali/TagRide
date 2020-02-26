using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Maps;
using Google.Maps.Direction;

namespace TagRides.Shared.Geo
{
    public static class DirectionsService
    {
        /// <summary>
        /// Computes an overview of the driving route between two points using
        /// the Google Directions API.
        /// </summary>
        /// <exception cref="ApplicationException">Thrown if the Google Maps service
        /// does not return a route for any reason.</exception>
        /// <returns>The route info.</returns>
        /// <param name="apiKey">Google API key.</param>
        /// <param name="origin">Origin.</param>
        /// <param name="destination">Destination.</param>
        /// <param name="waypoints">Waypoints to visit.</param>
        public static async Task<RouteInfo> ComputeApproximateDrivingInfo(
            string apiKey,
            GeoCoordinates origin,
            GeoCoordinates destination,
            params GeoCoordinates[] waypoints)
        {
            DirectionRequest request = new DirectionRequest
            {
                Origin = origin.ToGoogleLatLng(),
                Destination = destination.ToGoogleLatLng(),
                Mode = TravelMode.driving,
                Waypoints = waypoints.Length == 0 ?
                    null : new List<Location>(waypoints.Select(pt => new LatLng(pt.Latitude, pt.Longitude)))
            };

            DirectionRoute route = await GetRouteOrThrow(apiKey, request);

            return new RouteInfo(
                route.OverviewPolyline.ToGeoPolyline(),
                TimeSpan.FromSeconds(
                    route.Legs.Sum(leg => leg.Duration.Value)));

        }

        static async Task<DirectionRoute> GetRouteOrThrow(string apiKey, DirectionRequest request)
        {
            DirectionResponse response = await GetDirectionsOrThrow(apiKey, request);
            return response.Routes[0];
        }

        static async Task<DirectionResponse> GetDirectionsOrThrow(string apiKey, DirectionRequest request)
        {
            // NOTE not the same as this class
            DirectionService service = new DirectionService(new GoogleSigned(apiKey));
            DirectionResponse response = await service.GetResponseAsync(request);

            if (response.Status != ServiceResponseStatus.Ok)
            {
                throw new ApplicationException("Google Maps API access failed: " +
                    $"({response.Status}) - message: {response.ErrorMessage}");
            }

            return response;
        }

        static GeoPolyline ToGeoPolyline(this Polyline polyline)
        {
            return new GeoPolyline(
                PolylineEncoder.Decode(polyline.Points)
                    .Select(pt => new GeoCoordinates(pt.Latitude, pt.Longitude)));
        }

        static LatLng ToGoogleLatLng(this GeoCoordinates coords)
        {
            return new LatLng(coords.Latitude, coords.Longitude);
        }
    }
}
