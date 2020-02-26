using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagRides.Server.Rides;
using TagRides.Server.UserData;
using TagRides.Server.Utility;
using TagRides.Shared.Game;
using TagRides.Shared.UserProfile;
using TagRides.Shared.Utilities;
using TagRides.Shared.RideData.Status;
using Newtonsoft.Json;
using TagRides.Shared.RideData;
using TagRides.Server.Requests;

namespace TagRides.Server.Centers
{
    public static class GameCenter
    {
        static GameCenter()
        {
            ActiveRideCenter.RideStarted += OnRideStarted;
            ActiveRideCenter.RideEnded += OnRideEnded;
            ActiveRideCenter.UserJoinedRide += OnUserInRide;
            ActiveRideCenter.UserFinishedRide += OnUserFinishedRide;
            ActiveRideCenter.UserCanceledRide += OnUserCanceledRideInProgress;
        }

        #region EffectsQueue

        class EffectsQueue
        {
            public EffectsQueue(string userId, IEnumerable<GameInfoEffectBase> effects = null)
            {
                this.userId = userId;

                if (effects == null)
                    this.effects = new ConcurrentQueue<GameInfoEffectBase>();
                else
                    this.effects = new ConcurrentQueue<GameInfoEffectBase>(effects);
            }

            public void AddEffect(GameInfoEffectBase effect)
            {
                effects.Enqueue(effect);
            }

            /// <summary>
            /// Dequeues all effects up to and including the one with Id <paramref name="lastEffectId"/>
            /// DOES NOT POST THE NEW QUEUE
            /// </summary>
            /// <param name="lastEffectId">The last effect that is safe to remove</param>
            /// <returns>Whether on not the operation was successful</returns>
            public bool ClearTo(string lastEffectId)
            {
                if (effects.Where((e) => e.Id == lastEffectId).Count() == 0)
                    return false;
                
                while (true)
                {
                    if (!effects.TryDequeue(out GameInfoEffectBase e))
                        return false;
                    if (e.Id == lastEffectId)
                        return true;
                }
            }

            /// <summary>
            /// Store the effect queue to the data store
            /// </summary>
            public async Task PostEffects()
            {
                await Program.DataStore.PostGameInfoEffects(userId, effects);
            }

            readonly string userId;
            readonly ConcurrentQueue<GameInfoEffectBase> effects;
        }

        #endregion

        #region Start/Stop tracking

        public static void StartTracking(User user)
        {
            user.RequestAdded += OnRequest;
            user.ApartOfPendingRide += OnUserApartOfPendingRide;
            user.ConfirmedPendingRide += OnUserConfirmedRide;
        }

        public static void StopTracking(User user)
        {
            user.RequestAdded -= OnRequest;
            user.ApartOfPendingRide -= OnUserApartOfPendingRide;
            user.ConfirmedPendingRide -= OnUserConfirmedRide;
        }

        public static void StartTracking(PendingRide pendingRide)
        {
            pendingRide.UserTimedOut += OnUserNotConfirmedRide;
        }
        
        public static void StopTracking(PendingRide pendingRide)
        {
            pendingRide.UserTimedOut -= OnUserNotConfirmedRide;
        }

        #endregion

        #region User Actions

        public static async Task<bool> ClearReadEffects(string userId, string lastReadEffectId)
        {
            if (effectsQueues.TryGetValue(userId, out EffectsQueue effectQueue))
            {
                if (!effectQueue.ClearTo(lastReadEffectId))
                    return false;
                await effectQueue.PostEffects();
                return true;
            }

            //This method could be called while the user is not being tracked (not active) so pull one if it's there
            IEnumerable<GameInfoEffectBase> effects = await Program.DataStore.GetGameInfoEffects(userId);
            effectQueue = new EffectsQueue(userId, effects);

            //If there was no effets stored on the server, this will always return false, as expected
            if (!effectQueue.ClearTo(lastReadEffectId))
                return false;

            await effectQueue.PostEffects();
            return true;
        }

        /// <summary>
        /// Gives the given user a rating.
        /// </summary>
        /// <param name="userId">User to rate</param>
        /// <param name="rating">The rating on a scale of 0 - 1</param>
        /// <param name="shouldPost">Whether or not the resulting game info should be posted to the database</param>
        /// <returns></returns>
        public static async Task<bool> GiveRating(string userId, double rating, bool shouldPost = false)
        {
            if (rating < 0 || rating > 1)
                return false;

            GameInfo info = await GetGameInfo(userId);
            info.Rating.GiveRating(rating);

            if (shouldPost)
                await Program.DataStore.PostGameInfo(userId, info);

            return true;
        }

        public static async Task<bool> SetFaction(string userId, string factionName)
        {
            if (Program.TagRideProperties.GetFaction(factionName) == null)
                return false;

            GameInfo info = await GetGameInfo(userId);
            if (!string.IsNullOrEmpty(info.Faction) && !info.CanChangeFaction)
                return false;

            info.Faction = factionName;
            
            await Program.DataStore.PostGameInfo(userId, info);
            return true;
        }

