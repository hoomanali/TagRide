using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using TagRides.Shared.RideData;
using TagRides.Shared.RideData.Status;
using TagRides.Server.Utility;
using TagRides.Server.Requests;
using TagRides.Shared.Utilities;
using TagRides.Server.UserData;
using TagRides.Server.Centers;

namespace TagRides.Server.Rides
{
    using PendingRideState = PendingRideStatus.PendingRideState;

    /// <summary>
    /// Represents a matched ride awaiting confirmations.
    /// First the driver must confirm, after which riders can confirm.
    /// These are timed actions which auto reject after an interval.
    /// Once the ride has been successfully confirmed, an active ride will be emitted TODO
    /// </summary>
    public class PendingRide
    {
        #region Events

        /// <summary>
        /// Triggered when the ride state changes
        /// </summary>
        public event Action<PendingRide> StateUpdated;

        public event Action<string, bool, PendingRide> UserTimedOut;

        #endregion

        public RideInfo RideInfo { get; private set; }
        public readonly string Id;
        public string ActiveRideId { get; private set; }

        public PendingRideState State => state;

        /// <summary>
        /// Create a new <see cref="PendingRide"/>
        /// </summary>
        /// <param name="rideInfo">The original ride info of this ride</param>
        /// <param name="offer">The ride offer of this pending ride</param>
        /// <param name="requests">All the ride requests apart of this pending ride</param>
        /// <param name="rideMatcher">Used to create a new route if anything changes durring the confirmation phase</param>
        /// <param name="driverExpireTime">Time in milliseconds the driver has to confirm</param>
        /// <param name="riderExpireTime">Time in milliseconds the riders have to confirm, after the driver confirms</param>
        public PendingRide(RideInfo rideInfo,
                           UserRideOffer offer,
                           ICollection<UserRideRequest> requests,
                           IRideMatcher rideMatcher,
                           double driverExpireTime = 120000,
                           double riderExpireTime = 120000)
        {
            RideInfo = rideInfo;
            Id = IdGenerator.GenerateNewId(this);

            this.offer = offer;
            this.requests = requests;
            this.rideMatcher = rideMatcher;

            userToUserRequest.TryAdd(offer.User, offer);
            foreach (UserRideRequest rr in requests)
                userToUserRequest.TryAdd(rr.User, rr);

            driverTimer = new Timer
            {
                AutoReset = false,
                Interval = driverExpireTime,
                Enabled = false
            };
            riderTimer = new Timer
            {
                AutoReset = false,
                Interval = riderExpireTime,
                Enabled = false
            };

            driverTimer.Elapsed += (a, b) =>
            {
                offer.SetExpired();
                Cancel();
                UserTimedOut?.Invoke(offer.User.UserInfo.UserId, true, this);
            };
            riderTimer.Elapsed += async (a, b) => await DoneGettingConfirmations();
        }

        public async Task InitializeAndPost()
        {
            GameCenter.StartTracking(this);

            driverTimer.Start();

            // Make sure to post this status before we handle any cancellations.
            await PostStatus(driverTimer.Interval);

            // Delay running the OnRide___Canceled() methods until this method finishes.
            using (var delay = new DelayedActions())
            {
                // TODO handle request changes properly
                offer.Changed += OnRideOfferCanceled;

                delay.Run(() => offer.Canceled.RunWhenFired(OnRideOfferCanceled));
                offer.PendingRideMade(this);

                foreach (UserRideRequest rr in requests)
                {
                    // TODO handle request changes properly
                    rr.Changed += OnRideRequestCanceled;

                    delay.Run(() => rr.Canceled.RunWhenFired(OnRideRequestCanceled));
                    rr.PendingRideMade(this);
                }
            }
        }

        public async Task<bool> Confirm(User user)
        {
            if (!userToUserRequest.TryGetValue(user, out UserRequest request))
                return false;

            switch (request)
            {
                case UserRideRequest rideRequest:
                    return await RiderConfirm(rideRequest);
                case UserRideOffer offer:
                    return DriverConfirm(offer);
            }

            throw new Exception("Request not a Ride Request or Offer. That should not be possible...");
        }

        bool DriverConfirm(UserRideOffer offer)
        {
            //TODO there is the case where there are no riders, and we should go ahead and cancel, but if there were never
            //any riders, the pending ride maybe shouldn't have been made in the first place?
            if (offer != this.offer)
                return false;

            driverTimer.Stop();

            //No longer can the user cancel through the offer
            offer.Canceled.Remove(OnRideOfferCanceled);
            offer.Changed -= OnRideOfferCanceled;

            offer.UserConfirmed();

            bool hasPassengers;

            lock (requests)
            {
                if (requests.Any())
                {
                    hasPassengers = true;
                    foreach (var rr in requests)
                        rr.DriverConfirmed();
                }
                else
                {
                    hasPassengers = false;
                }
            }

            PostStatus(riderTimer.Interval).FireAndForgetAsync(Program.ErrorHandler);
            StateUpdated?.Invoke(this);

            if (hasPassengers)
            {
                state = PendingRideState.WaitingOnRiders;
                riderTimer.Start();
            }
            else
                DoneGettingConfirmations().FireAndForgetAsync(Program.ErrorHandler);

            return true;
        }

