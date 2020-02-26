using System;
namespace TagRides.Shared.Utilities
{
    public static class Geometry2d
    {
        public static Vector2 ToVector2(this Geo.GeoCoordinates coords)
        {
            return new Vector2(coords.Longitude, coords.Latitude);
        }

        /// <summary>
        /// Tests whether the minimum distance between two line segments is
        /// less than some given value.
        /// </summary>
        /// <returns><c>true</c>, if distance between segments is below <paramref name="dist"/>,
        /// <c>false</c> otherwise.</returns>
        /// <param name="seg1a">Segment 1 first endpoint.</param>
        /// <param name="seg1b">Segment 1 second endpoint.</param>
        /// <param name="seg2a">Segment 2 first endpoint.</param>
        /// <param name="seg2b">Segment 2 second endpoint.</param>
        /// <param name="dist">Distance to check.</param>
        public static bool SegmentsWithinDistance(
            Vector2 seg1a, Vector2 seg1b,
            Vector2 seg2a, Vector2 seg2b,
            double dist)
        {
            if (SegmentsIntersect(seg1a, seg1b, seg2a, seg2b))
                return true;

            return PointToSegmentDistance(seg1a, seg1b, seg2a) < dist
                || PointToSegmentDistance(seg1a, seg1b, seg2b) < dist
                || PointToSegmentDistance(seg2a, seg2b, seg1a) < dist
                || PointToSegmentDistance(seg2a, seg2b, seg1b) < dist;
        }

        /// <summary>
        /// Tests whether two line segments intersect.
        /// </summary>
        /// <returns><c>true</c>, if segments intersect, <c>false</c> otherwise.</returns>
        /// <param name="seg1a">Segment 1 first endpoint.</param>
        /// <param name="seg1b">Segment 1 second endpoint.</param>
        /// <param name="seg2a">Segment 2 first endpoint.</param>
        /// <param name="seg2b">Segment 2 second endpoint.</param>
        /// <param name="epsilon">Epsilon to use for floating point comparisons.</param>
        public static bool SegmentsIntersect(
            Vector2 seg1a, Vector2 seg1b,
            Vector2 seg2a, Vector2 seg2b,
            double epsilon = 1e-10)
        {
            Vector2 ray1 = seg1b - seg1a;
            Vector2 ray2 = seg2b - seg2a;

            // We want t1, t2 such that
            //  seg1a + ray1 * t1 = seg2a + ray2 * t2
            //
            // This reduces to the following linear system
            // (ray1.x -ray2.x) (t1) = (seg2a.x - seg1a.x)
            // (ray1.y -ray2.y) (t2) = (seg2a.y - seg1a.y)

            double determinant = ray1.y * ray2.x - ray1.x * ray2.y;

            if (Math.Abs(determinant) < epsilon)
            {
                // Segments are essentially parallel. They intersect if and
                // only if one of the points on the second segment lies on
                // the first segment.

                return PointToSegmentDistance(seg1a, seg1b, seg2a, epsilon) < epsilon
                    || PointToSegmentDistance(seg1a, seg1b, seg2b, epsilon) < epsilon;
            }

            double t1 = ((seg2a.y - seg1a.y) * ray2.x - (seg2a.x - seg1a.x) * ray2.y) / determinant;
            double t2 = ((seg2a.y - seg1a.y) * ray1.x - (seg2a.x - seg1a.x) * ray1.y) / determinant;

            return 0 <= t1 && t1 <= 1
                && 0 <= t2 && t2 <= 1;
        }

        /// <summary>
        /// Computes the distance between a point and a line segment.
        /// </summary>
        /// <returns>The distance from the point to the line segment.</returns>
        /// <param name="segA">First endpoint of segment.</param>
        /// <param name="segB">Second endpoint of segment.</param>
        /// <param name="p">Point.</param>
        /// <param name="epsilon">Epsilon to use for floating point comparisons.</param>
        public static double PointToSegmentDistance(
            Vector2 segA, Vector2 segB, Vector2 p,
            double epsilon = 1e-10)
        {
            Vector2 ray = segB - segA;
            double rayMagnitude = ray.Magnitude;

            if (rayMagnitude < epsilon)
                // Segment is just a single point.
                return (p - segA).Magnitude;

            // segA + t * ray / rayMagnitude is closest point to p along line crossing A and B
            double t = ray.Dot(p - segA) / rayMagnitude;

            if (0 <= t && t <= rayMagnitude)
            {
                // Point is between A and B, so just return the distance
                // from the point to the line crossing A and B.
                return Math.Sqrt((p - segA).SqrMagnitude - t * t);
            }

            if (t < 0)
            {
                // Point is behind A, so its distance to the segment is just
                // its distance to A.
                return (p - segA).Magnitude;
            }

            // Point is beyond B, so its distance to the segment is its distance to B.
            return (p - segB).Magnitude;
        }
    }
}
