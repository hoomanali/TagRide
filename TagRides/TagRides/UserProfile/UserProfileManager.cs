using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Timers;

using TagRides.Shared.UserProfile;
using TagRides.Shared.Utilities;
using TagRides.Services;
using System.IO;

namespace TagRides.UserProfile
{
    public class UserProfileManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userEmail"></param>
        /// <param name="timeBeforePush">Time in millis after a change before the <see cref="UserInfo"/> is pushed</param>
        public UserProfileManager(double timeBeforePush = 20000)
        {
            userInfo = new UserInfo();
            gameInfo = new GameInfo();

            pushTimer = new Timer
            {
                Interval = timeBeforePush,
                Enabled = false,
                AutoReset = false
            };

            pushTimer.Elapsed += (s, a) => PushUserInfo().FireAndForgetAsync(App.Current.ErrorHandler);
        }

        public UserInfo UserInfo => isInitialized? userInfo : throw new Exception("Getting user info before initializing.");
        public GameInfo GameInfo => isInitialized? gameInfo : throw new Exception("Getting game info before initializing.");
        public UserImage UserImage => isInitialized ? userImage : throw new Exception("Getting user image before initializing.");

        /// <summary>
        /// Set up tracking for the  for the first time!
        /// If a version already exists on the server, then it is pulled.
        /// Must be run for this to track updates to the contained <see cref="UserInfo"/>
        /// </summary>
        /// <returns>True if the UserInfo exists on the server. False otherwise</returns>
        public async Task<bool> Init(string userEmail)
        {
            if (isInitialized) throw new Exception("Initializing UserProfileManager more than once.");
            isInitialized = true;

            userInfo.EmailAddress = userEmail;
            userImage = new UserImage(userInfo.UserId);
            
            UserInfo dbUserInfo = await App.Current.DataStore.GetUserInfo(userInfo.UserId);
            if (dbUserInfo != null)
                userInfo.UpdateToMatch(dbUserInfo);
            
            //This will update this's gameInfo if it exists on the DB
            await PullGameInfo();

            userInfo.PropertyChanged += OnUserInfoChanged;
            return dbUserInfo != null;
        }

        /// <summary>
        /// Dissociates this from the user set up from the last call to init
        /// This can be used again after calling init
        /// </summary>
        /// <returns></returns>
        public async Task Reset()
        {
            if (!isInitialized)
                return;

            pushTimer.Stop();

            await PushUserInfo();

            userInfo.SetAllToDefault();
            gameInfo.SetAllToDefault();
            userImage = null;

            isInitialized = false;
        }

        /// <summary>
        /// Dissociates this from the user set up from the last call to init.
        /// Does not push profile info, used for profile deletion.
        /// </summary>
        public void ResetWithoutPush()
        {
            pushTimer.Stop();

            userInfo.SetAllToDefault();
            gameInfo.SetAllToDefault();
            userImage = null;

            isInitialized = false;
        }

        /// <summary>
        /// Uploads the stored user info to the server, and calls the api method TODO on the server
        /// </summary>
        public async Task PushUserInfo()
        {
            if (!isInitialized)
                throw new Exception();

            await App.Current.DataStore.PostUserInfo(userInfo);

            //TODO do we care if this is false?
            //Probably not
            await DataService.Instance.PostUserInfoUpdated(userInfo.UserId);
        }
        
        /// <summary>
        /// Update this's GameInfo to match the one stored on the data store, if it exists
        /// </summary>
        /// <returns></returns>
        public async Task PullGameInfo()
        {
            if (!isInitialized)
                throw new Exception();
            
            GameInfo dbGameInfo = await App.Current.DataStore.GetGameInfo(userInfo.UserId);

            //If a GameInfo isn't on the server, the user has the default GameInfo
            if (dbGameInfo == null)
                dbGameInfo = new GameInfo();

            gameInfo.UpdateToMatch(dbGameInfo);
        }

        public async Task UpdateImage(Stream data)
        {
            userImage.UpdateImage(data);
            await App.Current.DataStore.PostPhoto(userInfo.UserId, data);
        }

        void OnUserInfoChanged(object obj, PropertyChangedEventArgs args)
        {
            //If the timer is already running, then no need to do anything
            if (pushTimer.Enabled)
                return;

            pushTimer.Start();
        }

        readonly UserInfo userInfo;
        readonly GameInfo gameInfo;
        UserImage userImage;
        readonly Timer pushTimer;

        bool isInitialized = false;
    }
}
