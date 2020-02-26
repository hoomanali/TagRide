using System;
using System.Threading.Tasks;
using TagRides.Maps;
using TagRides.ViewUtilities;
using Xamarin.Forms;
using Plugin.Geolocator;
using TagRides.Rides.States;
using TagRides.Rides.Views;
using TagRides.Shared.Geo;
using TagRides.Shared.Utilities;
using TagRides.Shared.RideData.Status;
using TagRides.Shared.RideData;
using System.ComponentModel;
using System.Windows.Input;

namespace TagRides.Main.Views
{
    public partial class HomeView : SlideView, INotifyPropertyChanged
    {
        public string AppStatus => App.Current.AppStatusAsString;

        public ICommand StatusTappedCommand { get; private set; }

        public HomeView()
        {
            InitializeComponent();

            StatusTappedCommand = new Command(OnStatusTapped);

            App.Current.Rides.OnRidesharingStateChanged += OnRidesharingStateChanged;

            tagRideMap.BindingContext = this;
            tagRideMap.HomeView(OnSearchBarLocation, OnMapLongPress);
        }

        public void ShowConfirmView(Func<Task<bool>> confirm, Func<Task<bool>> decline, PendingRideStatus pendingRideStatus)
        {
            tagRideMap.RideInfoView(pendingRideStatus.RideInfo);

            View slideView = new RideConfirmationView(
                new RideConfirmationViewModel(
                    async () =>
                    {
                        bool success = await confirm();

                        ClearSlideInView();
                        tagRideMap.HomeView(OnSearchBarLocation, OnMapLongPress);

                        if (!success)
                            await this.GetPageParent()?.DisplayAlert("Failed", "Failed to confirm.", "Wait why?");
                    },
                    async () =>
                    {
                        bool success = await decline();

                        ClearSlideInView();
                        tagRideMap.HomeView(OnSearchBarLocation, OnMapLongPress);

                        if (!success)
                            await this.GetPageParent()?.DisplayAlert("Failed", "Failed to decline.", "Wait why?");
                    },
                    async () =>
                    {
                        await decline();

                        ClearSlideInView();
                        tagRideMap.HomeView(OnSearchBarLocation, OnMapLongPress);

                        await this.GetPageParent()?.DisplayAlert("Ride Expired", "Ride has expired, try again to get matched", "Ok");
                    },

                    pendingRideStatus.PostTime,
                    pendingRideStatus.TimeTillExpire,
                    pendingRideStatus.RideInfo));

            // TODO: Avoid hardcoded split. The following View properties do not help:
            // Height, HeightRequest, MinimumHeightRequest
            // They are all -1. There should be some way of determining a view's
            // preferred height.
            SlideInView(slideView, "Confirmation Page", 0.6);
        }

        #region Callbacks

        void OnRidesharingStateChanged(Rides.Ridesharing.StateBase state)
        {
            OnPropertyChanged(nameof(AppStatus));

            switch (state)
            {
                case RideInProgressState inProgress:
                    Device.BeginInvokeOnMainThread(() =>
                       tagRideMap.RideInProgressView(inProgress.MostRecentRideStatus.RideInfo,
                           () => inProgress.Finish().FireAndForgetAsync(App.Current.ErrorHandler),
                           () => inProgress.Cancel().FireAndForgetAsync(App.Current.ErrorHandler)));
                    break;
                case NoneState _:
                    Device.BeginInvokeOnMainThread(() => tagRideMap.HomeView(OnSearchBarLocation, OnMapLongPress));
                    break;
            }
        }

        async void OnStatusTapped()
        {
            if (App.Current.Rides.RidesharingState is AbstractPendingState pendingState)
            {
                Page parentPage = this.GetPageParent();
                bool cancel = false;

                if (parentPage != null)
                {
                    cancel = await parentPage.DisplayAlert(
                        "Status",
                        $"{App.Current.AppStatusDetail}. Would you like to cancel?",
                        "Yes, cancel request", "No");
                }

                if (cancel)
                {
                    tagRideMap.HomeView(OnSearchBarLocation, OnMapLongPress);
                    await pendingState.Cancel();
                }
            }
            else
            {
                await this.GetPageParent()?.DisplayAlert("Status", App.Current.AppStatusDetail, "OK");
            }
        }

        void OnSearchBarLocation(NamedLocation location)
        {
            MakeRideOrOfferTo(location).FireAndForgetAsync(App.Current.ErrorHandler);
        }

