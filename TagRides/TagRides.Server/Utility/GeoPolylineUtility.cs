using System;
using System.Linq;
using TagRides.Shared.Geo;
using Google.Maps;
using Google.Maps.Direction;

namespace TagRides.Server.Utility
{
    public static class GeoPolylineUtility
    {
        public static GeoPolyline ToGeo(this Polyline polyline)
        {
            return new GeoPolyline(from latLng in PolylineEncoder.Decode(polyline.Points) select latLng.ToGeo());
        }
    }
}
