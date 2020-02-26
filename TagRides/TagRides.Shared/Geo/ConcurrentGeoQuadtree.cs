using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;

using TagRides.Shared.Utilities;

namespace TagRides.Shared.Geo
{
    /*
    Each quadrant has an RW lock. Here are the rules to prevent various errors.

    Race condition rules:
        1) If you will subdivide or join a quadrant, or if you will modify the
        quadrant's list of data, you must hold an exclusive lock on the quadrant.
        2) If you will read some data about a quadrant that may be modified by
        a different thread, you must hold at least a shared lock on the quadrant.
        This is not necessary in cases where you can guarantee that the relevant
        data on the quadrant cannot change (e.g. the quadrant is an ancestor
        of a locked quadrant, and you are checking the list of children).

    Deadlock prevention rules:
        1) If you lock a quadrant and its parent, lock the parent first.
        2) If you lock siblings, you must lock their parent first.

    The second rule is less obvious than the first, but it is essential for
    preventing a deadlock with a join operation. A join operation will lock
    a parent node and then all of its children. If you hold a lock on one child,
    and try to acquire a lock on a second child, you might deadlock with an
    in-progress join operation that holds a lock on the second child and is
    waiting for a lock on the first.

    The rules above could be made more specific for even more fine-grained
    locking and better optimization, but that's kind of scary.

    How the various operations lock quadrants:
        1) Join gets an exclusive lock on the parent, and then an exclusive lock
            on each of the children. After holding the locks, it can safely check
            if the quadrant can actually be joined (its children all have to be
            leaves).
        2) Subdivide gets an exclusive lock on the target quadrant.
        3) Insert gets an upgradeable lock on the root node, and then it traverses
            down by getting an upgradeable lock on the next node and then releasing
            the upgradeable lock on the previous node. When it reaches the target
            node, it upgrades to an exclusive lock. (an upgradeable lock can only
            be held by one thread, but it doesn't prevent threads from acquiring
            shared locks).
        4) Remove gets an exclusive lock on the quadrant from which an element
            is to be removed.
        5) Move gets a shared lock on the quadrant in which the element resides.
            Most of the time this is enough, but sometimes the element needs to
            move to a new quadrant. In this case, Move releases its shared lock,
            and then acquires a shared lock on the parent and an exclusive lock
            on the original quadrant. Move then traverses up (no locking necessary)
            to find a common ancestor of the element's original and new position,
            and then traverses down using Insert's locking strategy to find the
            target quadrant.
        6) Search (e.g. GetElementsInside(...)) acquires a shared lock on the
            root node and then recursively performs the following steps:
                0)   If this is a leaf node, loop through the data and exit.
                i)   Acquire shared locks on the children.
                ii)  Release the shared lock on the previous quadrant (e.g. root).
                iii) For each child:
                        If the child should not be searched, release the shared lock.
                        If the child should be searched, recurse.

    Elements use a lightweight locking pattern using bools and atomic test-and-set
    operations. There are two properties: IsBusy and IsMarkedForRemove. The only
    element operations that can conflict are Move and Remove, so here is how they
    handle that:
        1) Move aborts if IsMarkedForRemove is true.

            Move atomically tests and sets IsBusy if it was previously false. If
            it was previously true, that means that the element is either being
            Moved or Removed by a different thread, so Move aborts.

            After this, move checks IsMarkedForRemove. If this is set to true,
            Move aborts so as not to make a Remove operation wait.

        2) Remove atomically tests and sets IsMarkedForRemove if it was previously
            false. If it was already true, another thread is removing the element,
            so Remove aborts.

            Then Remove atomically tests and sets IsBusy if it was previously false.
            If it was previously true, Remove yields the thread and keeps trying.

            Since Remove set IsMarkedForRemove, it's guaranteed that no new Move
            or Remove operation will start. Therefore, Remove only has to wait
            as long as the current Move operation continues. Since Move operations
            are generally fast, this is not a long wait.
    */
    /// <summary>
    /// A point quad tree implementation specifically for storing point data on the Earth.
    /// </summary>
    public partial class ConcurrentGeoQuadtree<T>
    {
        public interface IElement
        {
            T Data { get; }
            GeoCoordinates Coordinates { get; }
        }

