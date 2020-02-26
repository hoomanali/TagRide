using System.Threading;

namespace TagRides.Shared.Geo
{
    public partial class ConcurrentGeoQuadtree<T>
    {
        class Element : IElement
        {
            public T Data { get; }
            public GeoCoordinates Coordinates { get; internal set; }

            internal Element(T data, GeoCoordinates coordinates)
            {
                Data = data;
                Coordinates = coordinates;
                isBusy = 0;
                isMarkedForRemoval = 0;
            }

            /// <summary>
            /// Atomically marks the element for removal. Returns false if it was
            /// already marked, and true otherwise.
            /// </summary>
            internal bool TryMarkForRemoval()
            {
                return Interlocked.CompareExchange(ref isMarkedForRemoval, 1, 0) == 0;
            }

            /// <summary>
            /// Sets the element to the "busy" state atomically. Returns true on success,
            /// false if the element was already busy.
            /// </summary>
            internal bool TrySetBusy()
            {
                return Interlocked.CompareExchange(ref isBusy, 1, 0) == 0;
            }

            /// <summary>
            /// Sets the element to the "not busy" state. The caller must make sure that
            /// only the thread that succeeded on `TrySetBusy()` calls this.
            /// </summary>
            internal void SetNotBusy()
            {
                isBusy = 0;
            }

            internal bool IsBusy => isBusy == 1;
            internal bool IsMarkedForRemoval => isMarkedForRemoval == 1;

            private int isBusy;
            private int isMarkedForRemoval;
        }
    }
}
