using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagRides.UserProfile.Views;
using Xamarin.Forms;
using TagRides.Shared.Utilities;
using TagRides.Rides;
using TagRides.Rides.States;
using TagRides.Rides.Views;
using TagRides.Shared.RideData.Status;

namespace TagRides.Main.Views
{
    public class MainPage : MasterDetailPage
    {
        public MainPage(Ridesharing rides)
        {
            this.rides = rides;

            if(Device.RuntimePlatform == Device.Android)
            {
                Master = new MasterPage(this) { Title = "TagRide" };
            }
            else if(Device.RuntimePlatform == Device.iOS)
            {
                char hamburger = '\u2261';
                Master = new MasterPage(this) { Title = hamburger.ToString() };
            }

            ShowHomePage();
        }

        public void ShowHomePage()
        {
            if (homePage == null)
                homePage = new NavigationPage(
                    new ContentPage
                    {
                        Title = "TagRide",
                        Content = new HomeView()
                    }
                );

            SetDetail(homePage);
        }

        public void ShowAccountDetails()
        {
            if (accountDetailsPage == null)
            {
                accountDetailsPage = new NavigationPage(
                    new UserProfileDisplayPage(App.Current.UserInfo, App.Current.GameInfo)
                );
            }

            SetDetail(accountDetailsPage);
        }

        public void ShowDevMenu()
        {
            if (devPage == null)
            {
                devPage = new NavigationPage(new DevPage());

            }

            SetDetail(devPage);
        }

        public void ShowServerConfig()
        {
            if (serverConfigPage == null)
            {
                serverConfigPage = new NavigationPage(new ServerConfigPage());
            }

            SetDetail(serverConfigPage);
        }

        public void ShowGamePage()
        {
            if (gamePage == null)
                gamePage = new NavigationPage(new GamePage());

            SetDetail(gamePage);
        }

        public void ShowSettingsPage()
        {
            if (settingsPage == null)
                settingsPage = new NavigationPage(new SettingsPage());

            SetDetail(settingsPage);
        }

        protected override void OnAppearing()
        {
            rides.OnRidesharingStateChanged += HandleRidesStateChanged;
        }

        protected override void OnDisappearing()
        {
            rides.OnRidesharingStateChanged -= HandleRidesStateChanged;
        }

        void SetDetail(Page page)
        {
            if (page != null && Detail != page)
            {
                Detail = page;
            }

            IsPresented = false;
        }

        void HandleRidesStateChanged(Ridesharing.StateBase newState)
        {
            bool isMatched = false;
            PendingRideStatus pendingRideStatus = null;
            Func<Task<bool>> confirm = null;
            Func<Task<bool>> decline = null;

            //TODO ick...
            HomeView hPage = ((homePage as NavigationPage).RootPage as ContentPage).Content as HomeView;

            switch (newState)
            {
                case AbstractMatchedState matchedState:
                    isMatched = true;
                    confirm = matchedState.Confirm;
                    decline = matchedState.Decline;

                    pendingRideStatus = matchedState.MostRecentStatus;
                    break;

                case RideInProgressState rideInProgressState:
                    // TODO: Compute route to display.
                    break;
            }

            if (isMatched)
            {
                async Task ShowConfirmView()
                {
                    ShowHomePage();
                    await (homePage as NavigationPage).PopToRootAsync();

                    hPage.ShowConfirmView(confirm, decline, pendingRideStatus);
                }

                Device.BeginInvokeOnMainThread(() => ShowConfirmView().FireAndForgetAsync(App.Current.ErrorHandler));
            }
        }

        Page homePage;
        Page accountDetailsPage;
        Page serverConfigPage;
        Page gamePage;
        Page devPage;
        Page settingsPage;

        readonly Ridesharing rides;
    }
}
