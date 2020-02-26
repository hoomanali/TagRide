using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using TagRides.Shared.UserProfile;
using TagRides.Shared.RideData;
using TagRides.Shared.Geo;
using TagRides.Server.Requests;
using TagRides.Server.Rides;
using TagRides.Server.Centers;
using TagRides.Shared.Utilities;
using TagRides.Shared.RideData.Status;

namespace TagRides.Server.UserData
{
    using PendingRideState = PendingRideStatus.PendingRideState;
    /// <summary>
    /// Wrapper of <see cref="UserInfo"/> which keeps track of user activity
    /// including any pending requests/offers or active rides.
    /// </summary>
    public class User
    {
        #region Events

        /// <summary>
        /// Occurs when the user's 
        /// </summary>
        public event Action<User, GeoCoordinates> LocationUpdated;
        /// <summary>
        /// Event called when the user is no longer active.
        /// This is when the user has no pending or current activity.
        /// </summary>
        public event Action<User> NoLongerActive;

        public event Action<string, UserRideRelatedRequest> RequestAdded;
        public event Action<string, UserRideRelatedRequest> RequestCanceled;

        /// <summary>
        /// userId, isDriver, pendingRide
        /// </summary>
        public event Action<string, bool, PendingRide> ApartOfPendingRide;
        public event Action<string, bool, PendingRide> ConfirmedPendingRide;


        #endregion

        public readonly UserInfo UserInfo;

        #region Constructor

        //TODO it may be useful to know why this returns null
        /// <summary>
        /// Make a new User object based on the userId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static async Task<User> MakeUser(string userId)
        {
            UserInfo userInfo = await Program.DataStore.GetUserInfo(userId);
            if (userInfo == null) return null;

            return new User(userInfo);
        }

        private User(UserInfo userInfo)
        {
            UserInfo = userInfo;
        }

        #endregion

        #region Properties

        public GeoCoordinates? LastKnownLocation
        {
            get => lastKnowLocation;
            set
            {
                //TODO should we allow null values?
                lastKnowLocation = value;
                if (lastKnowLocation.TryGetValue(out GeoCoordinates location))
                    LocationUpdated?.Invoke(this, location);
            }
        }

        public IEnumerable<string> PendingRideIds =>
            pendingRides.Select((kvp) => kvp.Key).ToList();

        public IEnumerable<string> PendingRideRequestIds =>
            pendingRequests
            .Where((kvp) => kvp.Value.GetType() == typeof(UserRideRequest))
            .Select((kvp) => kvp.Key)
            .ToList();

        public IEnumerable<string> PendingRideOfferIds =>
            pendingRequests
            .Where((kvp) => kvp.Value.GetType() == typeof(UserRideOffer))
            .Select((kvp) => kvp.Key)
            .ToList();

        public IEnumerable<string> ActiveRideIds => activeRides.ToList();

        #endregion

        #region Requests Add/Cancel

        /// <summary>
        /// Try to add a request for this user
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Whether or not the request was added successfully</returns>
        public string AddRideRequest(RideRequest request)
        {
            //TODO For now, only one request at a time
            if (!pendingRequests.IsEmpty)
                return null;

            UserRideRequest newRequest = new UserRideRequest(this, request);

            if (!pendingRequests.TryAdd(newRequest.Id, newRequest))
                return null;

            newRequest.StateUpdated += OnRideRequestUpdate;
            newRequest.ApartOfPendingRide += OnRideRelatedRequestApartOfPendingRide;

            if (!PendingRideRequestCenter.AddRideRequest(newRequest))
            {
                RemoveRequest(newRequest.Id);

                return null;
            }

            RequestAdded?.Invoke(UserInfo.UserId, newRequest);

            return newRequest.Id;
        }

        /// <summary>
        /// Try to add an offer for this user
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Whether or not the offer was added successfully</returns>
        public string AddRideOffer(RideOffer request)
        {
            //TODO For now, only one request at a time
            if (!pendingRequests.IsEmpty)
                return null;

            UserRideOffer newRequest = new UserRideOffer(this, request);

            if (!pendingRequests.TryAdd(newRequest.Id, newRequest))
                return null;

            newRequest.StateUpdated += OnRideOfferUpdate;
            newRequest.ApartOfPendingRide += OnRideRelatedRequestApartOfPendingRide;

            if (!PendingRideRequestCenter.AddRideOffer(newRequest))
            {
                RemoveRequest(newRequest.Id);

                return null;
            }

            RequestAdded?.Invoke(UserInfo.UserId, newRequest);

            return newRequest.Id;
        }

