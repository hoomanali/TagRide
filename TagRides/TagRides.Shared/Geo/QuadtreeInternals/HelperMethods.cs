using System;
using System.Collections.Generic;
using System.Diagnostics;

using TagRides.Shared.Utilities;

namespace TagRides.Shared.Geo
{
    public partial class ConcurrentGeoQuadtree<T>
    {
        /// <summary>
        /// Collects the elements in quadrant that pass the intersection test.
        /// </summary>
        /// <param name="lockedQuadrant">A quadrant on which the current thread
        /// holds a shared lock.</param>
        /// <param name="intersectionTest">An predicate P such that if a
        /// rectangle B contains a rectangle A, then P(A) implies P(B).</param>
        /// <param name="output">The output collection where to place the elements.</param>
        void CollectElementsInQuadrantThatAreInRegion(Quadrant lockedQuadrant,
            Predicate<Rect> intersectionTest, ICollection<IElement> output)
        {
            Debug.Assert(lockedQuadrant.Lock.ThreadOwnsSharedLock);

            if (lockedQuadrant.IsLeaf)
            {
                foreach (var datum in lockedQuadrant.Data)
                {
                    Rect datumRect = new Rect(datum.Coordinates.Longitude, datum.Coordinates.Latitude, 0, 0);

                    if (intersectionTest(datumRect))
                        output.Add(datum);
                }

                lockedQuadrant.Lock.ExitSharedLock();
            }
            else
            {
                // Lock all children. Do this before releasing the lock
                // on the parent to prevent the parent from performing a join.
                for (int childIdx = 0; childIdx < 4; ++childIdx)
                {
                    lockedQuadrant.GetChild(childIdx).Lock.EnterSharedLock();
                }

                // Exit the lock on the parent before recursion to avoid
                // holding a shared lock on every quadrant all the way down.
                lockedQuadrant.Lock.ExitSharedLock();

                // Check each child.
                for (int childIdx = 0; childIdx < 4; ++childIdx)
                {
                    Quadrant child = lockedQuadrant.GetChild(childIdx);

                    if (intersectionTest(child.Bounds))
                    {
                        CollectElementsInQuadrantThatAreInRegion(child, intersectionTest, output);
                    }
                    else
                    {
                        child.Lock.ExitSharedLock();
                    }
                }
            }
        }

        /// <summary>
        /// Finds the leaf quadrant which contains <paramref name="location"/> by
        /// traversing downward starting at <paramref name="initialQuadrant"/> using
        /// upgradeable locks.
        /// </summary>
        /// <returns>The leaf quadrant containing the location. The current thread
        /// will hold an upgradable lock on this quadrant.</returns>
        /// <param name="initialQuadrant">
        /// The quadrant from which to start downward traversal. This must contain
        /// <paramref name="location"/> and the current thread should hold an
        /// upgradeable lock on it. This lock will be released unless this quadrant
        /// happens to be the return value.
        /// </param>
        /// <param name="location">The location for which to find a leaf quadrant.</param>
        Quadrant FindAndLockTargetLeafQuadrant(Quadrant initialQuadrant, GeoCoordinates location)
        {
            Debug.Assert(initialQuadrant.Contains(location));
            Debug.Assert(initialQuadrant.Lock.ThreadOwnsUpgradeableLock);

            Quadrant targetQuadrant = initialQuadrant;

            while (!targetQuadrant.IsLeaf)
            {
                Quadrant next = targetQuadrant.GetChildContaining(location);
                next.Lock.EnterUpgradeableLock();
                targetQuadrant.Lock.ExitUpgradeableLock();

                targetQuadrant = next;
            }

            Debug.Assert(targetQuadrant.Contains(location));

            return targetQuadrant;
        }

        /// <summary>
        /// Gets the leaf quadrant that contains the element.
        /// </summary>
        /// <returns>The and lock quadrant for element.</returns>
        /// <param name="element">The element for which to find the quadrant. This element must be inside some quadrant.</param>
        /// <param name="lockType">The type of lock to acquire on the quadrant.</param>
        Quadrant GetAndLockQuadrantForElement(Element element, QuadrantLock.LockType lockType)
        {
            bool elementExistedPreviously = elementsToNodes.TryGetValue(element, out Quadrant node);
            Debug.Assert(elementExistedPreviously);

            node.Lock.EnterLock(lockType);

            // At this point, the node cannot subdivide or be joined into its parent,
            // however this may have happened in between the previous two lines,
            // and if it did, then the element got moved and we have to try again.
            // This will work eventually assuming that subdivisions and joins
            // happen reasonably infrequently.

            while (node.IsDisconnected || !node.IsLeaf || !node.Data.Contains(element))
            {
                node.Lock.ExitLock(lockType);

                elementExistedPreviously = elementsToNodes.TryGetValue(element, out node);
                Debug.Assert(elementExistedPreviously);

                node.Lock.EnterLock(lockType);
            }

            return node;
        }

