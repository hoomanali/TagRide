using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using TagRides.Shared.Geo;
using TagRides.Shared.Utilities;
using TagRides.Shared.UserProfile;

namespace TagRides.Server.UserData
{
    using UserElement = ConcurrentGeoQuadtree<User>.IElement;

    public static class Users
    {
        public static async Task<User> GetUser(string userId)
        {
            User user = null;
            if (!userIdToQuadtreeElement.TryGetValue(userId, out UserElement userElement))
            {
                user = await User.MakeUser(userId);
                if (user == null) return null;

                UserElement newUserElement = activeUsers.InsertElement(user, user.LastKnownLocation.GetValueOrDefault());

                if (!userIdToQuadtreeElement.TryAdd(userId, newUserElement))
                {
                    //TODO what went wrong? And deal with it better
                    return null;
                }

                user.NoLongerActive += OnUserInactive;
                user.LocationUpdated += OnUserLocationUpdated;

                Centers.GameCenter.StartTracking(user);
            }
            else
                user = userElement.Data;

            return user;
        }

        public static IEnumerable<User> GetActiveUsersIn(Predicate<Rect> inTest)
        {
            return activeUsers.GetElementsInside(inTest).Select((ele) => ele.Data);
        }

        /// <summary>
        /// If the user of Id <paramref name="userId"/> is loaded, it's <see cref="Shared.UserProfile.UserInfo"/> will
        /// be updated to the version on the datastore
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>true if the user is loaded and was updated</returns>
        public static async Task<bool> UpdateUserIfLoaded(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            if (userIdToQuadtreeElement.TryGetValue(userId, out UserElement value))
            {
                UserInfo dbUserInfo = await Program.DataStore.GetUserInfo(userId);
                return true;
            }

            return false;
        }

        static void OnUserInactive(User user)
        {
            user.NoLongerActive -= OnUserInactive;
            user.LocationUpdated -= OnUserLocationUpdated;
            if (userIdToQuadtreeElement.TryRemove(user.UserInfo.UserId, out UserElement userElement))
            {
                activeUsers.RemoveElement(userElement);
            }

            Centers.GameCenter.StopTracking(user);
        }

        static void OnUserLocationUpdated(User user, GeoCoordinates newLocation)
        {
            if (userIdToQuadtreeElement.TryGetValue(user.UserInfo.UserId, out UserElement element))
            {
                activeUsers.MoveElement(element, newLocation);
            }
        }

        static Users()
        {
            Task.Run(ForeverUpdateQuadtreeAsync).FireAndForgetAsync(Program.ErrorHandler);
        }

        static async Task ForeverUpdateQuadtreeAsync()
        {
            while (true)
            {
                activeUsers.EfficientlyReindex();

                await Task.Delay(10000);
            }
        }

        static readonly ConcurrentDictionary<string, UserElement> userIdToQuadtreeElement = new ConcurrentDictionary<string, UserElement>();
        static readonly ConcurrentGeoQuadtree<User> activeUsers = new ConcurrentGeoQuadtree<User>();
    }
}