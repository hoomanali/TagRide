using System;
namespace TagRides.Shared.Utilities
{
    public struct Rect
    {
        public double x;
        public double y;
        public double width;
        public double height;

        /// <summary>
        /// The minimum X value of the rectangle. Setting this will change both
        /// the x value and the width so that only the x = xMin edge moves.
        /// </summary>
        /// <value>The minimum X value.</value>
        public double xMin
        {
            get => x;
            set
            {
                width = xMax - value;
                x = value;
            }
        }

        /// <summary>
        /// The maximum X value of the rectangle. Setting this will adjust just
        /// the width so that only the x = xMax edge moves.
        /// </summary>
        /// <value>The maximum X value.</value>
        public double xMax
        {
            get => x + width;
            set
            {
                width = value - xMin;
            }
        }

        /// <summary>
        /// The minimum Y value of the rectangle. Setting this will change both
        /// the y value and the height so that only the y = yMin edge moves.
        /// </summary>
        /// <value>The minimum Y value.</value>
        public double yMin
        {
            get => y;
            set
            {
                height = yMax - value;
                y = value;
            }
        }

        /// <summary>
        /// The maximum Y value of the rectangle. Setting this will adjust just
        /// the heigh so that only the y = yMax edge moves.
        /// </summary>
        /// <value>The maximum Y value.</value>
        public double yMax
        {
            get => y + height;
            set
            {
                height = value - yMin;
            }
        }

        public Rect(double x, double y, double width, double height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public bool Intersects(Rect otherRect)
        {
            // Use loose inequalities so that rectangles can't intersect at the
            // boundary. This is consistent with the implementation of Contains().
            if (otherRect.xMin >= xMax)
                return false;
            if (xMin >= otherRect.xMax)
                return false;
            if (otherRect.yMin >= yMax)
                return false;
            if (yMin >= otherRect.yMax)
                return false;

            return true;
        }

        public bool Contains(double x, double y)
        {
            // Mix loose and strict inequalities so that a rectangle can be
            // written as the disjoint union of more rectangles (otherwise
            // the union could never be disjoint because edges would intersect).
            return x >= xMin && x < xMax && y >= yMin && y < yMax;
        }
    }
}
