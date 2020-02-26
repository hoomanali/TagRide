using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagRides.Main.Views;
using TagRides.Rides.States;
using TagRides.Shared.Utilities;
using TagRides.Shared.RideData;
using TagRides.Shared.Geo;
using TagRides.Utilities;
using TagRides.ViewUtilities;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TK.CustomMap;
using Plugin.Geolocator;

namespace TagRides.Maps
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TagRideMapView : SlideView, IDialogService
	{
        public TagRideMapView()
		{
			InitializeComponent();

            App.Current.Rides.OnRidesharingStateChanged += OnRidesharingStateChanged;

            PureMapView();
        }

        #region Set view modes

        enum ViewState
        {
            /// <summary>
            /// Just displaying the map
            /// </summary>
            PureMap,
            /// <summary>
            /// The default view that tracks the user. From this view the rideshare flow can be started
            /// </summary>
            Home,
            /// <summary>
            /// Displays and focuses on the route of a <see cref="RideInfo"/>
            /// </summary>
            RideInfo,
            /// <summary>
            /// Select one location
            /// </summary>
            LocationSelection,
            /// <summary>
            /// Displays a route, allows user to finish the ride.
            /// </summary>
            RideInProgress
        };
        
        public void PureMapView()
        {
            if (!SetState(ViewState.PureMap))
                return;

            homeSearchBarLocationCallback = null;
            homeLongPressCallback = null;
            locationSelectedCallback = null;

            Map.IsShowingUser = true;
            Map.HasZoomEnabled = true;
            Map.HasScrollEnabled = true;

            searchBar.IsVisible = false;
            confirmButton.IsVisible = false;
            rideInProgressControls.IsVisible = false;

            Map.Pins = new TKCustomMapPin[] { };
            Map.Polylines = new TK.CustomMap.Overlays.TKPolyline[] { };
        }

        public void HomeView(Action<NamedLocation> searchBarLocationCallback, Action<GeoCoordinates> longPressLocationCallback)
        {
            if (!SetState(ViewState.Home))
                return;

            homeSearchBarLocationCallback = searchBarLocationCallback;
            homeLongPressCallback = longPressLocationCallback;
            locationSelectedCallback = null;

            Map.IsShowingUser = true;
            Map.HasZoomEnabled = true;
            Map.HasScrollEnabled = true;

            searchBar.IsVisible = searchBarLocationCallback != null;
            confirmButton.IsVisible = false;
            rideInProgressControls.IsVisible = false;

            Map.Pins = new TKCustomMapPin[]{ };
            Map.Polylines = new TK.CustomMap.Overlays.TKPolyline[]{ };

            //TODO Define the radius somewhere
            CenterOnUser(new Distance(500)).FireAndForgetAsync(App.Current.ErrorHandler);
        }

        public void RideInfoView(RideInfo rideInfo)
        {
            if (!SetState(ViewState.RideInfo))
                return;
            
            homeSearchBarLocationCallback = null;
            homeLongPressCallback = null;
            locationSelectedCallback = null;

            Map.HasZoomEnabled = false;
            Map.HasScrollEnabled = false;
            Map.IsShowingUser = true;

            searchBar.IsVisible = false;
            confirmButton.IsVisible = false;
            rideInProgressControls.IsVisible = false;

            IEnumerable<Position> route = rideInfo.Route.Stops.Select((s) => s.Location.ToTKPosition());
            Map.Polylines = new TK.CustomMap.Overlays.TKPolyline[]
            {
                new TK.CustomMap.Overlays.TKPolyline
                {
                    LineCoordinates = route.ToList(),
                    LineWidth = 5,
                    Color = Color.Blue
                }
            };

            Map.FitMapRegionToPositions(route, true, 20);
        }

        public void LocationSelectionView(GeoCoordinates startLocation, Action<NamedLocation> locationSelectedCallback)
        {
            if (!SetState(ViewState.LocationSelection))
                return;

            homeSearchBarLocationCallback = null;
            homeLongPressCallback = null;
            this.locationSelectedCallback = locationSelectedCallback;

            Map.HasZoomEnabled = true;
            Map.HasScrollEnabled = true;
            Map.IsShowingUser = true;
            
            searchBar.IsVisible = true;
            confirmButton.IsVisible = locationSelectedCallback != null;
            rideInProgressControls.IsVisible = false;

            Map.MoveToMapRegion(MapSpan.FromCenterAndRadius(startLocation.ToTKPosition(), new Distance(500)), true);

            Map.Pins = new TKCustomMapPin[]
            {
                new TKCustomMapPin
                {
                    Position = startLocation.ToTKPosition(),
                    IsDraggable = true
                }
            };
            
            Map.Polylines = new TK.CustomMap.Overlays.TKPolyline[]{ };
        }

        public void RideInProgressView(RideInfo rideInfo, Action finishCallback, Action cancelCallback)
        {
            if (!SetState(ViewState.RideInProgress))
                return;

            rideFinishCallback = finishCallback;
            rideCancelCallback = cancelCallback;

            homeSearchBarLocationCallback = null;
            homeLongPressCallback = null;
            locationSelectedCallback = null;

            Map.HasZoomEnabled = false;
            Map.HasScrollEnabled = false;
            Map.IsShowingUser = true;

            searchBar.IsVisible = false;
            confirmButton.IsVisible = false;
            rideInProgressControls.IsVisible = true;

            var route = rideInfo.Route.Stops.Select((s) => s.Location.ToTKPosition());
            Map.Polylines = new TK.CustomMap.Overlays.TKPolyline[]
            {
                new TK.CustomMap.Overlays.TKPolyline
                {
                    LineCoordinates = route.ToList(),
                    LineWidth = 5,
                    Color = Color.Blue
                }
            };

            try
            {
                Map.FitMapRegionToPositions(route, true, 20);
            }
            catch (InvalidOperationException)
            {
                Map.MapReady += (a, b) =>
                    Map.FitMapRegionToPositions(route, true, 20);
            }
        }

        #endregion

        #region UI event handlers

        void OnSearchBarFocused(object sender, EventArgs e)
        {
            LocationSearchPageViewModel searchViewModel = null;

            switch(state)
            {
                case ViewState.Home:
                    searchViewModel = new LocationSearchPageViewModel(
                        (loc) =>
                        {
                            ClearSlideOverView("LocationSelectionView");
                            searchBar.IsVisible = true;

                            homeSearchBarLocationCallback?.Invoke(loc);
                        });
                    break;
                case ViewState.LocationSelection:
                    searchViewModel = new LocationSearchPageViewModel(
                        (loc) =>
                        {
                            var pin = Map.Pins.First();
                            pin.Position = loc.Coordinates.ToTKPosition();

                            Map.MoveToMapRegion(MapSpan.FromCenterAndRadius(pin.Position, new Distance(500)), true);

                            ClearSlideOverView("LocationSelectionView");
                            searchBar.IsVisible = true;
                        });
                    break;
            }

            if (searchViewModel != null)
            {
                Rectangle searchBarBounds = AbsoluteLayout.GetLayoutBounds(searchBar);

                var searchView = new LocationSearchView(searchViewModel);

                SlideOverView(
                    searchView,
                    searchBarBounds,
                    new Rectangle(0.5, 0.5, 0.8, 0.8),
                    "LocationSelectionView");

                searchView.FocusBar();
                searchBar.IsVisible = false;
            }
        }

        void OnMapLongPress(object sender, TKGenericEventArgs<Position> e)
        {
            homeLongPressCallback?.Invoke(new GeoCoordinates(e.Value.Latitude, e.Value.Longitude));
        }

        void OnConfirmClicked(object sender, EventArgs e)
        {
            if (state != ViewState.LocationSelection)
                return;

            Position loc = Map.Pins.First().Position;
            GeoCoordinates coord = new GeoCoordinates(loc.Latitude, loc.Longitude);
            locationSelectedCallback?.Invoke(new NamedLocation("Map Pin", coord));
        }

        void OnRideFinishClicked(object sender, EventArgs e)
        {
            rideFinishCallback?.Invoke();
        }

        void OnRideCancelClicked(object sender, EventArgs e)
        {
            rideCancelCallback?.Invoke();
        }

        #endregion

        void OnRidesharingStateChanged(Rides.Ridesharing.StateBase state)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                switch (state)
                {
                    case AbstractPendingState pendingState:
                        statusLabel.IsVisible = true;
                        statusLabel.Text = "Request Pending";
                        break;
                    default:
                        statusLabel.IsVisible = false;
                        break;
                }
            });
        }

        public Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        {
            Page pageParent = this.GetPageParent();

            if (pageParent != null)
                return this.GetPageParent().DisplayAlert(title, message, accept, cancel);
            return Task.FromResult(false);
        }

        public Task DisplayAlert(string title, string message, string cancel)
        {
            Page pageParent = this.GetPageParent();

            //TODO maybe throw if pageParent is null? It should be impossible for it to be null
            if (pageParent != null)
                return this.GetPageParent().DisplayAlert(title, message, cancel);
            return Task.FromResult(false);
        }

        async Task CenterOnUser(Distance radius)
        {
            if (!CrossGeolocator.Current.IsGeolocationAvailable)
                throw new Exception("Geolocation not avalible");

            var currentLoctationTask = CrossGeolocator.Current.GetPositionAsync();
            
            //This should quickly give us a somewhat accurate location
            var lastKnowLocation = await CrossGeolocator.Current.GetLastKnownLocationAsync();
            Map.MapRegion = MapSpan.FromCenterAndRadius(lastKnowLocation.ToGeoCoordinates().ToTKPosition(), radius);

            //This gives us the actual current location
            var currentLocation = await currentLoctationTask;
            Map.MapRegion = MapSpan.FromCenterAndRadius(currentLocation.ToGeoCoordinates().ToTKPosition(), radius);
        }

        /// <summary>
        /// Sets the state variable. Does not actually change the view state
        /// </summary>
        /// <param name="toState"></param>
        /// <returns>false if the state transition shouldn't happen</returns>
        bool SetState(ViewState toState)
        {
            state = toState;
            return true;
        }

        ViewState state = ViewState.RideInfo;

        #region callbacks

        //HomeView
        Action<NamedLocation> homeSearchBarLocationCallback;
        Action<GeoCoordinates> homeLongPressCallback;

        //RideInfoView

        //LocationSelectionView
        Action<NamedLocation> locationSelectedCallback;

        //RideInProgressView
        Action rideFinishCallback;
        Action rideCancelCallback;

        #endregion
    }
}