using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using TagRides.Server.Requests;
using TagRides.Server.Rides;
using TagRides.Shared.Geo;
using TagRides.Shared.Utilities;

namespace TagRides.Server.Centers
{
    using RideMatchingQuadtree = ConcurrentGeoQuadtree<PendingRideRequestCenter.MatchableRideRequest>;

    /// <summary>
    /// Manages pending requests for rides, and creates matches.
    /// This includes <see cref="UserRideOffer"/> and <see cref="UserRideRequest"/>.
    /// </summary>
    public static class PendingRideRequestCenter
    {
        /// <summary>
        /// A wrapper for <see cref="UserRideRequest"/> used during matching.
        /// </summary>
        public class MatchableRideRequest
        {
            public readonly UserRideRequest Request;

            public RideMatchingQuadtree.IElement OriginElement { get; private set; }
            public RideMatchingQuadtree.IElement DestinationElement { get; private set; }

            public MatchableRideRequest(UserRideRequest request)
            {
                Request = request;
            }

            public void AddToQuadtree(RideMatchingQuadtree originTree, RideMatchingQuadtree destinationTree)
            {
                OriginElement = originTree.InsertElement(this, Request.RideRequest.Trip.Source);
                DestinationElement = destinationTree.InsertElement(this, Request.RideRequest.Trip.Destination);
            }

        }

        #region Access methods

        public static bool AddRideRequest(UserRideRequest request)
        {
            var matchableRequest = new MatchableRideRequest(request);

            matchableRequest.AddToQuadtree(rideRequestOrigins, rideRequestDestination);
            if (!pendingRequests.TryAdd(request.Id, matchableRequest))
                return false;

            request.Changed += OnRideRequestChanged;
            request.Canceled.RunWhenFired(OnRideRequestCanceled);

            return true;
        }

        public static bool AddRideOffer(UserRideOffer offer)
        {
            // Time-out matching task after 30 seconds.
            CancellationTokenSource tokenSource = new CancellationTokenSource(30000);
            CancellationToken token = tokenSource.Token;

            void OnChanged(UserRequest request)
            {
                //TODO v
                //The only case that needs to be addressed is when a match was made for the old ride offer
                //Otherwise, the matching loop has the new offer
            }

            void OnCanceled(UserRequest request)
            {
                tokenSource.Cancel();
            }

            offer.Changed += OnChanged;
            offer.Canceled.RunWhenFired(OnCanceled);

            Func<Task> computeMatch = ComputeMatch;
            Task.Run(computeMatch, token).OnError(Program.ErrorHandler);

            return true;


            async Task ComputeMatch()
            {
                try
                {
                    try
                    {
                        await MakeRide(offer, token);
                    }
                    finally
                    {
                        // Whatever happens, unsubscribe from the events.
                        offer.Canceled.Remove(OnCanceled);
                        offer.Changed -= OnChanged;
                    }
                }
                catch (TaskCanceledException)
                {
                    // Just in case the operation was canceled for a different
                    // reason (e.g. timeout), cancel the offer. No need to
                    // rethrow exception--nothing is wrong.
                    offer.Cancel();
                }
                catch (Exception)
                {
                    // If anything goes wrong, cancel the ride offer.
                    offer.Cancel();
                    throw;
                }
            }
        }

        public static IEnumerable<GeoCoordinates> OriginLocations =>
            pendingRequests.Select((kvp) => kvp.Value.OriginElement.Coordinates);
        public static IEnumerable<GeoCoordinates> DestinationLocations =>
            pendingRequests.Select((kvp) => kvp.Value.DestinationElement.Coordinates);

        #endregion

        #region Event callbacks

        static void OnRideRequestChanged(UserRequest request)
        {
            throw new NotImplementedException();
        }

        static void OnRideRequestCanceled(UserRequest request)
        {
            // NOTE: UserRequest guarantees that its Canceled event only fires
            // once, so there is no danger of another Canceled event while we
            // are waiting for the lock.

            // Get the rideBuildLock to avoid race conditions with MakeRide.
            lock (rideBuildLock)
                if (pendingRequests.TryGetValue(request.Id, out MatchableRideRequest matchableRequest))
                    RemoveRideRequest(request);
        }

        #endregion

        #region private Methods

        async static Task MakeRide(UserRideOffer offer, CancellationToken cancellationToken)
        {
            bool matched = false;

            do
            {
                // This is the preferred way of cancelling a task. Note that
                // a cancellation token can be made with a timeout.
                cancellationToken.ThrowIfCancellationRequested();

                var potentialRide = await rideMatcher.GetRide(offer, rideRequestOrigins, rideRequestDestination);

                cancellationToken.ThrowIfCancellationRequested();

                // NOTE: C# disallows using await inside a lock. This means that
                // the thread that is running MakeRide() will not enter OnRideRequestCanceled()
                // while it holds this lock (because it won't yield execution).
                // As long as no method call here invokes the Canceled event,
                // this guarantees that the locking will work as expected.
                lock (rideBuildLock)
                {
                    IList<MatchableRideRequest> requests = potentialRide.Item1;

                    // Some requests could have gotten canceled during the matching process.
                    matched = requests.All(req => pendingRequests.ContainsKey(req.Request.Id));

                    if (matched)
                    {
                        // This must happen while we still hold the ride build lock
                        // because because requests could become canceled.
                        foreach (var request in requests)
                            RemoveRideRequest(request.Request);

                        var pendingRide = new PendingRide(potentialRide.Item2,
                                                          offer,
                                                          requests.Select((Mrr) => Mrr.Request).ToList(),
                                                          rideMatcher);

                        pendingRide.InitializeAndPost().FireAndForgetAsync(Program.ErrorHandler);
                    }
                }
            } while (!matched);
        }

        static void RemoveRideRequest(UserRequest request)
        {
            if (!pendingRequests.TryRemove(request.Id, out MatchableRideRequest matchableRequest))
                return;

            rideRequestOrigins.RemoveElement(matchableRequest.OriginElement);
            rideRequestDestination.RemoveElement(matchableRequest.DestinationElement);

            request.Changed -= OnRideRequestChanged;
            request.Canceled.Remove(OnRideRequestCanceled);
        }

        #endregion

        static PendingRideRequestCenter()
        {
            Task.Run(ForeverUpdateQuadtreesAsync).FireAndForgetAsync(Program.ErrorHandler);
        }

        static async Task ForeverUpdateQuadtreesAsync()
        {
            while (true)
            {
                rideRequestOrigins.EfficientlyReindex();
                rideRequestDestination.EfficientlyReindex();

                await Task.Delay(10000);
            }
        }

        static readonly IRideMatcher rideMatcher = new SinglePassengerRideMatcher();

        static readonly RideMatchingQuadtree rideRequestOrigins = new RideMatchingQuadtree();
        static readonly RideMatchingQuadtree rideRequestDestination = new RideMatchingQuadtree();

        /// <summary>
        /// Maps request id to <see cref="MatchableRideRequest"/>
        /// </summary>
        static readonly ConcurrentDictionary<string, MatchableRideRequest> pendingRequests = new ConcurrentDictionary<string, MatchableRideRequest>();

        //Used to lock the final ride building stage
        static readonly object rideBuildLock = new object();
    }
}