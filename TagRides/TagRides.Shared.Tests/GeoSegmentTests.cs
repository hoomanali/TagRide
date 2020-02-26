using System;
using Xunit;
using TagRides.Shared.Geo;
using TagRides.Shared.Utilities;
namespace TagRides.Shared.Tests
{
    public class GeoSegmentTests
    {
        [Fact]
        public void DistanceToPointForSmallSegmentNotCrossingPrimeMeridian()
        {
            Vector2 point = new Vector2(1, 1);
            Vector2 segA = new Vector2(-1, 0);
            Vector2 segB = new Vector2(1, 0);

            GeoCoordinates geoPoint = new GeoCoordinates(point.y, point.x);
            GeoSegment geoSegment = new GeoSegment(
                new GeoCoordinates(segA.y, segA.x),
                new GeoCoordinates(segB.y, segB.x));

            Assert.Equal(
                Geometry2d.PointToSegmentDistance(segA, segB, point),
                geoSegment.DistanceToPoint(geoPoint),
                6);
        }

        [Fact]
        public void DistanceToPointForSegmentCrossingPrimeMeridian()
        {
            Vector2 point = new Vector2(-180, 0);
            Vector2 segA = new Vector2(175, -10);
            Vector2 segB = new Vector2(185, 10);

            GeoCoordinates geoPoint = new GeoCoordinates(point.y, point.x);
            GeoSegment geoSegment = new GeoSegment(
                new GeoCoordinates(segA.y, segA.x),
                new GeoCoordinates(segB.y, segB.x));

            Assert.Equal(
                0,
                geoSegment.DistanceToPoint(geoPoint),
                6);
        }
    }
}