        /// <summary>
        /// Try to cancel a request for this user
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns>Whether or not the request was successfully canceled</returns>
        public bool CancelRequest(string requestId)
        {
            UserRequest request = RemoveRequest(requestId);

            if (request == null) return false;

            request.Cancel();

            if (request is UserRideRelatedRequest rrr)
                RequestCanceled?.Invoke(UserInfo.UserId, rrr);

            return true;
        }

        #endregion

        public async Task<bool> ConfirmPendingRide(string pendingRideId)
        {
            if (!pendingRides.TryGetValue(pendingRideId, out PendingRide pendingRide))
                return false;

            if (await pendingRide.Confirm(this))
            {
                ConfirmedPendingRide?.Invoke(UserInfo.UserId, pendingRide.RideInfo.DriverId == UserInfo.UserId, pendingRide);
                return true;
            }

            return false;
        }

        #region Request event callbacks

        void OnRideOfferUpdate(UserRequest request)
        {
            UserRideOffer rideOffer = request as UserRideOffer;

            switch (rideOffer.State)
            {
                case UserRideOffer.RideOfferState.Matched:
                case UserRideOffer.RideOfferState.Expired:
                    RemoveRequest(request.Id);
                    break;
            }
        }

        void OnRideRequestUpdate(UserRequest request)
        {
            UserRideRequest rideRequest = request as UserRideRequest;

            //We care about very few updates for the request; most updates come from the pendingRide
            switch (rideRequest.State)
            {
                case UserRideRequest.RideRequestState.Matched:
                case UserRideRequest.RideRequestState.Expired:
                    RemoveRequest(request.Id);
                    break;
            }
        }

        void OnRideRelatedRequestApartOfPendingRide(UserRideRelatedRequest request, PendingRide pendingRide)
        {
            pendingRides.TryAdd(pendingRide.Id, pendingRide);

            pendingRide.StateUpdated += OnPendingRideUpdate;

            ApartOfPendingRide?.Invoke(UserInfo.UserId, request is UserRideOffer, pendingRide);
        }

        void OnPendingRideUpdate(PendingRide pendingRide)
        {
            // TODO Create "ActiveRide" class and use event pattern similar to PendingRide. Remove ActiveRide when canceled or no longer in progress.
            if (pendingRide.ActiveRideId != null)
                activeRides.Add(pendingRide.ActiveRideId);

            switch (pendingRide.State)
            {
                case PendingRideState.Confirmed:
                case PendingRideState.Canceled:
                    RemovePendingRide(pendingRide.Id);
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Removes the request and disconnects any <see cref="User"/> event
        /// handlers for the request.
        /// </summary>
        /// <param name="id">The request's ID.</param>
        /// <returns>The removed request, or null if it wasn't found.</returns>
        UserRequest RemoveRequest(string id)
        {
            if (!pendingRequests.TryRemove(id, out UserRequest request))
                return null;

            switch (request)
            {
                case UserRideRequest rideRequest:
                    rideRequest.StateUpdated -= OnRideRequestUpdate;
                    rideRequest.ApartOfPendingRide -= OnRideRelatedRequestApartOfPendingRide;
                    break;
                case UserRideOffer rideOffer:
                    rideOffer.StateUpdated -= OnRideOfferUpdate;
                    rideOffer.ApartOfPendingRide -= OnRideRelatedRequestApartOfPendingRide;
                    break;
            }

            return request;
        }

        PendingRide RemovePendingRide(string id)
        {
            if (!pendingRides.TryRemove(id, out PendingRide ride))
                return null;

            ride.StateUpdated -= OnPendingRideUpdate;

            return ride;
        }

        GeoCoordinates? lastKnowLocation;

        /// <summary>
        /// Maps requestIds with requests
        /// </summary>
        readonly ConcurrentDictionary<string, UserRequest> pendingRequests = new ConcurrentDictionary<string, UserRequest>();
        readonly ConcurrentDictionary<string, PendingRide> pendingRides = new ConcurrentDictionary<string, PendingRide>();
        readonly ConcurrentBag<string> activeRides = new ConcurrentBag<string>();
    }
}