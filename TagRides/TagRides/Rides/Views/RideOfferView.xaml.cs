using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TagRides.Utilities;
using TagRides.Shared.UserProfile;
using TagRides.Shared.Game;
using TagRides.ViewUtilities;
using TagRides.Game.Views;
using System.Threading.Tasks;

namespace TagRides.Rides.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RideOfferView : SlideView
    {
        RideOfferViewModel viewModel;

        //TODO Currently not having a car setup is okay. This should probably not be the case
        public RideOfferView(RideOfferViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.viewModel = viewModel ?? throw new ArgumentNullException();
            this.viewModel.EquippedItem = new GameItem("No item, tap to equip!", GameItem.Effect.None, 0);

            viewModel.DisplayActivityIndicator = ActivityOverlay.BeginActivity;

            if (App.Current.UserInfo.DriverInfo.Cars.Count > 0)
            {
                carPicker.ItemsSource = App.Current.UserInfo.DriverInfo.Cars;
                carPicker.SelectedItem = App.Current.UserInfo.DriverInfo.DefaultCar;
            }
            else
            {
                carPicker.Title = "No Preset Cars";
                carPicker.IsEnabled = false;
            }
        }

        private void CarPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;

            viewModel.Car = picker.SelectedItem as CarInfo;
        }

        void OnStartTapped()
        {
            viewModel.ChangeStart();
        }

        void OnEndTapped()
        {
            viewModel.ChangeEnd();
        }

        async void OnItemTapped(object sender, EventArgs e)
        {
            Task pressAnim = AnimationUtilites.Press(itemView);

            SlideOverView(
                new GameItemPicker
                {
                    Inventory = App.Current.GameInfo.Inventory,
                    ItemPicked = (item) =>
                    {
                        ClearSlideOverView(nameof(GameItemPicker));

                        if (item != null)
                            viewModel.EquippedItem = item;
                    },
                    Cancel = () =>
                    {
                        ClearSlideOverView(nameof(GameItemPicker));
                    },
                    BackgroundColor = Color.White
                },
                new Rectangle(0.5, 1, 0, 0),
                new Rectangle(0.5, 0.5, 0.8, 0.5),
                nameof(GameItemPicker));

            await pressAnim;
        }
    }
}