using System;
using System.Collections.Generic;
using Xunit;
using TagRides.Shared.Utilities;

namespace TagRides.Shared.Tests
{
    public class Geometry2dTests
    {
        [Fact]
        public void PointToSegmentDistance1()
        {
            Vector2 p = new Vector2(0, 1);
            Vector2 a = new Vector2(-1, 0);
            Vector2 b = new Vector2(1, 0);

            Assert.InRange(Geometry2d.PointToSegmentDistance(a, b, p),
                1 - 1e-10,
                1 + 1e-10);
        }

        [Fact]
        public void PointToSegmentDistance2()
        {
            Vector2 p = new Vector2(0, 1);
            Vector2 a = new Vector2(1, 0);
            Vector2 b = new Vector2(2, 0);

            double ap = (a - p).Magnitude;

            Assert.InRange(Geometry2d.PointToSegmentDistance(a, b, p),
                ap - 1e-10,
                ap + 1e-10);
        }

        [Fact]
        public void PointToSegmentDistance3()
        {
            Vector2 p = new Vector2(0, 1);
            Vector2 a = new Vector2(2, 0);
            Vector2 b = new Vector2(1, 0);

            double bp = (b - p).Magnitude;

            Assert.InRange(Geometry2d.PointToSegmentDistance(a, b, p),
                bp - 1e-10,
                bp + 1e-10);
        }

        [Fact]
        public void PointToDegenerateSegmentDistance()
        {
            Vector2 p = new Vector2(0, 1);
            Vector2 a = new Vector2(1, 1);

            double dist = (p - a).Magnitude;

            Assert.InRange(Geometry2d.PointToSegmentDistance(a, a, p),
                dist - 1e-10,
                dist + 1e-10);
        }

        [Fact]
        public void SegmentsIntersect1()
        {
            Vector2 a1 = new Vector2(-1, 0);
            Vector2 b1 = new Vector2(1, 0);

            Vector2 a2 = new Vector2(0, -1);
            Vector2 b2 = new Vector2(0, 1);

            Assert.True(Geometry2d.SegmentsIntersect(a1, b1, a2, b2));
        }

        [Fact]
        public void ParallelSegmentsNotNearEachOtherDontIntersect()
        {
            Vector2 a1 = new Vector2(-1, 0);
            Vector2 b1 = new Vector2(1, 0);

            Vector2 up = new Vector2(0, 1);
            Vector2 a2 = a1 + up;
            Vector2 b2 = b1 + up;

            Assert.False(Geometry2d.SegmentsIntersect(a1, b1, a2, b2));
        }

        [Fact]
        public void ParallelSegmentsCanIntersect()
        {
            Vector2 a1 = new Vector2(-1, 0);
            Vector2 b1 = new Vector2(1, 0);

            Vector2 a2 = new Vector2(0, 0);
            Vector2 b2 = new Vector2(0.5, 0);

            Assert.True(Geometry2d.SegmentsIntersect(a1, b1, a2, b2));
        }
    }
}
