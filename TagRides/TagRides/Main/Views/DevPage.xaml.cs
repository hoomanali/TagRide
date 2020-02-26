using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TagRides.Services;
using Xamarin.Forms;
using TagRides.Shared.DataStore;
using TagRides.Shared.UserProfile;
using TagRides.Shared.Utilities;

namespace TagRides.Main.Views
{
    /// <summary>
    /// Developer menu page.
    /// </summary>
    public partial class DevPage : ContentPage
    {
        public DevPage()
        {
            InitializeComponent();

            var userInfo = App.Current.UserInfo;
            var dataStore = App.Current.DataStore;

            InitPushProfileButton(userInfo, dataStore);
            InitPullProfileButton(userInfo, dataStore);
            InitDeleteProfileButton(userInfo, dataStore);

            // Server config page
            AddressField.Text = App.Current.ServerAddress?.ToString() ?? "";
        }

        /// <summary>
        /// Initializes the push profile button.
        /// </summary>
        /// <param name="u">The current UserInfo object.</param>
        /// <param name="d">The current TagRideDataStore object.</param>
        void InitPushProfileButton(UserInfo u, TagRideDataStore d)
        {
            var button = pushProfileButton;
            button.Text = "Push profile to cloud";
            button.Clicked += async (sender, e) => await d.PostUserInfo(u);
        }

        /// <summary>
        /// Initializes the pull profile button.
        /// </summary>
        /// <param name="u">The current UserInfo object</param>
        /// <param name="d">The current TagRideDataStore object</param>
        void InitPullProfileButton(UserInfo u, TagRideDataStore d)
        {
            var button = pullProfileButton;
            button.Text = "Pull profile from cloud";
            button.Clicked += async (sender, e) => await PullProfileButtonClicked(u, d);
        }

        /// <summary>
        /// Click event for Pull Profile button async.
        /// </summary>
        /// <param name="u">App.Current.UserInfo</param>
        /// <param name="d">App.Current.DataStore</param>
        async Task PullProfileButtonClicked(UserInfo u, TagRideDataStore d)
        {
            UserInfo user = await d.GetUserInfo(u.UserId);
            u.UpdateToMatch(user);
        }

        // Copied over from ServerConfigPage
        async void HandleServerAddressEntered(object sender, EventArgs e)
        {
            try
            {
                App.Current.ServerAddress = new Uri(AddressField.Text);
                await Ping();
            }
            catch (FormatException)
            {
                ErrorText.IsVisible = true;
                ErrorText.Text = "Incorrect format. (example: http://192.168.1.10:5000/)";
            }
        }

        // Copied over from ServerConfigPage
        void HandleServerAddressChanged(object sender, TextChangedEventArgs e)
        {
            ErrorText.IsVisible = false;
        }

        // Copied over from ServerConfigPage
        async void HandlePing(object sender, EventArgs e)
        {
            await Ping();
        }

        // Copied over from ServerConfigPage
        async Task Ping()
        {
            Animation ellipsisAnimation = new Animation(t => PingStatus.Text = "waiting" + new string('.', (int)(t * 4)));

            PingStatus.Text = "waiting";
            PingStatus.Animate("Ellipsis", ellipsisAnimation, 200, 800, null, null, () => true);

            bool success = await DataService.Instance.PingServerAsync();

            PingStatus.AbortAnimation("Ellipsis");
            PingStatus.Text = success ? "OK" : "Failed";
            PingStatus.TextColor = success ? Color.Green : Color.Red;

            // This triggers the App to recheck the server address, so the text
            // under PingStatus is somewhat synchronized with the IsServerAddressGood
            // property. It is mostly a quick hack to avoid a possible headscratcher.
            App.Current.ServerAddress = App.Current.ServerAddress;
        }

        /// <summary>
        /// Inits button for deleting profile and photo from cloud storage.
        /// </summary>
        /// <param name="u">UuserInfo.</param>
        /// <param name="d">TagRideDataStore.</param>
        void InitDeleteProfileButton(UserInfo u, TagRideDataStore d)
        {
            var button = deleteProfileButton;
            button.Text = "Delete profile and logout";

            button.Clicked += async (sender, e) =>
            {
                await DeleteProfileButtonClicked(u, d);
            };
        }

        /// <summary>
        /// Click event for deleteProfileButton.
        /// </summary>
        /// <returns>The profile button clicked.</returns>
        /// <param name="u">UserInfo.</param>
        /// <param name="d">TagRideDataStore.</param>
        async Task DeleteProfileButtonClicked(UserInfo u, TagRideDataStore d)
        {
            // Prompt for profile deletion
            bool deleteProfile = await DisplayAlert("Delete Profile",
                "Permanently delete profile information and photo?", "Delete", "Cancel");

            if(deleteProfile)
            {
                await d.DeleteUserInfo(u);
                Login.LoginUtils.SetUserLoggedOut();
                App.Current.UserProfileManager.ResetWithoutPush();
                App.Current.ShowLogin();
            }
            else
            {
                return;
            }
        }

    }
}
