using System;
using System.Linq;
using System.Diagnostics;
using TagRides.Shared.Utilities;
namespace TagRides.Shared.Geo
{
    public static class GeoRectUtils
    {
        /// <summary>
        /// Checks whether two latitude/longitude rectangles intersect.
        /// </summary>
        /// <returns>Whether the two geographical rectangles intersect.</returns>
        /// <param name="a">The first rectangle.</param>
        /// <param name="b">The second rectangle.</param>
        /// <exception cref="ArgumentException">Thrown if either of the parameters
        /// is not a valid geo rectangle. See <see cref="IsValidGeoRect(Rect)"/>.</exception>
        public static bool Intersect(Rect a, Rect b)
        {
            if (!IsValidGeoRect(a))
                throw new ArgumentException($"Given rectangle {a} is not a valid geo rectangle.");
            if (!IsValidGeoRect(b))
                throw new ArgumentException($"Given rectangle {b} is not a valid geo rectangle.");

            int numA = Split(a, out Rect a1, out Rect? a2);
            int numB = Split(b, out Rect b1, out Rect? b2);

            if (a1.Intersects(b1))
                return true;

            if (numB > 1 && a1.Intersects(b2.Value))
                return true;

            if (numA > 1)
            {
                if (a2.Value.Intersects(b1))
                    return true;

                if (numB > 1 && a2.Value.Intersects(b2.Value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether the given geo rectangle contains the geo position.
        /// </summary>
        /// <returns><c>true</c>, if the rectangle contains the position, <c>false</c> otherwise.</returns>
        /// <param name="rect">A valid geo rectangle.</param>
        /// <param name="coordinates">A geo position.</param>
        /// <exception cref="ArgumentException">Thrown if the given rect is not
        /// a valid geo rectangle. See <see cref="IsValidGeoRect(Rect)"/>.</exception>
        public static bool GeoContains(Rect rect, GeoCoordinates coordinates)
        {
            if (!IsValidGeoRect(rect))
                throw new ArgumentException($"Given rectangle {rect} is not a valid geo rectangle.");

            int num = Split(rect, out Rect piece1, out Rect? piece2);

            double x = coordinates.Longitude;
            double y = coordinates.Latitude;

            if (piece1.Contains(x, y))
                return true;

            if (num > 1 && piece2.Value.Contains(x, y))
                return true;

            return false;
        }

        /// <summary>
        /// Tests whether the geo rectangle is within the given distance of the
        /// geo segment.
        /// </summary>
        /// <returns><c>true</c>, if the rectangle contains the segment or one
        /// of the rectangle's edges is near the segment, <c>false</c> otherwise.</returns>
        /// <param name="segment">A reasonably short segment on the Earth.</param>
        /// <param name="rect">A valid geo rectangle to test.</param>
        /// <param name="maxDist">Maximum distance from segment.</param>
        public static bool RectNearSegment(GeoSegment segment, Rect rect, double maxDist)
        {
            // (Positive) cases:
            // - Segment fully contained within rect
            // - At least one edge of rect is within maxDist of segment

            if (GeoContains(rect, segment.Endpoint1) && GeoContains(rect, segment.Endpoint2))
                return true;

            Vector2[] corners = {
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMax, rect.yMin)
            };

            var rectEdges = corners.ConsecutivePairs();

            Vector2 segA = segment.Endpoint1.ToVector2();
            Vector2 segB = segment.Endpoint2.ToVector2(); ;

            if (segment.CrossesMeridianLeftOfP1)
            {
                segB -= new Vector2(360, 0);

                if (rect.xMax > 180)
                {
                    // Since this is a valid geo rect, xMin is < 180, so this
                    // rect crosses the opposite side of the map that the
                    // segment crosses.
                    segA += new Vector2(360, 0);
                    segB += new Vector2(360, 0);
                }
            }
            else if (segment.CrossesMeridianRightOfP1)
            {
                segB += new Vector2(360, 0);

                if (rect.xMin < -180)
                {
                    // Since this is a valid geo rect, xMax is > -180, so this
                    // rect crosses the opposite side of the map that the
                    // segment crosses.
                    segA -= new Vector2(360, 0);
                    segB -= new Vector2(360, 0);
                }
            }

            return rectEdges.Any(edge =>
                Geometry2d.SegmentsWithinDistance(
                    segA,
                    segB,
                    edge.Item1,
                    edge.Item2,
                    maxDist));
        }

        /// <summary>
        /// Checks whether the Rect instance is a valid geo rectangle.
        /// </summary>
        /// <returns><c>true</c>, if <paramref name="rect"/> is a valid geo rectangle,
        /// <c>false</c> otherwise.</returns>
        /// <param name="rect">The rectangle to check.</param>
        public static bool IsValidGeoRect(Rect rect)
        {
            // Check that the latitudes are in the correct range. This class
            // does not support wrapping across the poles.
            if (rect.yMax > 90 || rect.yMin < -90)
                return false;

            // Check that the rectangle has nonnegative height and width.
            if (rect.height < 0 || rect.width < 0)
                return false;

            // It's okay for the rectangle to go around the globe.
            if (rect.width >= 360)
                return true;

            // If the rectangle doesn't go around the globe, then at least
            // one of its sides should be inside the [-180, 180] range.
            return (rect.xMin > -180 && rect.xMin < 180) || (rect.xMax > -180 && rect.xMax < 180);
        }

        /// <summary>
        /// Splits the given rectangle into one or two rectangles that are
        /// contained within the latitude/longitude bounds. Asserts that
        /// the given rectangle is valid.
        /// </summary>
        /// <returns>The number of rectangles. This is at least 1 and at most 2.</returns>
        /// <param name="geoRect">A valid geo rectangle.</param>
        /// <param name="rect1">First rectangle in the split.</param>
        /// <param name="rect2">Second rectangle in the split, if any.</param>
        static int Split(Rect geoRect, out Rect rect1, out Rect? rect2)
        {
            Debug.Assert(IsValidGeoRect(geoRect));

            // The rectangle goes all the way around the Earth.
            if (geoRect.width >= 360)
            {
                rect1 = new Rect(-180, -90, 360, 180);
                rect2 = null;
                return 1;
            }

            if (geoRect.xMin < -180)
            {
                rect1 = new Rect
                {
                    xMin = geoRect.xMin + 180,
                    xMax = 180,
                    yMin = geoRect.yMin,
                    yMax = geoRect.yMax
                };

                rect2 = new Rect
                {
                    xMin = -180,
                    xMax = geoRect.xMax,
                    yMin = geoRect.yMin,
                    yMax = geoRect.yMax
                };

                return 2;
            }

            if (geoRect.xMax > 180)
            {
                rect1 = new Rect
                {
                    xMin = geoRect.xMin,
                    xMax = 180,
                    yMin = geoRect.yMin,
                    yMax = geoRect.yMax
                };

                rect2 = new Rect
                {
                    xMin = -180,
                    xMax = geoRect.xMax - 180,
                    yMin = geoRect.yMin,
                    yMax = geoRect.yMax
                };

                return 2;
            }

            // In this case, the geoRect was already within the bounds.
            rect1 = geoRect;
            rect2 = null;
            return 1;
        }
    }
}
