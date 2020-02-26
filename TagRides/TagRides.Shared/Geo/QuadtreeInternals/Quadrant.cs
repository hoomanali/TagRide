using System;
using System.Linq;
using System.Collections.Generic;
using TagRides.Shared.Geo;
using TagRides.Shared.Utilities;

namespace TagRides.Shared.Geo
{
    public partial class ConcurrentGeoQuadtree<T>
    {
        class Quadrant
        {
            public Rect Bounds { get; }

            public bool IsDisconnected { get; private set; } = false;

            public QuadrantLock Lock { get; } = new QuadrantLock();

            public int SubdivisionLevel { get; }

            public Quadrant Parent
            {
                get
                {
                    if (parentRef.TryGetTarget(out Quadrant parent))
                        return parent;

                    return null;
                }
            }

            /// <summary>
            /// The data contained within this node. This is non-null if and
            /// only if <see cref="IsLeaf"/> is true.
            /// </summary>
            /// <value>The data.</value>
            public List<Element> Data { get; private set; }

            public bool IsLeaf => children == null;

            public Quadrant(Rect bounds, Quadrant parent = null)
            {
                Bounds = bounds;
                Data = new List<Element>();
                children = null;

                if (parent != null)
                {
                    parentRef = new WeakReference<Quadrant>(parent);
                    SubdivisionLevel = parent.SubdivisionLevel + 1;
                }
                else
                {
                    parentRef = new WeakReference<Quadrant>(null);
                    SubdivisionLevel = 0;
                }
            }

            /// <summary>
            /// Checks whether this quad covers some of the area containing <paramref name="geoRect"/>.
            /// </summary>
            /// <returns>True if this intersects the given rectangle, false otherwise.</returns>
            /// <param name="geoRect">The rectangle for which to test for intersection.</param>
            public bool Intersects(Rect geoRect)
            {
                return GeoRectUtils.Intersect(Bounds, geoRect);
            }

            public bool Contains(GeoCoordinates coordinates)
            {
                return GeoRectUtils.GeoContains(Bounds, coordinates);
            }

            /// <summary>
            /// Gets the child at the given index.
            /// </summary>
            /// <returns>The child.</returns>
            /// <param name="idx">Index of the child. Must be between 0 and 3.</param>
            /// <exception cref="ApplicationException">Thrown if this quadrant is a leaf.</exception>
            /// <exception cref="IndexOutOfRangeException">Thrown if the index is not one of 0,1,2,3.</exception>
            public Quadrant GetChild(int idx)
            {
                if (IsLeaf)
                    throw new ApplicationException("Tried to get the child of a leaf node.");
                if (idx < 0 || idx > 4)
                    throw new IndexOutOfRangeException("Child index must be between 0 and 3 inclusive.");

                return children[idx];
            }

            public Quadrant GetChildContaining(GeoCoordinates coordinates)
            {
                double subWidth = Bounds.width / 2;
                double subHeight = Bounds.height / 2;

                if (coordinates.Longitude >= Bounds.x + subWidth)
                {
                    if (coordinates.Latitude >= Bounds.y + subHeight)
                        return children[0];
                    else
                        return children[3];
                }
                else
                {
                    if (coordinates.Latitude >= Bounds.y + subHeight)
                        return children[1];
                    else
                        return children[2];
                }
            }

            /// <summary>
            /// Subdivides this quadrant into four quadrants. The caller is
            /// responsible for acquiring an exclusive lock on this quadrant.
            /// This method returns false if the quadrant is not a valid candidate
            /// for subdivision.
            /// </summary>
            public bool TrySubdivide()
            {
                if (IsDisconnected || !IsLeaf)
                    return false;

                children = new Quadrant[4];

                double subWidth = Bounds.width / 2;
                double subHeight = Bounds.height / 2;

                Rect bottomLeft = new Rect(Bounds.x, Bounds.y, subWidth, subHeight);
                Rect topLeft = new Rect(Bounds.x, Bounds.y + subHeight, subWidth, subHeight);
                Rect topRight = new Rect(Bounds.x + subWidth, Bounds.y + subHeight, subWidth, subHeight);
                Rect bottomRight = new Rect(Bounds.x + subWidth, Bounds.y, subWidth, subHeight);

                children[0] = new Quadrant(topRight, this);
                children[1] = new Quadrant(topLeft, this);
                children[2] = new Quadrant(bottomLeft, this);
                children[3] = new Quadrant(bottomRight, this);

                foreach (var datum in Data)
                {
                    GetChildContaining(datum.Coordinates).Data.Add(datum);
                }

                Data = null;
                return true;
            }

            /// <summary>
            /// Makes this quadrant a leaf by aggregating all of the data stored
            /// in its children. The caller is responsible for acquiring an
            /// exclusive lock on this quadrant. This method will return false
            /// if the quadrant is not a valid candidate for a join.
            /// <para>
            /// This method will acquire exclusive locks on all of the children
            /// of the quadrant.
            /// </para>
            /// </summary>
            public bool TryJoin(int maxCapacity)
            {
                if (IsDisconnected || IsLeaf)
                    return false;

                for (int childIdx = 0; childIdx < 4; ++childIdx)
                {
                    Quadrant child = children[childIdx];

                    child.Lock.EnterExclusiveLock();

                    if (!child.IsLeaf)
                    {
                        // Unlock all of the previous children and abort. Cannot
                        // join if all children aren't leaves.

                        for (; childIdx >= 0; --childIdx)
                            children[childIdx].Lock.ExitExclusiveLock();

                        return false;
                    }
                }

                int capacity = 0;
                for (int i = 0; i < 4; ++i)
                    capacity += children[i].Data.Count;

                // Don't join if the quadrant's capacity would be above the
                // accepted level.
                if (capacity > maxCapacity)
                {
                    for (int childIdx = 0; childIdx < 4; ++childIdx)
                        children[childIdx].Lock.ExitExclusiveLock();
                    return false;
                }

                Data = new List<Element>(maxCapacity * 2);
                for (int i = 0; i < 4; ++i)
                {
                    Data.AddRange(children[i].Data);
                    children[i].IsDisconnected = true;
                }

                for (int childIdx = 0; childIdx < 4; ++childIdx)
                    children[childIdx].Lock.ExitExclusiveLock();

                children = null;

                return true;
            }

            Quadrant[] children;
            WeakReference<Quadrant> parentRef;
        }
    }
}