        /// <summary>
        /// Creates a new concurrent quadtree.
        /// </summary>
        /// <param name="minLeafCapacity">
        ///     Minimum leaf capacity. When a quadrant holds less than this amount
        ///     of data, it may be joined into its parent. This should be no more
        ///     than one fourth of <paramref name="maxLeafCapacity"/>.
        /// </param>
        /// <param name="maxLeafCapacity">
        ///     Maximum leaf capacity. When a quarant holds more than this amount
        ///     of data, it will be subdivided into more quadrants.
        /// </param>
        /// <param name="maxSubdivisionLevel">
        ///     Maximum subdivision level. This is the largest depth of any node
        ///     in the quad tree. Quadrants will not subdivide past this much.
        /// </param>
        /// <exception cref="ApplicationException">
        ///     Thrown if the minimum capacity is too large in comparison to
        ///     the maximum capacity.
        /// </exception>
        public ConcurrentGeoQuadtree(int minLeafCapacity = 1, int maxLeafCapacity = 10, int maxSubdivisionLevel = 25)
        {
            if (minLeafCapacity > maxLeafCapacity / 4)
                throw new ApplicationException($"Given minimum capacity ({minLeafCapacity}) too large." +
                    $" The minimum capacity should be no more than one fourth of the maximum capacity ({maxLeafCapacity}).");

            this.minLeafCapacity = minLeafCapacity;
            this.maxLeafCapacity = maxLeafCapacity;
            this.maxSubdivisionLevel = maxSubdivisionLevel;
        }

        /// <summary>
        /// Gets the elements that are within a certain region.
        /// </summary>
        /// <returns>The elements inside the region defined by <paramref name="intersectionTest"/>.</returns>
        /// <param name="intersectionTest">
        ///     A predicate P that tests for intersection with some region. This
        ///     must satisfy that if B is a rectangle that contains A, then P(A)
        ///     implies P(B). To test if an element satisfies the predicate,
        ///     a zero width and zero height rectangle will be used.
        /// </param>
        public HashSet<IElement> GetElementsInside(Predicate<Rect> intersectionTest)
        {
            rootNode.Lock.EnterSharedLock();

            HashSet<IElement> elements = new HashSet<IElement>();
            CollectElementsInQuadrantThatAreInRegion(rootNode, intersectionTest, elements);

            return elements;
        }

        public IElement InsertElement(T data, GeoCoordinates coordinates)
        {
            rootNode.Lock.EnterUpgradeableLock();

            Quadrant targetQuadrant = FindAndLockTargetLeafQuadrant(rootNode, coordinates);
            targetQuadrant.Lock.EnterExclusiveLock();

            Element element = new Element(data, coordinates);
            targetQuadrant.Data.Add(element);
            elementsToNodes[element] = targetQuadrant;

            ++count;

            // If the quadrant now contains too much data, mark it as a candidate
            // for subdivision.
            if (targetQuadrant.Data.Count > maxLeafCapacity && targetQuadrant.SubdivisionLevel < maxSubdivisionLevel)
            {
                candidatesForSubdivide.TryAdd(targetQuadrant);
            }

            targetQuadrant.Lock.ExitExclusiveLock();
            targetQuadrant.Lock.ExitUpgradeableLock();

            return element;
        }

