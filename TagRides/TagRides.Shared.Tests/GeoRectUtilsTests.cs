using System;
using System.Collections.Generic;
using Xunit;
using TagRides.Shared.Geo;
using TagRides.Shared.Utilities;

namespace TagRides.Shared.Tests
{
    public class GeoRectUtilsTests
    {
        [Fact]
        public void RectCrossingPolesNotValid()
        {
            Rect rectCrossingNorth = new Rect
            {
                xMin = -10,
                xMax = 10,
                yMin = 50,
                yMax = 100
            };

            Rect rectCrossingSouth = new Rect
            {
                xMin = -10,
                xMax = 10,
                yMin = 50,
                yMax = 100
            };

            Assert.False(GeoRectUtils.IsValidGeoRect(rectCrossingNorth));
            Assert.False(GeoRectUtils.IsValidGeoRect(rectCrossingSouth));
        }

        [Fact]
        public void RectWrappingAroundGlobeIsValid()
        {
            Rect rect = new Rect
            {
                xMin = -200,
                xMax = 200,
                yMin = -10,
                yMax = 10
            };

            Assert.True(GeoRectUtils.IsValidGeoRect(rect));
        }

        [Theory]
        [MemberData(nameof(VariousValidGeoRects))]
        public void RectIsValidTheory(Rect rect, string message)
        {
            Assert.True(GeoRectUtils.IsValidGeoRect(rect), message);
        }

        [Theory]
        [MemberData(nameof(IntersectingGeoRectPairs))]
        public void GeoRectsIntersectTheory(Rect rect1, Rect rect2, string message)
        {
            Assert.True(GeoRectUtils.Intersect(rect1, rect2), message);
        }

        [Fact]
        public void GeoRectIsNearSegmentItContains()
        {
            Rect rect = new Rect
            {
                xMin = -10,
                xMax = 10,
                yMin = -10,
                yMax = 10
            };

            GeoCoordinates segA = new GeoCoordinates(-1, 0);
            GeoCoordinates segB = new GeoCoordinates(1, 0);
            GeoSegment seg = new GeoSegment(segA, segB);

            Assert.True(GeoRectUtils.RectNearSegment(seg, rect, 0));
        }

        [Fact]
        public void GeoRectIsNearSegmentThatIntersectsItsEdge()
        {
            Rect rect = new Rect
            {
                xMin = -10,
                xMax = 10,
                yMin = -10,
                yMax = 10
            };

            GeoCoordinates segA = new GeoCoordinates(0, -20);
            GeoCoordinates segB = new GeoCoordinates(0, 20);
            GeoSegment seg = new GeoSegment(segA, segB);

            Assert.True(GeoRectUtils.RectNearSegment(seg, rect, 0));
        }

        [Fact]
        public void GeoRectIsWithinDistanceOfSegment()
        {
            Rect rect = new Rect
            {
                xMin = -10,
                xMax = 10,
                yMin = -10,
                yMax = 10
            };

            GeoCoordinates segA = new GeoCoordinates(11, -20);
            GeoCoordinates segB = new GeoCoordinates(11, 20);
            GeoSegment seg = new GeoSegment(segA, segB);

            Assert.True(GeoRectUtils.RectNearSegment(seg, rect, 1 + 1e-10));
        }

        [Fact]
        public void RectIsntNearSegment()
        {
            Rect rect = new Rect
            {
                xMin = -10,
                xMax = 10,
                yMin = -10,
                yMax = 10
            };

            GeoCoordinates segA = new GeoCoordinates(11, -20);
            GeoCoordinates segB = new GeoCoordinates(11, 20);
            GeoSegment seg = new GeoSegment(segA, segB);

            Assert.False(GeoRectUtils.RectNearSegment(seg, rect, 0.5));
        }

        [Fact]
        public void FullEarthRectContainsAverageSegment()
        {
            Rect fullEarth = GetFullEarthRect();

            GeoCoordinates segA = new GeoCoordinates(0, -10);
            GeoCoordinates segB = new GeoCoordinates(10, 10);
            GeoSegment seg = new GeoSegment(segA, segB);

            Assert.True(GeoRectUtils.RectNearSegment(seg, fullEarth, 0));
        }

