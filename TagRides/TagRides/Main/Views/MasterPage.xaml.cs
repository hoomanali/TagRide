using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using TagRides.Services;
using TagRides.UserProfile;
using TagRides.Shared.UserProfile;
using System.ComponentModel;

namespace TagRides.Main.Views
{
    public partial class MasterPage : ContentPage
    {
        class MasterPageViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public ImageSource Image => userImage.ImageSource;
            public string Name => userInfo.NameFirst;
            public string Faction => gameInfo.Faction ?? "";
            public string Level => "Lvl: " + (gameInfo.Level != null ? gameInfo.Level.Level : 0);
            public float LevelProgress => gameInfo.Level != null ? gameInfo.Level.LevelProgress : 0;

            public MasterPageViewModel(UserInfo userInfo, GameInfo gameInfo, UserImage userImage)
            {
                this.userInfo = userInfo;
                this.gameInfo = gameInfo;
                this.userImage = userImage;

                userInfo.PropertyChanged += OnPropertyChanged;
                gameInfo.PropertyChanged += OnPropertyChanged;
                userImage.ImageUpdated += (s) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Image)));
            }

            void OnPropertyChanged(object obj, PropertyChangedEventArgs arg)
            {
                switch (arg.PropertyName)
                {
                    case nameof(UserInfo.NameFirst):
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                        break;
                    case nameof(GameInfo.Level):
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Level)));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LevelProgress)));
                        break;
                    case nameof(GameInfo.Faction):
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Faction)));
                        break;
                }
            }

            readonly UserInfo userInfo;
            readonly GameInfo gameInfo;
            readonly UserImage userImage;
        }

        public MasterPage(MainPage mainPage)
        {
            InitializeComponent();

            On<Xamarin.Forms.PlatformConfiguration.iOS>().SetUseSafeArea(true);

            this.mainPage = mainPage;

            BindingContext = new MasterPageViewModel(App.Current.UserInfo, App.Current.GameInfo, App.Current.UserImage);

            InitSideBarTaps();
        }

        void ShowHomePage(object sender, System.EventArgs e)
        {
            mainPage.ShowHomePage();
        }

        void ShowAccountDetails(object sender, System.EventArgs e)
        {
            mainPage.ShowAccountDetails();
        }

        void ShowDevMenu(object sender, System.EventArgs e)
        {
            mainPage.ShowDevMenu();
        }

        async void ShowGamePage(object sender, EventArgs e)
        {
            //Do this somewhere better. Maybe with a pull to refresh?
            await App.Current.UserProfileManager.PullGameInfo();
            mainPage.ShowGamePage();
        }

        void ShowSettingsPage(object sender, EventArgs e)
        {
            mainPage.ShowSettingsPage();
        }

        void OnLogoutClicked(object sender, EventArgs e)
        {
            App.Current.LogOut();
        }

        /// <summary>
        /// Initializes tap gestures for the sidebar profile summary.
        /// </summary>
        void InitSideBarTaps()
        {
            // Setup Account Details tap gesture
            var accountDetailsTap = new TapGestureRecognizer();
            accountDetailsTap.Tapped += (s, e) =>
            {
                mainPage.ShowAccountDetails();
            };

            // Setup Game Details tap gesture
            var gameDetailsTap = new TapGestureRecognizer();
            gameDetailsTap.Tapped += (s, e) =>
            {
                mainPage.ShowGamePage();
            };

            // Add gestures to UI elements
            ProfileGrid.GestureRecognizers.Add(accountDetailsTap);
            profilePic.GestureRecognizers.Add(accountDetailsTap);
            profileLevel.GestureRecognizers.Add(gameDetailsTap);
            profileXP.GestureRecognizers.Add(gameDetailsTap);
        }

        MainPage mainPage;
    }
}
