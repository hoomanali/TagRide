using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using TagRides.Shared.Geo;
using TagRides.Shared.Utilities;

namespace TagRides.Shared.Tests
{
    public class ConcurrentGeoQuadtreeTests
    {
        [Fact]
        public void MovingElementChangesPosition()
        {
            ConcurrentGeoQuadtree<object> quadtree = new ConcurrentGeoQuadtree<object>();

            var element = quadtree.InsertElement(null, new GeoCoordinates(0, 0));

            Assert.True(element.Coordinates.Equals(new GeoCoordinates(0, 0)));

            Assert.True(quadtree.MoveElement(element, new GeoCoordinates(1, 1)));

            Assert.True(element.Coordinates.Equals(new GeoCoordinates(1, 1)));
        }

        [Fact]
        public void CannotMoveRemovedElement()
        {
            ConcurrentGeoQuadtree<object> quadtree = new ConcurrentGeoQuadtree<object>();

            var element = quadtree.InsertElement(null, new GeoCoordinates(0, 0));

            quadtree.RemoveElement(element);

            Assert.False(quadtree.MoveElement(element, new GeoCoordinates(1, 1)));
        }

        [Fact]
        public void QuadrantsSubdivideAfterPassingMaxCapacity()
        {
            ConcurrentGeoQuadtree<object> quadtree = new ConcurrentGeoQuadtree<object>(1, 10, 25);

            Assert.Equal(0, quadtree.GetLargestSubdivisionLevel());

            for (int i = 0; i < 20; ++i)
                quadtree.InsertElement(null, new GeoCoordinates(0, 0));

            quadtree.EfficientlyReindex();

            Assert.True(quadtree.GetLargestSubdivisionLevel() > 0);
        }

        [Fact]
        public void QuadrantsContinueSubdividingAfterCapacityEvenWhenNoInsert()
        {
            ConcurrentGeoQuadtree<object> quadtree = new ConcurrentGeoQuadtree<object>(1, 10, 25);

            for (int i = 0; i < 20; ++i)
                quadtree.InsertElement(null, new GeoCoordinates(0, 0));

            Assert.Equal(0, quadtree.GetLargestSubdivisionLevel());

            for (int i = 0; i < 10; ++i)
            {
                quadtree.EfficientlyReindex();
                Assert.True(quadtree.GetLargestSubdivisionLevel() > i);
            }
        }

        [Fact]
        public void QuadrantsJoinWhenAtLowCapacity()
        {
            ConcurrentGeoQuadtree<object> quadtree = new ConcurrentGeoQuadtree<object>(1, 10, 25);

            List<ConcurrentGeoQuadtree<object>.IElement> elements = new List<ConcurrentGeoQuadtree<object>.IElement>();
            for (int i = 0; i < 20; ++i)
                elements.Add(quadtree.InsertElement(null, new GeoCoordinates(0, 0)));

            Assert.Equal(0, quadtree.GetLargestSubdivisionLevel());
            quadtree.EfficientlyReindex();

            int prevSubdivLevel = quadtree.GetLargestSubdivisionLevel();
            Assert.True(prevSubdivLevel >= 1);

            foreach (var element in elements)
                quadtree.RemoveElement(element);

            quadtree.EfficientlyReindex();
            Assert.True(quadtree.GetLargestSubdivisionLevel() < prevSubdivLevel);
        }

        [Fact]
        public void QuadrantsContinueJoiningWhenAtLowCapacity()
        {
            ConcurrentGeoQuadtree<object> quadtree = new ConcurrentGeoQuadtree<object>(1, 10, 25);

            List<ConcurrentGeoQuadtree<object>.IElement> elements = new List<ConcurrentGeoQuadtree<object>.IElement>();
            for (int i = 0; i < 20; ++i)
                elements.Add(quadtree.InsertElement(null, new GeoCoordinates(0, 0)));

            Assert.Equal(0, quadtree.GetLargestSubdivisionLevel());
            quadtree.EfficientlyReindex();
            quadtree.EfficientlyReindex();
            quadtree.EfficientlyReindex();
            quadtree.EfficientlyReindex();

            int startingLevel = quadtree.GetLargestSubdivisionLevel();
            Assert.True(startingLevel >= 4);

            foreach (var element in elements)
                quadtree.RemoveElement(element);

            for (int i = 0; i < 4; ++i)
            {
                quadtree.EfficientlyReindex();
                Assert.True(quadtree.GetLargestSubdivisionLevel() < startingLevel - i);
            }
        }

        [Fact]
        public void QuadrantDoesntJoinIfWouldBeAboveCapacity()
        {
            ConcurrentGeoQuadtree<object> quadtree = new ConcurrentGeoQuadtree<object>(2, 10, 25);

            List<ConcurrentGeoQuadtree<object>.IElement> elements = new List<ConcurrentGeoQuadtree<object>.IElement>();
            for (int i = 0; i < 10; ++i)
                elements.Add(quadtree.InsertElement(null, new GeoCoordinates(1, 1)));

            for (int i = 0; i < 10; ++i)
                quadtree.InsertElement(null, new GeoCoordinates(-1, -1));

            quadtree.EfficientlyReindex();
            Assert.True(quadtree.GetLargestSubdivisionLevel() >= 1);

            // Remove all but one element from the top-right quadrant but not the
            // bottom-left quadrant.
            foreach (var element in elements.Take(9))
                quadtree.RemoveElement(element);

            quadtree.EfficientlyReindex();
            Assert.True(quadtree.GetLargestSubdivisionLevel() >= 1);
        }

        [Fact]
        public void IntersectionTestFindsCorrectData()
        {
            ConcurrentGeoQuadtree<object> quadtree = new ConcurrentGeoQuadtree<object>(1, 5, 25);
            Random rand = new Random(12345);

            List<ConcurrentGeoQuadtree<object>.IElement> inElements = new List<ConcurrentGeoQuadtree<object>.IElement>();
            for (int i = 0; i < 10; ++i)
                inElements.Add(quadtree.InsertElement(null,
                    new GeoCoordinates(rand.NextDouble() * 5 - 2.5, rand.NextDouble() * 5 - 2.5)));

            List<ConcurrentGeoQuadtree<object>.IElement> outElements = new List<ConcurrentGeoQuadtree<object>.IElement>();
            for (int i = 0; i < 10; ++i)
                outElements.Add(quadtree.InsertElement(null,
                    new GeoCoordinates(rand.NextDouble() * 5 + 40, rand.NextDouble() * 5 + 40)));

            quadtree.EfficientlyReindex();

            // All inElements should be in here, no outElements should be in here.
            Rect inRect = new Rect(-3, -3, 6, 6);

            var foundElements = quadtree.GetElementsInside(inRect.Intersects);

            foreach (var elt in inElements)
                Assert.True(foundElements.Contains(elt));

            foreach (var elt in outElements)
                Assert.False(foundElements.Contains(elt));
        }
    }
}