        [Fact]
        public void FullEarthRectContainsSegmentCrossingPrimeMeridian()
        {
            Rect fullEarth = GetFullEarthRect();

            GeoCoordinates segA = new GeoCoordinates(-10, 170);
            GeoCoordinates segB = new GeoCoordinates(10, 190);
            GeoSegment seg = new GeoSegment(segA, segB);

            Assert.True(GeoRectUtils.RectNearSegment(seg, fullEarth, 0));
        }

        [Fact]
        public void SmallRectContainsSegmentCrossingPrimeMeridian()
        {
            Rect rect = new Rect
            {
                xMin = 175,
                xMax = 178,
                yMin = 10,
                yMax = 13
            };

            GeoCoordinates segA = new GeoCoordinates(11, 170);
            GeoCoordinates segB = new GeoCoordinates(11, 190);
            GeoSegment seg = new GeoSegment(segA, segB);

            Assert.True(GeoRectUtils.RectNearSegment(seg, rect, 0));
        }
        [Fact]
        public void SmallRectContainsSegmentCrossingPrimeMeridianOnOppositeSide()
        {
            Rect rect = new Rect
            {
                xMin = -181,
                xMax = -179,
                yMin = 10,
                yMax = 13
            };

            GeoCoordinates segA = new GeoCoordinates(11, 170);
            GeoCoordinates segB = new GeoCoordinates(11, 190);
            GeoSegment seg = new GeoSegment(segA, segB);

            Assert.True(GeoRectUtils.RectNearSegment(seg, rect, 0));
        }

        [Fact]
        public void RectNearSegmentWorksWithRectCrossingPrimeMeridian()
        {
            Rect rect = new Rect
            {
                xMin = -185,
                xMax = -175,
                yMin = -10,
                yMax = 10
            };

            GeoCoordinates segA = new GeoCoordinates(-1, -170);
            GeoCoordinates segB = new GeoCoordinates(5, -190);
            GeoSegment seg = new GeoSegment(segA, segB);

            Assert.True(GeoRectUtils.RectNearSegment(seg, rect, 0));
        }

        [Fact]
        public void RectNearSegmentWorksWithRectCrossingPrimeMeridian2()
        {
            Rect rect = new Rect
            {
                xMin = -185,
                xMax = -175,
                yMin = -10,
                yMax = 10
            };

            GeoCoordinates segA = new GeoCoordinates(-30, -177);
            GeoCoordinates segB = new GeoCoordinates(30, -177);
            GeoSegment seg = new GeoSegment(segA, segB);

            Assert.True(GeoRectUtils.RectNearSegment(seg, rect, 0));
        }

        public static IEnumerable<object[]> IntersectingGeoRectPairs = new List<object[]>
        {
            new object[] {
                new Rect(-10, -10, 20, 20),
                new Rect(-10, -10, 20, 20),
                "A rectangle intersects itself."
            },

            new object[] {
                new Rect(-10, -10, 200, 10),
                new Rect(-10, -10, 200, 10),
                "A rectangle intersects itself even if it crosses the longitude=180 line."
            },

            new object[] {
                GetFullEarthRect(),
                new Rect(-10, 10, 10, 10),
                "Every valid georectangle intersects full Earth georectangle"
            }
        };

        public static IEnumerable<object[]> VariousValidGeoRects = new List<object[]>
        {
            new object[] {
                GetFullEarthRect(),
                "The rectangle representing the whole globe is valid."
            },

            new object[] {
                new Rect(-10, -10, 10, 10),
                "A rectangle within the lat/long bounds is valid."
            },

            new object[] {
                new Rect(170, -10, 20, 20),
                "A rectangle that crosses the longitude boundary is valid."
            },

            new object[] {
                new Rect(0, -10, 400, 20),
                "A rectangle with width > 360 is valid."
            }
        };

        // This is a method to avoid static initialization order issues.
        public static Rect GetFullEarthRect() => new Rect(-180, -90, 360, 180);
    }
}