        /// <summary>
        /// Moves the element to a new position. This will return false and do
        /// nothing if another thread is currently adjusting this element.
        /// </summary>
        /// <returns><c>true</c>, if element was moved, <c>false</c> otherwise.</returns>
        /// <param name="elementRef">Element.</param>
        /// <param name="newCoordinates">New coordinates.</param>
        public bool MoveElement(IElement elementRef, GeoCoordinates newCoordinates)
        {
            Element element = (Element)elementRef;

            if (element.IsMarkedForRemoval)
                return false;

            if (!element.TrySetBusy())
                return false;

            // This check has to happen after TrySetBusy() to avoid a race
            // condition with RemoveElement(). After TrySetBusy() succeeds,
            // no other move or remove operation can run on this element,
            // so we are guaranteed not to move the element after it has been
            // removed. If the element will be removed, we just abort the move.
            if (element.IsMarkedForRemoval)
            {
                element.SetNotBusy();
                return false;
            }

            while (true)
            {
                Quadrant originalQuadrant = GetAndLockQuadrantForElement(element, QuadrantLock.LockType.Shared);

                // Catch tricky issues early.
                Debug.Assert(originalQuadrant.Lock.ThreadOwnsSharedLock);
                Debug.Assert(!originalQuadrant.IsDisconnected);
                Debug.Assert(originalQuadrant.IsLeaf);

                if (originalQuadrant.Contains(newCoordinates))
                {
                    // Case (1): new position is inside the old quadrant. Just change x and y.
                    element.Coordinates = newCoordinates;
                    originalQuadrant.Lock.ExitSharedLock();
                    break;
                }

                // Case (2): new position is inside a new quadrant. Need to move
                // to a new quadrant.

                // The root node should contain all elements, so the above if statement
                // should prevent this assert.
                Debug.Assert(originalQuadrant != rootNode);

                // We have to lock the parent to avoid deadlocking with a join
                // operation if the new quadrant is also a child of the parent.
                if (!SwitchSharedLockToExclusiveLockAndLockParent(originalQuadrant))
                {
                    // If in between releasing the shared lock and acquiring the
                    // exclusive lock the quadrant became disconnected (due to
                    // a join on its parent) or became subdivided, we have to
                    // restart the move.
                    continue;
                }

                Debug.Assert(originalQuadrant.Lock.ThreadOwnsExlusiveLock);
                Debug.Assert(originalQuadrant.Parent.Lock.ThreadOwnsSharedLock);

                // If originalQuadrant was not subdivided or joined (or was
                // subdivided and joined an equal number of times), then element
                // could not have been moved because we set it to busy.
                Debug.Assert(originalQuadrant.Data.Contains(element));

                // Traverse upward to search for the nearest ancestor that contains
                // the new position. No locking is necessary because ancestors
                // cannot be subdivided since they're not leaves, and they cannot
                // be joined because we hold a lock on a child node.
                Quadrant closestContainingAncestor = originalQuadrant.Parent;
                while (!closestContainingAncestor.Contains(newCoordinates))
                {
                    closestContainingAncestor = closestContainingAncestor.Parent;

                    // This only happens if rootNode doesn't contain the new
                    // position, but that cannot happen.
                    Debug.Assert(closestContainingAncestor != null);
                }

                // Traverse downward to target quadrant. Use locking like in an insert operation.

                // It cannot be a leaf node because it is an ancestor node.
                Debug.Assert(!closestContainingAncestor.IsLeaf);

                // Find the leaf quadrant that contains the new coordinates.
                Quadrant targetQuadrant = closestContainingAncestor.GetChildContaining(newCoordinates);
                targetQuadrant.Lock.EnterUpgradeableLock();
                targetQuadrant = FindAndLockTargetLeafQuadrant(targetQuadrant, newCoordinates);
                targetQuadrant.Lock.EnterExclusiveLock();

                // Change the element's coordinates and move it to the new quadrant.
                element.Coordinates = newCoordinates;
                originalQuadrant.Data.Remove(element);
                targetQuadrant.Data.Add(element);
                elementsToNodes[element] = targetQuadrant;

                // If the target quadrant now contains too much data, mark it as a candidate
                // for subdivision.
                if (targetQuadrant.Data.Count > maxLeafCapacity && targetQuadrant.SubdivisionLevel < maxSubdivisionLevel)
                {
                    candidatesForSubdivide.TryAdd(targetQuadrant);
                }

                // If the original quadrant is now nearly empty, mark its parent
                // as a candidate for a join operation.
                if (originalQuadrant.Parent != null && originalQuadrant.Data.Count < minLeafCapacity)
                {
                    candidatesForJoin.TryAdd(originalQuadrant.Parent);
                }

                targetQuadrant.Lock.ExitExclusiveLock();
                targetQuadrant.Lock.ExitUpgradeableLock();

                // Release the locks on the original quadrant and its parent,
                // which were acquired earlier.
                originalQuadrant.Lock.ExitExclusiveLock();
                originalQuadrant.Parent.Lock.ExitSharedLock();
                break;
            }

            element.SetNotBusy();
            return true;
        }

