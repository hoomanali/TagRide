using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using TagRides.Places.Details;
using Xamarin.Forms;
using TagRides.Shared.Utilities;
using TagRides.Shared.Geo;
using TagRides.ViewUtilities;

namespace TagRides.Main.Views
{
    public partial class LocationSearchView : ContentView
    {
        public LocationSearchView(LocationSearchPageViewModel viewModel)
        {
            InitializeComponent();
            
            this.viewModel = viewModel;
            BindingContext = viewModel ?? throw new NullReferenceException();

            //TODO focus on searchbar?

            SearchBar.SessionToken = Guid.NewGuid().ToString();
        }

        public void FocusBar()
        {
            SearchBar.Focus();
        }

        void OnPredictionsUpdated(Places.Autocomplete.AutocompleteResponse predictions)
        {
            viewModel.SearchResults.Clear();

            if (predictions != null)
            {
                foreach (var prediction in predictions.Predictions)
                    viewModel.SearchResults.Add(
                        new LocationSearchPageViewModel.SearchResult(
                            prediction.Description,
                            prediction.PlaceId));
            }
        }

        void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            var searchResult = (LocationSearchPageViewModel.SearchResult)e.Item;

            HandleItemTapped(searchResult).FireAndForgetAsync(App.Current.ErrorHandler);
        }

        async Task HandleItemTapped(LocationSearchPageViewModel.SearchResult searchResult)
        {
            if (!canTap)
                return;
            canTap = false;

            string sessionToken = SearchBar.SessionToken;
            SearchBar.SessionToken = Guid.NewGuid().ToString();

            try
            {
                DetailsResponse response = await DetailsApi.GetPlaceGeometry(
                    searchResult.Id, SearchBar.ApiKey, sessionToken);

                if (response == null)
                {
                    await this.GetPageParent()?.DisplayAlert("No result", "Something went wrong.", "Ok");
                    return;
                }

                GeoCoordinates location = new GeoCoordinates(
                    response.Result.Geometry.Location.Latitude,
                    response.Result.Geometry.Location.Longitude);

                viewModel.SelectLocation(searchResult.Name, location);
            }
            catch (Exception ex)
            {
                // TODO Exceptions we can handle more neatly:
                // HttpRequestException
                // GoogleApiKeyException

                App.Current.ErrorHandler.HandleError(ex);
                return;
            }
            finally
            {
                canTap = true;
            }
        }

        bool canTap = true;
        LocationSearchPageViewModel viewModel;
    }
}
