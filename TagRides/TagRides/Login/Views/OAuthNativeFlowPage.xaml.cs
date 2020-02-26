using System;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Auth;
using TagRides.Shared.UserProfile;

namespace TagRides.Login
{
    public partial class OAuthNativeFlowPage : ContentPage
    {
        Account account;
        AccountStore store;
        int devTeamPicClicked = 0;

        public OAuthNativeFlowPage()
        {
            InitializeComponent();

            store = AccountStore.Create();

            EasterEggInit();
        }

        void OnLoginClicked(object sender, EventArgs e)
        {
            string clientId = null;
            string redirectUri = null;

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    clientId = Constants.iOSClientId;
                    redirectUri = Constants.iOSRedirectUrl;
                    break;

                case Device.Android:
                    clientId = Constants.AndroidClientId;
                    redirectUri = Constants.AndroidRedirectUrl;
                    break;
            }

            try
            {
                account = store.FindAccountsForService(Constants.AppName).FirstOrDefault();
            }
            catch (Exception)
            {
                // FIXME On iOS, this throws an exception for an unknown reason.
            }

            var authenticator = new OAuth2Authenticator(
                clientId,
                null,
                Constants.Scope,
                new Uri(Constants.AuthorizeUrl),
                new Uri(redirectUri),
                new Uri(Constants.AccessTokenUrl),
                null,
                true);

            authenticator.Completed += OnAuthCompleted;
            authenticator.Error += OnAuthError;

            AuthenticationState.Authenticator = authenticator;

            var presenter = new Xamarin.Auth.Presenters.OAuthLoginPresenter();
            presenter.Login(authenticator);
        }

        async void OnAuthCompleted(object sender, AuthenticatorCompletedEventArgs e)
        {
            var authenticator = sender as OAuth2Authenticator;
            if (authenticator != null)
            {
                authenticator.Completed -= OnAuthCompleted;
                authenticator.Error -= OnAuthError;
            }

            TurnOnLoginSpinner(true);
            User user = null;
            if (e.IsAuthenticated)
            {
                // If the user is authenticated, request their basic user data from Google
                // UserInfoUrl = https://www.googleapis.com/oauth2/v2/userinfo
                var request = new OAuth2Request("GET", new Uri(Constants.UserInfoUrl), null, e.Account);
                var response = await request.GetResponseAsync();
                if (response != null)
                {
                    // Deserialize the data and store it in the account store
                    // The users email address will be used to identify data in SimpleDB
                    string userJson = await response.GetResponseTextAsync();
                    user = JsonConvert.DeserializeObject<User>(userJson);
                }

                if (account != null)
                {
                    store.Delete(account, Constants.AppName);
                }

                await store.SaveAsync(account = e.Account, Constants.AppName);

                App app = App.Current;

                // Reject non UCSC domains
                string[] domainCheck = user.Email.Split('@');
                if (!String.Equals(domainCheck[1], "ucsc.edu", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[OAuth_OnAuthCompleted] Authentication error: Not a UCSC email");
                    app.ShowLogin();
                    await DisplayAlert("Authentication Error", "Not a UCSC email", "OK");
                    LoginUtils.SetUserLoggedOut();
                    await Application.Current.SavePropertiesAsync();
                }
                else
                {
                    if (await app.UserProfileManager.Init(user.Email))
                    {
                        app.ShowMain();
                    }
                    else
                    {
                        app.UserInfo.NameFirst = user.Name;
                        app.UserInfo.NameLast = user.FamilyName;
                        app.ShowProfileCreation();
                    }

                    LoginUtils.SetUserLoggedIn();
                }
                TurnOnLoginSpinner(false);
            }
        }

        void OnAuthError(object sender, AuthenticatorErrorEventArgs e)
        {
            var authenticator = sender as OAuth2Authenticator;
            if (authenticator != null)
            {
                authenticator.Completed -= OnAuthCompleted;
                authenticator.Error -= OnAuthError;
            }

            Debug.WriteLine("[OAuth_OnAuthError] Authentication error: " + e.Message);
        }

        void TurnOnLoginSpinner(bool state)
        {
            double setOpacity = 1.0;
            if (state)
            {
                setOpacity = 0.25;
            }
            loginSpinner.IsEnabled = state;
            loginSpinner.IsRunning = state;
            loginSpinner.IsVisible = state;
            tagRidesLogo.Opacity = setOpacity;
            loginButton.Opacity = setOpacity;
            loginButton.IsEnabled = !state;
        }

        void EasterEggInit()
        {
            var tapGestureRecognizer = new TapGestureRecognizer();

            tapGestureRecognizer.Tapped += (s, e) => {
                devTeamPic.IsVisible = false;
                devTeamPicClicked++;
                if (devTeamPicClicked % 5 == 0) {
                    devTeamPic.IsVisible = true;
                }
            };

            tagRidesLogo.GestureRecognizers.Add(tapGestureRecognizer);
            tapGestureRecognizer.NumberOfTapsRequired = 1;
        }
    }
}