        public void RemoveElement(IElement elementRef)
        {
            Element element = (Element)elementRef;

            // If this returns false, then some thread is already waiting to remove
            // this element or has removed it.
            if (!element.TryMarkForRemoval())
                return;

            // Busy-wait and/or yield until the element is no longer being moved
            // by another thread. Note that because we marked the element for
            // removal, no other thread will attempt to move or remove it, so
            // this thread is next in the queue.
            while (!element.TrySetBusy())
                Thread.Yield();

            Quadrant quadrant = GetAndLockQuadrantForElement(element, QuadrantLock.LockType.Exclusive);

            bool elementRemovedSuccessfully = quadrant.Data.Remove(element);
            elementRemovedSuccessfully &= elementsToNodes.TryRemove(element, out _);

            Debug.Assert(elementRemovedSuccessfully);

            --count;

            // If this removal has made the quadrant nearly empty, mark the parent
            // as a candidate for a join operation.
            if (quadrant.Parent != null && quadrant.Data.Count < minLeafCapacity)
            {
                candidatesForJoin.TryAdd(quadrant.Parent);
            }

            quadrant.Lock.ExitExclusiveLock();

            element.SetNotBusy();
        }

        /// <summary>
        /// Reindexes the quadtree to improve efficiency by subdividing nodes
        /// with too much data and joining together nodes with too little data.
        /// This can run concurrently with all the other operations. It is
        /// recommended to run this every once in a while but with some delay
        /// in between.
        /// </summary>
        public void EfficientlyReindex()
        {
            ICollection<Quadrant> maybeSubdivide = candidatesForSubdivide.GetSnapshotAndClear();
            foreach (Quadrant quadrant in maybeSubdivide)
                Subdivide(quadrant);

            ICollection<Quadrant> maybeJoin = candidatesForJoin.GetSnapshotAndClear();
            foreach (Quadrant quadrant in maybeJoin)
                Join(quadrant);
        }

        /// <summary>
        /// Gets an approximation of the depth of the tree. This will be exact
        /// assuming no join or subdivide operations are happening, but may
        /// be slightly incorrect otherwise.
        /// </summary>
        public int GetLargestSubdivisionLevel()
        {
            return GetApproximateLargestSubdivisionLevel(rootNode);
        }

        public int Count => count;

        Quadrant rootNode = new Quadrant(new Rect(-180, -90, 360, 180));

        ConcurrentDictionary<Element, Quadrant> elementsToNodes = new ConcurrentDictionary<Element, Quadrant>();

        ConcurrentSet<Quadrant> candidatesForSubdivide = new ConcurrentSet<Quadrant>();
        ConcurrentSet<Quadrant> candidatesForJoin = new ConcurrentSet<Quadrant>();

        /// <summary>
        /// The minimum leaf capacity. Should be no more than one fourth of
        /// <see cref="maxLeafCapacity"/>. When a leaf quadrant contains less
        /// than this amount of data, its parent is marked as a candidate for
        /// a join.
        /// </summary>
        int minLeafCapacity;
        int maxLeafCapacity;
        int maxSubdivisionLevel;

        int count = 0;
    }
}