        async Task<bool> RiderConfirm(UserRideRequest request)
        {
            if (state != PendingRideState.WaitingOnRiders)
                return false;

            lock (requests)
            {
                //Not a part of this
                if (!requests.Contains(request))
                    return false;
            }

            //TODO should return something to indicate the request was already confirmed
            if (confirmedRideRequests.Contains(request))
                return true;

            confirmedRideRequests.Add(request);

            //These events should no longer be invokable anyway
            request.Canceled.Remove(OnRideRequestCanceled);
            request.Changed -= OnRideRequestCanceled;

            request.UserConfirmed();

            bool doneGettingConfirmations = false;
            lock (requests)
            {
                if (confirmedRideRequests.Count == requests.Count)
                    doneGettingConfirmations = true;
            }

            if (doneGettingConfirmations)
                await DoneGettingConfirmations();

            return true;
        }

        #region Event callbacks

        void OnRideRequestCanceled(UserRequest request)
        {
            //if the ride has been confirmed, we don't care about canceled requests
            if (state == PendingRideState.Confirmed) return;

            UserRideRequest rr = request as UserRideRequest;

            //If one already confirmed, they cannot cancel through the request
            if (confirmedRideRequests.Contains(rr)) return;

            rr.Canceled.Remove(OnRideRequestCanceled);
            rr.Changed -= OnRideRequestCanceled;

            originalRideModified = true;

            bool doneGettingConfirmations = false;
            lock (requests)
            {
                requests.Remove(rr);
                userToUserRequest.Remove(rr.User, out UserRequest r);

                if (confirmedRideRequests.Count == requests.Count)
                    doneGettingConfirmations = true;
            }

            if (doneGettingConfirmations)
                DoneGettingConfirmations().FireAndForgetAsync(Program.ErrorHandler);
        }

        void OnRideOfferCanceled(UserRequest _)
        {
            //Can't cancel after confirming (Shouldn't be possible)
            if (state != PendingRideState.WaitingOnDriver) return;

            //First put all the ride requests back into the PendingRideRequestCenter
            lock (requests)
            {
                foreach (var rr in requests)
                {
                    rr.Unmatched();
                    Centers.PendingRideRequestCenter.AddRideRequest(rr);
                }
            }

            Cancel();
        }

        #endregion

        async Task DoneGettingConfirmations()
        {
            IEnumerable<UserRideRequest> passengers = requests;

            lock (requests)
            {
                if (confirmedRideRequests.Count != requests.Count)
                {
                    //Can't just use confirmedRideRequsts because it is unordered; we want to maintain the original ordering.
                    passengers = requests.Where((rr) => confirmedRideRequests.Contains(rr));

                    originalRideModified = true;

                    //Expire the requests of all the others
                    foreach (var rr in requests.Where((r) => !confirmedRideRequests.Contains(r)))
                    {
                        rr.SetExpired();
                        UserTimedOut?.Invoke(rr.User.UserInfo.UserId, false, this);
                    }
                }
            }

            // If all passengers canceled, cancel the offer. If there were no
            // passengers (which should only happen in development), confirm
            // the offer.
            if (!passengers.Any() && requests.Any())
            {
                Cancel();

                return;
            }

            state = PendingRideState.Confirmed;

            if (originalRideModified)
                RideInfo = await rideMatcher.MakeBestRide(offer, passengers);

            //Now we pass the final info to the ActiveRideCenter
            ActiveRideId = await ActiveRideCenter.ActiveRideStarted(RideInfo, offer, confirmedRideRequests);

            PostStatus(0, ActiveRideId).FireAndForgetAsync(Program.ErrorHandler);

            CleanUp();

            StateUpdated?.Invoke(this);
        }

        void Cancel()
        {
            CleanUp();

            state = PendingRideState.Canceled;

            PostStatus(0).FireAndForgetAsync(Program.ErrorHandler);

            StateUpdated?.Invoke(this);
        }

        void CleanUp()
        {
            GameCenter.StopTracking(this);

            if (state == PendingRideState.WaitingOnDriver)
            {
                offer.Canceled.Remove(OnRideOfferCanceled);
                offer.Changed -= OnRideOfferCanceled;
            }
            offer = null;

            lock (requests)
            {
                foreach (UserRideRequest rr in requests)
                {
                    //if it is contained, the events have already been muted
                    if (!confirmedRideRequests.Contains(rr))
                    {
                        rr.Canceled.Remove(OnRideRequestCanceled);
                        rr.Changed -= OnRideRequestCanceled;
                    }
                }

                requests.Clear();
                userToUserRequest.Clear();
            }

            confirmedRideRequests.Clear();

            driverTimer.Enabled = false;
            riderTimer.Enabled = false;
            driverTimer.Dispose();
            riderTimer.Dispose();
            driverTimer = null;
            riderTimer = null;
        }

        async Task PostStatus(double expireTime, string activeRideId = null)
        {
            await Program.DataStore.PostPendingRideStatus(
                new PendingRideStatus
                {
                    Id = Id,
                    State = State,
                    RideInfo = RideInfo,
                    PostTime = DateTime.Now,
                    TimeTillExpire = TimeSpan.FromMilliseconds(expireTime),
                    ActiveRideId = activeRideId,
                    Version = statusVersion++
                });
        }

        private UserRideOffer offer;
        private readonly ICollection<UserRideRequest> requests;
        private readonly IRideMatcher rideMatcher;

        private bool originalRideModified = false;
        private readonly ConcurrentBag<UserRideRequest> confirmedRideRequests = new ConcurrentBag<UserRideRequest>();

        private Timer driverTimer;
        private Timer riderTimer;

        /// <summary>
        /// Map between user and their requests apart of this.
        /// Used for confirmation
        /// </summary>
        private readonly ConcurrentDictionary<User, UserRequest> userToUserRequest = new ConcurrentDictionary<User, UserRequest>();

        PendingRideState state = PendingRideState.WaitingOnDriver;
        int statusVersion = 0;
    }
}
