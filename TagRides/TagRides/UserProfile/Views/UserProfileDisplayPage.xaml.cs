using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TagRides.Shared.UserProfile;
using TagRides.ViewUtilities;
using System.IO;
using Plugin.Media;
using Plugin.Media.Abstractions;

namespace TagRides.UserProfile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class UserProfileDisplayPage : ContentPage
    {
        class ImageViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            
            public ImageViewModel(UserImage image)
            {
                this.image = image;

                image.ImageUpdated += (s) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Image)));
            }

            public ImageSource Image => image.ImageSource;

            UserImage image;
        }

        UserInfo viewModel;
        bool isBusy = false;

        public UserProfileDisplayPage(UserInfo userInfo, GameInfo gameInfo)
        {
            InitializeComponent();

            BindingContext = viewModel = userInfo ?? throw new ArgumentNullException();
            ratingView.BindingContext = gameInfo ?? throw new ArgumentNullException();
            profileImage.BindingContext = new ImageViewModel(App.Current.UserImage);

            viewModel.PropertyChanged += OnUserInfoPropertyChanged;

            LayoutCarViews();
        }

        //Currently does a full rebuilt of the list, only needs to update those values that change
        void LayoutCarViews()
        {
            carStackView.Children.Clear();

            if (viewModel.DriverInfo == null) return;
            
            foreach (CarInfo car in viewModel.DriverInfo.Cars)
            {
                View carView = new CarView(car);
                TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.Tapped += CarTapped;

                carView.GestureRecognizers.Add(tapGestureRecognizer);

                carStackView.Children.Add(carView);
            }
        }

        /// <summary>
        /// Prompts user to update profile photo.
        /// </summary>
        async void OnPhotoTapped(object sender, EventArgs args)
        {
            string title = "Change profile photo";
            string cancel = "Cancel";
            string take = "Take new photo";
            string upload = "Upload from gallery";

            string action = await DisplayActionSheet(title, cancel, null, take, upload);

            if(action == take)
            {
                await CrossMedia.Current.Initialize();
                if (!CrossMedia.Current.IsCameraAvailable && !CrossMedia.Current.IsTakePhotoSupported)
                {
                    await Application.Current.MainPage.DisplayAlert("No Camera", "No Camera available.", "OK");
                }
                else
                {
                    var mediaOptions = new StoreCameraMediaOptions
                    {
                        SaveToAlbum = false,
                        AllowCropping = true,
                        RotateImage = true
                    };
                    MediaFile pic = await CrossMedia.Current.TakePhotoAsync(mediaOptions);

                    if(pic != null)
                        await App.Current.UserProfileManager.UpdateImage(pic.GetStream());
                }
            }
            else if(action == upload)
            {
                await CrossMedia.Current.Initialize();
                if (!CrossMedia.Current.IsPickPhotoSupported)
                {
                    await Application.Current.MainPage.DisplayAlert("Permission Issue", "Access to Photos Denied", "OK");
                }
                else
                {
                    var mediaOptions = new PickMediaOptions();
                    MediaFile pic = await CrossMedia.Current.PickPhotoAsync(mediaOptions);

                    if (pic != null)
                        await App.Current.UserProfileManager.UpdateImage(pic.GetStream());
                }
            }
        }

        void OnUserInfoPropertyChanged(object o, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "DriverInfo.Cars"
                || args.PropertyName == "DriverInfo")
                LayoutCarViews();
        }

        void AddDriverInfoButtonClicked(object sender, EventArgs e)
        {
            DriverInfo di = viewModel.DriverInfo = new DriverInfo();
        }

        void RemoveButtonClicked(object sender, EventArgs e)
        {
            viewModel.DriverInfo = null;
        }

        async void NewCarButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CarEditPage(viewModel.DriverInfo), true);
        }

        async void NameTapped(object sender, EventArgs e)
        {
            if (isBusy) return;
            isBusy = true;

            await AnimationUtilites.Press(sender as View);

            StackLayout editStackLayout = new StackLayout
            {
                Spacing = 4,
                Padding = 10
            };
            editStackLayout.Children.Add(new EditViews.EditStringView(viewModel, "NameFirst", "First"));
            editStackLayout.Children.Add(new EditViews.EditStringView(viewModel, "NameLast", "Last"));

            ContentPage editPage = new ContentPage
            {
                Content = new ScrollView
                {
                    Content = editStackLayout
                },

                Title = "Edit Name"
            };

            await Navigation.PushAsync(editPage, true);

            isBusy = false;
        }

        async void PhoneTapped(object sender, EventArgs e)
        {
            if (isBusy) return;
            isBusy = true;

            await AnimationUtilites.Press(sender as View);

            StackLayout editStackLayout = new StackLayout
            {
                Spacing = 4,
                Padding = 10
            };
            editStackLayout.Children.Add(
                new EditViews.EditStringView(
                    viewModel, 
                    "PhoneNumber", 
                    "Phone", 
                    Keyboard.Telephone, 
                    new ViewUtilities.PhoneNumberPrettyConverter()));

            ContentPage editPage = new ContentPage
            {
                Content = new ScrollView
                {
                    Content = editStackLayout
                },

                Title = "Edit Phone"
            };

            await Navigation.PushAsync(editPage, true);

            isBusy = false;
        }

        async void EmailTapped(object sender, EventArgs e)
        {
            if (isBusy) return;
            isBusy = true;

            await AnimationUtilites.Press(sender as View);

            bool switchAccount = await DisplayAlert("Switch accounts?", "You must logout to use a different email.", "Logout", "Cancel");
            if(switchAccount)
            {
                App.Current.LogOut();
            } 

            isBusy = false;
        }

        async void CarTapped(object sender, EventArgs e)
        {
            if (isBusy) return;
            isBusy = true;

            await AnimationUtilites.Press(sender as View);

            CarInfo car = (sender as CarView).Car;

            await Navigation.PushAsync(new CarEditPage(viewModel.DriverInfo, car), true);

            isBusy = false;
        }

        void OnLogoutClicked(object sender, EventArgs e)
        {
            App.Current.LogOut();
        }
    }
}