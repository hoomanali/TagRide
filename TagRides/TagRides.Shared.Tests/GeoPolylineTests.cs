using System;
using System.Collections.Generic;
using TagRides.Shared.Geo;
using TagRides.Shared.Utilities;
using Xunit;

namespace TagRides.Shared.Tests
{
    public class GeoPolylineTests
    {
        [Fact]
        public void SinglePointPolylineRectWithinDistanceIsCircleTest()
        {
            GeoPolyline pl = new GeoPolyline(new GeoCoordinates(0, 0));

            Rect r = new Rect
            {
                xMin = 1,
                xMax = 2,
                yMin = 1,
                yMax = 2
            };

            Assert.False(pl.RectWithinDistance(r, 1.1));
            Assert.True(pl.RectWithinDistance(r, 1.5));
        }

        [Fact]
        public void LongPolylineRectWithinDistanceSanityTest1()
        {
            List<GeoCoordinates> points = new List<GeoCoordinates>();
            for (int i = 0; i < 1000; ++i)
                // Points stretch from lng=0 to lng=9.99
                points.Add(new GeoCoordinates(0, i / 100.0));

            GeoPolyline pl = new GeoPolyline(points);

            // Rect 1 unit above line.
            Rect r = new Rect
            {
                xMin = 0,
                xMax = 10,
                yMin = 1,
                yMax = 2
            };

            Assert.False(pl.RectWithinDistance(r, 0.9));
            Assert.True(pl.RectWithinDistance(r, 1.1));
        }

        [Fact]
        public void LongPolylineRectWithinDistanceSanityTest2()
        {
            List<GeoCoordinates> points = new List<GeoCoordinates>();
            for (int i = 0; i < 1000; ++i)
                // Points stretch from (0,0) to (9.99, 9.99)
                points.Add(new GeoCoordinates(i / 100.0, i / 100.0));

            GeoPolyline pl = new GeoPolyline(points);

            // Rect is in upper left corner of the square (0,0),(10,0),(10,10),(0,10)
            // and has side length 4. It should be sqrt(2) units away from line.
            Rect r = new Rect
            {
                xMin = 0,
                xMax = 4,
                yMin = 6,
                yMax = 10
            };

            Assert.False(pl.RectWithinDistance(r, 1.3));
            Assert.True(pl.RectWithinDistance(r, 1.5));
        }
    }
}