        void OnMapLongPress(GeoCoordinates location)
        {
            tagRideMap.LocationSelectionView(location,
                (loc) =>
                {
                    tagRideMap.PureMapView();
                    MakeRideOrOfferTo(loc).FireAndForgetAsync(App.Current.ErrorHandler);
                });
        }

        #endregion

        #region Make request/offer

        async Task MakeRideOrOfferTo(NamedLocation destination)
        {
            if (!(App.Current.Rides.RidesharingState is NoneState))
                return;

            bool isOffer = false;
            if (App.Current.UserInfo.DriverInfo != null && App.Current.UserInfo.DriverInfo.Cars.Count > 0)
                isOffer = await this.GetPageParent()?.DisplayAlert("Offer or request?", "Which is it?", "Offer", "Request");

            NamedLocation origin = new NamedLocation("Current Location",
                (await CrossGeolocator.Current.GetPositionAsync()).ToGeoCoordinates());

            if (isOffer)
            {
                await OfferRide(origin, destination);
            }
            else
            {
                await RequestRide(origin, destination);
            }
        }

        async Task OfferRide(NamedLocation origin, NamedLocation destination)
        {
            if (!(App.Current.Rides.RidesharingState is NoneState currentState))
                return;

            RideOfferViewModel rideOfferViewModel =
                new RideOfferViewModel(
                    async (off) =>
                    {
                        var offer = off as RideOffer;

                        // FIXME: What if current state changed?
                        bool success = await currentState.PostOffer(offer);
                        await Navigation.PopAsync();

                        if (!success)
                        {
                            tagRideMap.HomeView(OnSearchBarLocation, OnMapLongPress);

                            await this.GetPageParent()?.DisplayAlert("Failed", "Failed to offer ride.", "Ok");
                        }
                        else
                        {
                            tagRideMap.RideInfoView(
                                new RideInfo(null, null,
                                    new Route(
                                        new Route.Stop[]
                                        {
                                            new Route.Stop(offer.Trip.Source),
                                            new Route.Stop(offer.Trip.Destination)
                                        }
                                    )
                                )
                            );
                        }
                    },
                    async () =>
                    {
                        tagRideMap.HomeView(OnSearchBarLocation, OnMapLongPress);

                        await Navigation.PopAsync();
                    },
                    async (loc, callback) =>
                    {
                        Page p = await Navigation.PopAsync();
                        tagRideMap.LocationSelectionView(loc,
                            async (newLoc) =>
                            {
                                callback?.Invoke(newLoc);
                                await Navigation.PushAsync(p);
                                tagRideMap.PureMapView();
                            });
                    },
                    origin,
                    destination);

            await Navigation.PushAsync(
                new ContentPage
                {
                    Content = new RideOfferView(rideOfferViewModel)
                }
            );
        }

        async Task RequestRide(NamedLocation origin, NamedLocation destination)
        {
            if (!(App.Current.Rides.RidesharingState is NoneState currentState))
                return;

            RideRequestViewModel rideRequestViewModel =
                new RideRequestViewModel(
                    async (req) =>
                    {
                        var request = req as RideRequest;

                        // FIXME: What if current state changed?
                        bool success = await currentState.PostRequest(request);
                        await Navigation.PopAsync();

                        if (!success)
                        {
                            tagRideMap.HomeView(OnSearchBarLocation, OnMapLongPress);

                            await this.GetPageParent()?.DisplayAlert("Failed", "Failed to request ride.", "Ok");
                        }
                        else
                        {
                            tagRideMap.RideInfoView(
                                new RideInfo(null, null,
                                    new Route(
                                        new Route.Stop[]
                                        {
                                            new Route.Stop(request.Trip.Source),
                                            new Route.Stop(request.Trip.Destination)
                                        }
                                    )
                                )
                            );
                        }
                    },
                    async () =>
                    {
                        tagRideMap.HomeView(OnSearchBarLocation, OnMapLongPress);
                        await Navigation.PopAsync();
                    },
                    async (loc, callback) =>
                    {
                        Page p = await Navigation.PopAsync();
                        tagRideMap.LocationSelectionView(loc,
                            async (newLoc) =>
                            {
                                callback?.Invoke(newLoc);
                                await Navigation.PushAsync(p);
                                tagRideMap.PureMapView();
                            });
                    },
                    origin,
                    destination);

            await Navigation.PushAsync(
                new ContentPage
                {
                    Content = new RideRequestView(rideRequestViewModel)
                }
            );
        }

        #endregion
    }
}
