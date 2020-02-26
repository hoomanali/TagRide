using System;
namespace TagRides.Shared.Geo
{
    /// <summary>
    /// A route overview including a polyline and a driving time.
    /// </summary>
    public class RouteInfo
    {
        public readonly TimeSpan? drivingTime;
        public readonly GeoPolyline overviewPolyline;

        public RouteInfo(GeoPolyline overview, TimeSpan? time = null)
        {
            overviewPolyline = overview;
            drivingTime = time;
        }
    }
}