        /// <summary>
        /// Switches a shared lock on a quadrant to an exclusive lock, returning
        /// true if the quadrant was not disconnected and kept the same leaf status
        /// in between the two operations. Also acquires a shared lock on the
        /// quadrant's parent. If the method returns false, it releases the lock
        /// on the given quadrant and its parent.
        /// </summary>
        /// <remarks>
        /// This method acquires a lock on the parent before acquiring a lock
        /// on <paramref name="quadrant"/> to avoid a deadlock with the join
        /// operation, which acquires a lock on a parent first and then on
        /// all of its children.
        /// </remarks>
        /// <returns><c>true</c>, if the quadrant did not become disconnected or
        /// changed from a leaf to a non-leaf or vice-versa, <c>false</c> otherwise
        /// (in which case the lock was released and the parent is not locked).</returns>
        /// <param name="quadrant">The non-root quadrant for which the current thread
        /// holds a shared lock. If this method returns true, the current thread
        /// will hold an exclusive lock on this and a shared lock on its parent.
        /// Otherwise, the thread will hold no lock on this or its parent.</param>
        bool SwitchSharedLockToExclusiveLockAndLockParent(Quadrant quadrant)
        {
            Debug.Assert(quadrant.Lock.ThreadOwnsSharedLock);
            Debug.Assert(!quadrant.IsDisconnected);

            bool wasLeaf = quadrant.IsLeaf;

            Quadrant parent = quadrant.Parent;
            Debug.Assert(parent != null, "Quadrant should have a parent.");

            quadrant.Lock.ExitSharedLock();
            parent.Lock.EnterSharedLock();
            quadrant.Lock.EnterExclusiveLock();

            // After releasing the shared lock, the quadrant could have been
            // joined into its parent, or the quadrant could have subdivided.
            if (quadrant.IsDisconnected || (quadrant.IsLeaf != wasLeaf))
            {
                quadrant.Lock.ExitExclusiveLock();
                parent.Lock.ExitSharedLock();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the depth of the tree rooted at this quadrant. For efficiency,
        /// this performs no locking. Because of this, the return value should be
        /// regarded as an approximation.
        /// </summary>
        int GetApproximateLargestSubdivisionLevel(Quadrant quadrant)
        {
            if (quadrant.IsLeaf)
                return 0;

            int maxLevel = 1;

            for (int childIdx = 0; childIdx < 4; ++childIdx)
            {
                try
                {
                    int childLevel = 1 + GetApproximateLargestSubdivisionLevel(quadrant.GetChild(childIdx));

                    if (childLevel > maxLevel)
                        maxLevel = childLevel;
                }
                catch (ApplicationException)
                {
                    // This can happen if the quadrant becomes a leaf due to
                    // a join operation (since we didn't lock it).
                    break;
                }
            }

            return maxLevel;
        }

        /// <summary>
        /// Subdivides the specified quadrant if it is a valid candidate for
        /// subdivision. This will acquire and release an exclusive lock on
        /// the quadrant.
        /// </summary>
        /// <param name="quadrant">The quadrant which to subdivide.</param>
        void Subdivide(Quadrant quadrant)
        {
            quadrant.Lock.EnterExclusiveLock();

            SubdivideWithoutLocking(quadrant);

            quadrant.Lock.ExitExclusiveLock();
        }

        void SubdivideWithoutLocking(Quadrant quadrant)
        {
            if (quadrant.SubdivisionLevel >= maxSubdivisionLevel)
                return;

            if (quadrant.Data.Count <= maxLeafCapacity)
                return;

            if (!quadrant.TrySubdivide())
                return;

            for (int childIdx = 0; childIdx < 4; ++childIdx)
            {
                Quadrant child = quadrant.GetChild(childIdx);

                foreach (var datum in child.Data)
                {
                    elementsToNodes[datum] = child;
                }

                if (child.Data.Count > maxLeafCapacity)
                {
                    SubdivideWithoutLocking(child);
                }
            }
        }

        bool Join(Quadrant quadrant)
        {
            // Always lock from top to bottom to avoid deadlocking.
            quadrant.Lock.EnterExclusiveLock();

            try
            {
                if (!quadrant.TryJoin(maxLeafCapacity))
                    return false;

                foreach (var datum in quadrant.Data)
                {
                    elementsToNodes[datum] = quadrant;
                }

                if (quadrant.Parent != null && quadrant.Data.Count < minLeafCapacity)
                    candidatesForJoin.TryAdd(quadrant.Parent);

                return true;
            }
            finally
            {
                quadrant.Lock.ExitExclusiveLock();
            }
        }
    }
}