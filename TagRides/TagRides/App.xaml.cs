using System;
using System.Net.Http;
using System.Threading.Tasks;
using Plugin.Geolocator;
using TagRides.UserProfile;
using TagRides.Services;
using TagRides.Shared.RideData;
using TagRides.Shared.RideData.Status;
using TagRides.Shared.DataStore;
using TagRides.Shared.UserProfile;
using TagRides.Shared.Utilities;
using TagRides.Shared.AppData;
using TagRides.Utilities;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;
using TagRides.Rides;
using TagRides.Rides.States;
using System.IO;
using Newtonsoft.Json;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace TagRides
{
    public partial class App : Application, IDialogService
    {
        public new static App Current => Application.Current as App;

        #region Properties

        public readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        public readonly AsyncLazy<TagRideProperties> TagRideProperties;

        public readonly UserProfileManager UserProfileManager;
        public UserInfo UserInfo => UserProfileManager.UserInfo;
        public GameInfo GameInfo => UserProfileManager.GameInfo;
        public UserImage UserImage => UserProfileManager.UserImage;

        public readonly TagRideDataStore DataStore = new TagRideDataStore
        {
            DataStore = new AzureDataStore()
        };

        public readonly IErrorHandler ErrorHandler;

        public Uri ServerAddress
        {
            get => serverAddress;
            set
            {
                serverAddress = value;
            }
        }

        public Ridesharing Rides { get; private set; }

        public string AppStatusAsString
        {
            get
            {
                switch (Rides.RidesharingState)
                {
                    case OfferPendingState _:
                        return "Ride Offer Pending";
                    case OfferMatchedState _:
                        return "Ride Offer Matched";
                    case RequestPendingState _:
                        return "Ride Request Pending";
                    case RequestMatchedState _:
                        return "Ride Request Matched";
                    case WaitingForConfirmedState _:
                        return "Waiting for Others";
                    case RideInProgressState _:
                        return "Ride in Progress";
                    default:
                        return "";
                }
            }
        }

        public string AppStatusDetail => AppStatusAsString;

        #endregion

        public App()
        {
            InitializeComponent();

            TagRideProperties = new AsyncLazy<TagRideProperties>(DataStore.GetTagRideProperties);
            TagRideProperties.BeginInitializingValue();

            ErrorHandler = new DialogErrorHandler(this);

            UserProfileManager = new UserProfileManager();

            MainPage = new Main.Views.AppLoadingPage();
        }

        #region Show Page Methods

        public void ShowLogin()
        {
            MainPage = new NavigationPage(new Login.OAuthNativeFlowPage());
        }

        public void ShowProfileCreation()
        {
            Page mainPage;
            NewUser.ProfileCreationController creationController = new NewUser.ProfileCreationController(out mainPage);

            MainPage = mainPage;

            creationController.ProfileCreationComplete += OnProfileCreationComplete;
        }

        public void ShowMain()
        {
            // TODO Initialize Ridesharing after log-in.
            // Doing it here isn't explicit enough.
            Rides = new Ridesharing(ErrorHandler)
            {
                RideOfferer = new ServerRideOfferer(),
                RideRequester = new ServerRideRequester()
            };

            MainPage = new Main.Views.MainPage(Rides);
        }

        public void ShowDevMenu()
        {
            MainPage = new Main.Views.DevPage();
        }

        #endregion

        #region Theme

        public bool IsLightTheme { get; private set; }

        public void SetDarkTheme()
        {
            if (!IsLightTheme)
                return;

            Resources["textColor"] = Color.FromHex("#d9e5d6");
            Resources["dimTextColor"] = Color.FromHex("#90998e");
            Resources["mainBackgroundColor"] = Color.FromHex("#313E50");
            Resources["secondaryBackgroundColor"] = Color.FromHex("#3A435E");

            IsLightTheme = false;
        }

        public void SetLightTheme()
        {
            if (IsLightTheme)
                return;

            Resources["textColor"] = Color.FromHex("#333333");
            Resources["dimTextColor"] = Color.FromHex("#606060");
            Resources["mainBackgroundColor"] = Color.FromHex("#ffffff");
            Resources["secondaryBackgroundColor"] = Color.FromHex("#16c4ff");

            IsLightTheme = true;
        }

        #endregion

        public void LogOut()
        {
            Login.LoginUtils.SetUserLoggedOut();
            ShowLogin();
            TaskUtilities.WaitSync(UserProfileManager.Reset);
        }

        #region App focus callbacks

        protected override void OnStart()
        {
            Task.Run(InitializeAppAsync).OnError(ErrorHandler);

            if (!CrossGeolocator.IsSupported)
            {
                Console.WriteLine("CrossGeolocator not supported on this platform!");
            }
            else
            {
                if (!CrossGeolocator.Current.IsListening)
                {
                    async Task BeginListening()
                    {
                        await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(5), 1);
                    }

                    Task.Run(BeginListening).OnError(ErrorHandler);
                }
            }
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            // TODO: Maybe stop listening for location updates on sleep?

            if (Properties.ContainsKey("theme"))
                Properties["theme"] = IsLightTheme ? "light" : "dark";
            else
                Properties.Add("theme", IsLightTheme ? "light" : "dark");

            SavePropertiesAsync().FireAndForgetAsync(ErrorHandler);
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        #endregion

        #region IDialogService implementation

        Task IDialogService.DisplayAlert(string title, string message, string cancel)
        {
            Console.WriteLine($"ALERT MESSAGE: {title}; MESSAGE: {message}");

            Device.BeginInvokeOnMainThread(
                () => MainPage?.DisplayAlert(title, message, cancel)
                        .FireAndForgetAsync(ErrorHandler));

            return Task.CompletedTask;
        }

        async Task<bool> IDialogService.DisplayAlert(string title, string message, string accept, string cancel)
        {
            Console.WriteLine($"ALERT MESSAGE: {title}; MESSAGE: {message}");

            if (MainPage == null)
                return false;

            if (MainThread.IsMainThread)
                return await MainPage.DisplayAlert(title, message, accept, cancel);

            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

            async Task Display()
            {
                completionSource.SetResult(
                    await MainPage.DisplayAlert(title, message, accept, cancel));
            }

            Device.BeginInvokeOnMainThread(() => Display().FireAndForgetAsync(ErrorHandler));

            return await completionSource.Task;
        }

        #endregion

        async Task InitializeAppAsync()
        {
            if (Properties.ContainsKey("theme") && Properties["theme"] as string == "dark")
                Device.BeginInvokeOnMainThread(SetDarkTheme);
            else
                Device.BeginInvokeOnMainThread(SetLightTheme);

            if (Login.LoginUtils.IsUserLoggedIn())
            {
                string userEmail = Login.LoginUtils.LoggedInUserEmail();
                if (await UserProfileManager.Init(userEmail))
                    Device.BeginInvokeOnMainThread(ShowMain);
                else
                    Device.BeginInvokeOnMainThread(ShowProfileCreation);
            }
            else
            {
                Device.BeginInvokeOnMainThread(ShowLogin);
                Console.WriteLine("[App_CheckLogin] Logged out, show login.");
            }
        }

        void OnProfileCreationComplete(NewUser.ProfileCreationController controller, UserInfo userInfo, Stream image, string faction)
        {
            controller.ProfileCreationComplete -= OnProfileCreationComplete;

            userInfo.EmailAddress = UserInfo.EmailAddress;
            UserInfo.UpdateToMatch(userInfo);
            if (image != null)
                UserProfileManager.UpdateImage(image).FireAndForgetAsync(ErrorHandler);

            //TODO what if this fails?
            GameService.Instance.PostFactionAsync(UserInfo.UserId, faction).FireAndForgetAsync(ErrorHandler);

            ShowMain();
        }

        #region Constants

        Uri serverAddress = new Uri("http://34.83.227.160:80/");

        #endregion
    }
}