        public static async Task<bool> RemoveFaction(string userId, bool canChangeAgain = true)
        {
            GameInfo info = await GetGameInfo(userId);
            info.Faction = null;
            info.CanChangeFaction = canChangeAgain;

            await Program.DataStore.PostGameInfo(userId, info);
            return true;
        }

        #endregion

        #region Event handlers

        #region Requests
        
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// When a user does a request
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="request"></param>
        static async Task OnRequestAsync(string userId, UserRideRelatedRequest request)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {

        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// When a user cancels a request
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="request"></param>
        static async Task OnRequestCanceledAsync(string userId, UserRideRelatedRequest request)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {

        }

        #endregion

        #region Pending ride

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// When user is apart of a pending ride
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="isDriver"></param>
        /// <param name="pendingRide"></param>
        static async Task OnUserApartOfPendingRideAsync(string userId, bool isDriver, PendingRide pendingRide)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {

        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// When a user confirms a pending ride
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="isDriver"></param>
        /// <param name="pendingRide"></param>
        static async Task OnUserConfirmedRideAsync(string userId, bool isDriver, PendingRide pendingRide)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {

        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// When a user fails to confirm a pending ride, and is timed out.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="isDriver"></param>
        /// <param name="pendingRide"></param>
        static async Task OnUserNotConfirmedRideAsync(string userId, bool isDriver, PendingRide pendingRide)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {

        }

        #endregion

        #region Active Ride

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// The ride has started
        /// </summary>
        /// <param name="rideId"></param>
        static async Task OnRideStartedAsync(ActiveRideStatus status)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {

        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// When a user cancels a ride in progress
        /// </summary>
        /// <param name="rideId"></param>
        /// <param name="userId"></param>
        /// <param name="isDriver"></param>
        static async Task OnUserCanceledRideInProgressAsync(ActiveRideStatus status, string userId, bool isDriver)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {

        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// When a user physically gets in a ride
        /// </summary>
        /// <param name="rideId"></param>
        /// <param name="userId"></param>
        static async Task OnUserInRideAsync(ActiveRideStatus status, string userId)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {

        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// When a user finishes a ride (The whole ride might not be finished yet, but this user is done with it)
        /// </summary>
        /// <param name="rideId"></param>
        /// <param name="userId"></param>
        /// <param name="isDriver"></param>
        static async Task OnUserFinishedRideAsync(ActiveRideStatus status, string userId, bool isDriver)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {

        }


        /// <summary>
        /// When the whole ride ends
        /// </summary>
        /// <param name="rideId"></param>
        static async Task OnRideEndedAsync(ActiveRideStatus status)
        {
            //Reward the driver:
            {
                string driverId = status.RideInfo.DriverId;

                //Handle any equipped item
                GameItem item = await ValidateAndTakeEquippedItem(driverId, status.DriverGameElements);
                GameMultipliers multipliers = new GameMultipliers(item);

                await AddNewEffect(driverId,
                    new CompoundEffect(
                        new GameInfoEffectBase[]
                        {
                        new LevelEffect((int)(1000 * multipliers.Level), "", "", ""),
                        new KingdomEffect((int)(500 * multipliers.Kingdom), "", "", ""),
                        new ItemEffect(
                            new GameItem(
                                "Sword",
                                GameItem.Effect.Level,
                                2,
                                10,
                                1,
                                flavorText: "It's sharp!"), "", "", "")
                        },
                        "Gave a ride",
                        "Gave a chocolate ride",
                        IdGenerator.GenerateNewId(status)
                    ), true
                );
            }

            //Reward the passengers
            foreach (var kvp in status.RidersState)
            {
                //Only riders who finished the ride
                //TODO this assuems that all riders either canceled, or got dropped off
                if (kvp.Value == ActiveRideStatus.RiderState.Canceled)
                    continue;

                GameItem item = null;
                if (status.RidersGameElements.TryGetValue(kvp.Key, out RequestGameElements gameElements))
                    item = await ValidateAndTakeEquippedItem(kvp.Key, gameElements);

                GameMultipliers multipliers = new GameMultipliers(item);

                await AddNewEffect(kvp.Key,
                    new CompoundEffect(
                        new GameInfoEffectBase[]
                        {
                            new LevelEffect((int)(200 * multipliers.Level), "", "", ""),
                            new ItemEffect(
                                new GameItem(
                                    "Ticket stub",
                                    GameItem.Effect.None,
                                    0,
                                    startCount: 1,
                                    flavorText: "What a nice ride it was"), "", "", "")
                        },
                        "Took a ride",
                        "Took a strawberry ride",
                        IdGenerator.GenerateNewId(status)
                    ), true
                );
            }
        }

        #endregion

        #region Sync Wrappers

        #region Requests
        
        static void OnRequest(string userId, UserRideRelatedRequest request)
        {
            OnRequestAsync(userId, request).FireAndForgetAsync(Program.ErrorHandler);
        }
        
        static void OnRequestCanceled(string userId, UserRideRelatedRequest request)
        {
            OnRequestCanceledAsync(userId, request).FireAndForgetAsync(Program.ErrorHandler);
        }

        #endregion

        #region Pending ride
        
        static void OnUserApartOfPendingRide(string userId, bool isDriver, PendingRide pendingRide)
        {
            OnUserApartOfPendingRideAsync(userId, isDriver, pendingRide).FireAndForgetAsync(Program.ErrorHandler);
        }
        static void OnUserConfirmedRide(string userId, bool isDriver, PendingRide pendingRide)
        {
            OnUserConfirmedRideAsync(userId, isDriver, pendingRide).FireAndForgetAsync(Program.ErrorHandler);
        }
        static void OnUserNotConfirmedRide(string userId, bool isDriver, PendingRide pendingRide)
        {
            OnUserNotConfirmedRideAsync(userId, isDriver, pendingRide).FireAndForgetAsync(Program.ErrorHandler);
        }

        #endregion

        #region Active Ride

        static void OnRideStarted(ActiveRideStatus status)
        {
            OnRideStartedAsync(status).FireAndForgetAsync(Program.ErrorHandler);
        }
        static void OnUserCanceledRideInProgress(ActiveRideStatus status, string userId, bool isDriver)
        {
            OnUserCanceledRideInProgressAsync(status, userId, isDriver).FireAndForgetAsync(Program.ErrorHandler);
        }
        static void OnUserInRide(ActiveRideStatus status, string userId)
        {
            OnUserInRideAsync(status, userId).FireAndForgetAsync(Program.ErrorHandler);
        }
        
        static void OnUserFinishedRide(ActiveRideStatus status, string userId, bool isDriver)
        {
            OnUserFinishedRideAsync(status, userId, isDriver).FireAndForgetAsync(Program.ErrorHandler);
        }
        static void OnRideEnded(ActiveRideStatus status)
        {
            OnRideEndedAsync(status).FireAndForgetAsync(Program.ErrorHandler);
        }

        #endregion

        #endregion

        #endregion

        #region Private methods

        /// <summary>
        /// Applies the effect to the user's <see cref="GameInfo"/>, and <see cref="EffectsQueue"/>
        /// </summary>
        /// <param name="user"></param>
        /// <param name="effect"></param>
        /// <param name="shouldPost">If true, then evenything will be posted to the datastore</param>
        /// <returns></returns>
        static async Task AddNewEffect(string userId, GameInfoEffectBase effect, bool shouldPost = false)
        {
            GameInfo gameInfo = await GetGameInfo(userId);
            gameInfo += effect;

            if (shouldPost)
                await Program.DataStore.PostGameInfo(userId, gameInfo);

            EffectsQueue effectsQueue = await GetEffectQueue(userId);

            effectsQueue.AddEffect(effect);

            if (shouldPost)
                await effectsQueue.PostEffects();
        }

        static async Task<GameInfo> GetGameInfo(string userId)
        {
            if (gameInfos.TryGetValue(userId, out GameInfo gameInfo))
                return gameInfo;

            gameInfo = await Program.DataStore.GetGameInfo(userId);

            if (gameInfo == null)
                gameInfo = new GameInfo();

            gameInfos.TryAdd(userId, gameInfo);
            return gameInfo;
        }

        static async Task<EffectsQueue> GetEffectQueue(string userId)
        {
            if (effectsQueues.TryGetValue(userId, out EffectsQueue queue))
                return queue;

            IEnumerable<GameInfoEffectBase> effects = await Program.DataStore.GetGameInfoEffects(userId);
            queue = new EffectsQueue(userId, effects);

            effectsQueues.TryAdd(userId, queue);
            return queue;
        }

        /// <summary>
        /// Ensures the user does have at least one of the item specified in <see cref="RequestGameElements.EquippedItem"/>
        /// and reduces that item stack by one. Posting the resulting game info if <paramref name="shouldPost"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="gameElements">Can be null</param>
        /// <param name="shouldPost"></param>
        /// <returns></returns>
        static async Task<GameItem> ValidateAndTakeEquippedItem(
            string userId, 
            RequestGameElements gameElements, 
            bool shouldPost = false)
        {
            if (gameElements == null || string.IsNullOrEmpty(gameElements.EquippedItemName))
                return null;

            GameInfo gameInfo = await GetGameInfo(userId);

            GameItem ret = gameInfo.Inventory.TakeOne(gameElements.EquippedItemName);

            if (shouldPost)
                await Program.DataStore.PostGameInfo(userId, gameInfo);

            return ret;
        }

        #endregion

        //TODO currently these will never clear anything old. Eventually, we need a little garbage collection for things
        //     that havent been used in a long time
        static readonly ConcurrentDictionary<string, EffectsQueue> effectsQueues = new ConcurrentDictionary<string, EffectsQueue>();
        static readonly ConcurrentDictionary<string, GameInfo> gameInfos = new ConcurrentDictionary<string, GameInfo>();
    }
}