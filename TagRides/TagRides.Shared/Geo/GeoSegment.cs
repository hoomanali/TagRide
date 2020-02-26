using System;
using TagRides.Shared.Utilities;
namespace TagRides.Shared.Geo
{
    /// <summary>
    /// Defines a "line segment" on the world map. Most pairs of points on the
    /// Earth have a unique shortest line segment connecting them.
    /// </summary>
    public class GeoSegment
    {
        public readonly GeoCoordinates Endpoint1;
        public readonly GeoCoordinates Endpoint2;

        /// <summary>
        /// Vector2 representative of first endpoint. Equivalent to
        /// Endpoint1.ToVector2().
        /// </summary>
        public Vector2 Point1Representative => Endpoint1.ToVector2();

        /// <summary>
        /// Vector2 representative of second endpoint. This might not be
        /// equivalent to Endpoint2.ToVector2(). The representative is picked
        /// such that the segment consists of all points given by linearly
        /// interpolating between <see cref="Point1Representative"/> and
        /// <see cref="Point2Representative"/>.
        /// </summary>
        public Vector2 Point2Representative
        {
            get
            {
                if (CrossesMeridianLeftOfP1)
                    return Endpoint2.ToVector2() - new Vector2(360, 0);

                if (CrossesMeridianRightOfP1)
                    return Endpoint2.ToVector2() + new Vector2(360, 0);

                return Endpoint2.ToVector2();
            }
        }

        public GeoSegment(GeoCoordinates p1, GeoCoordinates p2)
        {
            Endpoint1 = p1;
            Endpoint2 = p2;
        }

        /// <summary>
        /// Does this segment cross the line with zero longitude?
        /// </summary>
        public bool CrossesMeridian
        {
            get
            {
                return CrossesMeridianLeftOfP1 || CrossesMeridianRightOfP1;
            }
        }

        /// <summary>
        /// Is the representative of <see cref="Endpoint2"/> with longitude
        /// increased by 360 closer to <see cref="Endpoint1"/>?
        /// </summary>
        public bool CrossesMeridianRightOfP1
        {
            get
            {
                // These representatives will be within the usual coordinate range.
                Vector2 p1 = Endpoint1.ToVector2();
                Vector2 p2 = Endpoint2.ToVector2();

                // A different representative of P2 that is to the right of P1.
                Vector2 p2OtherR = p2 + new Vector2(360, 0);

                double normalDist = (p2 - p1).SqrMagnitude;
                double rightDist = (p2OtherR - p1).SqrMagnitude;

                return rightDist < normalDist;
            }
        }

        /// <summary>
        /// Is the representative of <see cref="Endpoint2"/> with longitude
        /// decreased by 360 closer to <see cref="Endpoint1"/>?
        /// </summary>
        public bool CrossesMeridianLeftOfP1
        {
            get
            {
                // These representatives will be within the usual coordinate range.
                Vector2 p1 = Endpoint1.ToVector2();
                Vector2 p2 = Endpoint2.ToVector2();

                // A different representative of P2 that is to the left of P1.
                Vector2 p2OtherL = p2 - new Vector2(360, 0);

                double normalDist = (p2 - p1).SqrMagnitude;
                double leftDist = (p2OtherL - p1).SqrMagnitude;

                return leftDist < normalDist;
            }
        }


        /// <summary>
        /// Tests whether the point is within a given distance of the segment,
        /// where the distance is given by the metric lat^2 + long^2 in WGS84
        /// coordinates (so it doesn't correspond to real distance).
        /// </summary>
        /// <returns><c>true</c>, if point is within distance, <c>false</c> otherwise.</returns>
        /// <param name="point">Point on Earth.</param>
        /// <param name="distance">Distance in units of sqrt(lat^2 + long^2).</param>
        public bool PointWithinDistance(GeoCoordinates point, double distance)
        {
            return DistanceToPoint(point) < distance;
        }

        /// <summary>
        /// Computes the distance from the point to the segment. The return
        /// value is in units of sqrt(lat^2 + long^2) in WGS84 coordinates.
        /// </summary>
        /// <returns>The distance to the point in units of sqrt(lat^2 + long^2).</returns>
        /// <param name="point">Point on Earth.</param>
        public double DistanceToPoint(GeoCoordinates point)
        {
            Vector2 p1 = Endpoint1.ToVector2();
            Vector2 p2 = Endpoint2.ToVector2();

            if (CrossesMeridianRightOfP1)
            {
                p2 += new Vector2(360, 0);

                Vector2 pointRepr1 = point.ToVector2();
                Vector2 pointRepr2 = pointRepr1 + new Vector2(360, 0);

                double dist1 = Geometry2d.PointToSegmentDistance(p1, p2, pointRepr1);
                double dist2 = Geometry2d.PointToSegmentDistance(p1, p2, pointRepr2);

                return Math.Min(dist1, dist2);
            }
            else if (CrossesMeridianLeftOfP1)
            {
                p2 -= new Vector2(360, 0);

                Vector2 pointRepr1 = point.ToVector2();
                Vector2 pointRepr2 = pointRepr1 - new Vector2(360, 0);

                double dist1 = Geometry2d.PointToSegmentDistance(p1, p2, pointRepr1);
                double dist2 = Geometry2d.PointToSegmentDistance(p1, p2, pointRepr2);

                return Math.Min(dist1, dist2);
            }
            else
            {
                return Geometry2d.PointToSegmentDistance(p1, p2, point.ToVector2());
            }
        }
    }
}
