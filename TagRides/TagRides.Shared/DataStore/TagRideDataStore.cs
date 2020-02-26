using System;
using System.Xml.Serialization;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using TagRides.Shared.UserProfile;
using TagRides.Shared.Utilities;
using TagRides.Shared.AppData;
using TagRides.Shared.RideData.Status;
using TagRides.Shared.Game;
using Newtonsoft.Json;

namespace TagRides.Shared.DataStore
{
    /// <summary>
    /// Used to access and modify data structures on a data store
    /// </summary>
    public class TagRideDataStore
    {
        /// <summary>
        /// The backing <see cref="IDataStore"/> used to fetch data.
        /// </summary>
        public IDataStore DataStore { get; set; }

        #region Resource names

        const string TagRidePropertiesName = "public-resources/appProperties";
        string UserInfoName(string userId) => $"{userId}/userInfo";
        string GameInfoName(string userId) => $"{userId}/gameInfo";
        string UserPhotoName(string userId) => $"{userId}/photo";
        string RideRelatedRequestStatusName(string userId, string requestId) => $"requests/{userId}/{requestId}";
        string PendingRideStatusName(string pendingRideId) => $"pending-rides/{pendingRideId}";
        string ActiveRideStatusName(string activeRideId) => $"active-rides/{activeRideId}";
        string EffectsQueueName(string userId) => $"{userId}/gameEffects";

        #endregion

        #region TagRideProperties

        public async Task<TagRideProperties> GetTagRideProperties()
        {
            if (DataStore == null) return null;

            if (!await DataStore.ResourceExists(TagRidePropertiesName))
                return null;

            string data = await DataStore.GetStringResource(TagRidePropertiesName);
            return JsonConvert.DeserializeObject<TagRideProperties>(data);
        }

        public async Task PostTagRideProperties(TagRideProperties properties)
        {
            if (DataStore == null) return;

            string data = JsonConvert.SerializeObject(properties);
            await DataStore.PostStringResource(TagRidePropertiesName, data);
        }

        #endregion

        #region UserInfo

        /// <summary>
        /// Get a user profile with ID <paramref name="userId"/>
        /// </summary>
        /// <param name="userId">The user id to fetch the profile for</param>
        /// <returns></returns>
        public async Task<UserInfo> GetUserInfo(string userId)
        {
            if (DataStore == null) return null;

            if (!await DataStore.ResourceExists(UserInfoName(userId))) return null;

            string data = await DataStore.GetStringResource(UserInfoName(userId));
            return JsonConvert.DeserializeObject<UserInfo>(data);
        }

        /// <summary>
        /// Post a <see cref="UserInfo"/>. If one already existed for <paramref name="userId"/>, then it is overwritten.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        public async Task PostUserInfo(UserInfo userInfo)
        {
            if (DataStore == null) return;

            string rawData = JsonConvert.SerializeObject(userInfo);

            await DataStore.PostStringResource(UserInfoName(userInfo.UserId), rawData);
        }

        /// <summary>
        /// Deletes user from cloud storage.
        /// </summary>
        /// <param name="userInfo">User info.</param>
        public async Task DeleteUserInfo(UserInfo userInfo)
        {
            if (DataStore == null) return;

            await DataStore.DeleteResource(userInfo.UserId);
        }

        public async Task<GameInfo> GetGameInfo(string userId)
        {
            if (DataStore == null) return null;

            if (!await DataStore.ResourceExists(GameInfoName(userId)))
                return null;

            string data = await DataStore.GetStringResource(GameInfoName(userId));

            return JsonConvert.DeserializeObject<GameInfo>(data);
        }

        public async Task PostGameInfo(string userId, GameInfo gameInfo)
        {
            if (DataStore == null) return;

            string data = JsonConvert.SerializeObject(gameInfo);

            await DataStore.PostStringResource(GameInfoName(userId), data);
        }

        #endregion

        #region ProfilePhoto

        /// <summary>
        /// Posts the photo to Azure storage.
        /// </summary>
        /// <param name="data">Stream Data.</param>
        public async Task PostPhoto(string userId, Stream data)
        {
            if(DataStore == null)
                return;

            await DataStore.PostStreamResource(UserPhotoName(userId), data);
        }

        /// <summary>
        /// Gets the profile photo.
        /// </summary>
        /// <returns>A byte array of the profile photo.</returns>
        public async Task<byte[]> GetPhoto(string userId)
        {
            if (DataStore == null)
                return null;

            if (!await DataStore.ResourceExists(UserPhotoName(userId)))
                return null;

            return await DataStore.GetByteResource(UserPhotoName(userId));
        }

        #endregion

        #region Statuses

        public async Task<RideRelatedRequestStatus> GetRideRelatedRequestStatus(string userId, string requestId)
        {
            if (DataStore == null) return null;

            if (!await DataStore.ResourceExists(RideRelatedRequestStatusName(userId, requestId)))
                return null;

            string data = await DataStore.GetStringResource(RideRelatedRequestStatusName(userId, requestId));

            return JsonConvert.DeserializeObject<RideRelatedRequestStatus>(data);
        }

        public async Task PostRideRelatedRequestStatus(string userId, string requestId, RideRelatedRequestStatus status)
        {
            if (DataStore == null) return;

            string data = JsonConvert.SerializeObject(status);

            await DataStore.PostStringResource(RideRelatedRequestStatusName(userId, requestId), data);
        }

        public async Task<PendingRideStatus> GetPendingRideStatus(string pendingRideId)
        {
            if (DataStore == null) return null;

            if (!await DataStore.ResourceExists(PendingRideStatusName(pendingRideId)))
                return null;

            string data = await DataStore.GetStringResource(PendingRideStatusName(pendingRideId));

            return JsonConvert.DeserializeObject<PendingRideStatus>(data);
        }

        public async Task PostPendingRideStatus(PendingRideStatus status)
        {
            if (DataStore == null) return;

            string data = JsonConvert.SerializeObject(status);

            await DataStore.PostStringResource(PendingRideStatusName(status.Id), data);
        }

        public async Task<ActiveRideStatus> GetActiveRideStatus(string activeRideId)
        {
            if (DataStore == null) return null;

            if (!await DataStore.ResourceExists(ActiveRideStatusName(activeRideId)))
                return null;

            string data = await DataStore.GetStringResource(ActiveRideStatusName(activeRideId));

            return JsonConvert.DeserializeObject<ActiveRideStatus>(data);
        }

        public async Task PostActiveRideStatus(ActiveRideStatus status)
        {
            if (DataStore == null) return;

            string data = JsonConvert.SerializeObject(status);

            await DataStore.PostStringResource(ActiveRideStatusName(status.Id), data);
        }

        #endregion

        #region Game Stuff

        public async Task<IEnumerable<GameInfoEffectBase>> GetGameInfoEffects(string userId)
        {
            if (DataStore == null) return null;

            if (!await DataStore.ResourceExists(EffectsQueueName(userId)))
                return null;

            string data = await DataStore.GetStringResource(EffectsQueueName(userId));

            return JsonConvert.DeserializeObject<List<GameInfoEffectBase>>(data);
        }

        public async Task PostGameInfoEffects(string userId, IEnumerable<GameInfoEffectBase> effects)
        {
            if (DataStore == null) return;

            string data = JsonConvert.SerializeObject(effects);

            await DataStore.PostStringResource(EffectsQueueName(userId), data);
        }

        #endregion
    }
}
