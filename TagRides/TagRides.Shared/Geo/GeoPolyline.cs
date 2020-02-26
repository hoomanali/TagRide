using System.Collections.Generic;
using System.Linq;
using System;
using TagRides.Shared.Utilities;

namespace TagRides.Shared.Geo
{
    /// <summary>
    /// A readonly data structure representing a polyline of geocoordinates.
    /// </summary>
    public class GeoPolyline
    {
        /// <summary>
        /// The list of points that define the polyline.
        /// </summary>
        /// <value>The list of points.</value>
        public IReadOnlyList<GeoCoordinates> Points { get; }

        public IEnumerable<GeoSegment> Segments =>
            Points
                .ConsecutivePairs()
                .Select(pair => new GeoSegment(pair.Item1, pair.Item2));

        /// <summary>
        /// Creates a new GeoPolyline with the given points. Note that most
        /// methods assume that consecutive points are within 180 degrees of latitude
        /// of each other.
        /// </summary>
        /// <param name="points">Points.</param>
        public GeoPolyline(IEnumerable<GeoCoordinates> points)
        {
            Points = points.ToList();
        }

        /// <summary>
        /// Creates a new GeoPolyline with the given points. Note that most
        /// methods assume that consecutive points are within 180 degrees of latitude
        /// of each other.
        /// </summary>
        /// <param name="points">Points.</param>
        public GeoPolyline(params GeoCoordinates[] points)
        {
            Points = points.ToList();
        }

        /// <summary>
        /// Creates a new GeoPolyline that is a piece of a larger GeoPolyline.
        /// This is a lightweight view into the larger polyline.
        /// </summary>
        /// <param name="original">Original polyline.</param>
        /// <param name="startIdx">Index of first point that belongs to this polyline.</param>
        /// <param name="endIdx">Index after the last point that belongs to this polyline.</param>
        public GeoPolyline(GeoPolyline original, int startIdx, int endIdx)
        {
            Points = new ReadOnlySubList<GeoCoordinates>(original.Points, startIdx, endIdx - startIdx);
        }

        /// <summary>
        /// Tests whether the point is within a given distance of the polyline,
        /// where the distance is given by the metric lat^2 + long^2 in WGS84
        /// coordinates (so it doesn't correspond to real distance).
        /// </summary>
        /// <returns><c>true</c>, if point is within distance, <c>false</c> otherwise.</returns>
        /// <param name="point">Point on Earth.</param>
        /// <param name="distance">Distance in units of sqrt(lat^2 + long^2).</param>
        public bool PointWithinDistance(GeoCoordinates point, double distance)
        {
            return Segments.Any(segment => segment.PointWithinDistance(point, distance));
        }

        /// <summary>
        /// Tests whether the rectangle is within a given distance of the polyline,
        /// where the distance is given by the metric lat^2 + long^2 in WGS84
        /// coordinates (so it doesn't correspond to real distance).
        /// </summary>
        /// <returns><c>true</c>, if rect is within distance, <c>false</c> otherwise.</returns>
        /// <param name="geoRect">A valid geo rectangle.</param>
        /// <param name="distance">Distance in units of sqrt(lat^2 + long^2).</param>
        public bool RectWithinDistance(Rect geoRect, double distance)
        {
            if (!GeoRectUtils.IsValidGeoRect(geoRect))
                throw new ArgumentException("Rectangle must be valid geo rectangle.");

            // Try fast heuristics for longer polylines.
            if (Points.Count >= 10)
            {
                Rect fattenedBounds = BoundingRect();
                fattenedBounds.xMin -= distance;
                fattenedBounds.yMin -= distance;
                fattenedBounds.xMax += distance;
                fattenedBounds.yMax += distance;

                // Ensure rectangle is within geo bounds.
                fattenedBounds.yMin = Math.Max(-90, fattenedBounds.yMin);
                fattenedBounds.yMax = Math.Min(90, fattenedBounds.yMax);

                // Quick test to rule out certain cases (good for long polylines)
                if (!GeoRectUtils.Intersect(geoRect, fattenedBounds))
                    return false;

                GeoPolyline firstHalf = new GeoPolyline(this, 0, Points.Count / 2);
                GeoPolyline secondHalf = new GeoPolyline(this, Points.Count / 2, Points.Count);

                return firstHalf.RectWithinDistance(geoRect, distance)
                    || secondHalf.RectWithinDistance(geoRect, distance);
            }

            if (Points.Count >= 2)
            {
                return Segments.Any(segment => GeoRectUtils.RectNearSegment(segment, geoRect, distance));
            }

            if (Points.Count == 1)
            {
                // Perform a capsule test on a single point (this amounts to
                // a circle test).
                return GeoRectUtils.RectNearSegment(new GeoSegment(Points[0], Points[0]), geoRect, distance);
            }

            return true;
        }

        public Rect BoundingRect()
        {
            double maxLat = -90, minLat = 90, minLng = 180, maxLng = -180;
            foreach (GeoCoordinates p in Points)
            {
                if (p.Latitude < minLat) minLat = p.Latitude;
                if (p.Latitude > maxLat) maxLat = p.Latitude;

                if (p.Longitude > maxLng) maxLng = p.Longitude;
                if (p.Longitude < minLng) minLng = p.Longitude;
            }

            return new Rect(minLng, minLat, maxLng - minLng, maxLat - minLat);
        }

        /// <summary>
        /// Creates a polyline by joining together the end of each polyline
        /// with the beginning of the next polyline. This method assumes that
        /// the first point of each polyline corresponds to the last point of
        /// its predecessor, if any. It also assumes that no polyline is empty.
        /// </summary>
        /// <returns>The overall polyline.</returns>
        /// <param name="polylines">List of polylines whose ends align.</param>
        public static GeoPolyline Join(IEnumerable<GeoPolyline> polylines)
        {
            GeoPolyline first = polylines.First();
            var rest = polylines.Skip(1);

            return new GeoPolyline(first.Points.Concat(
                        rest.SelectMany((line) => line.Points.Skip(1))));
        }
    }
}
