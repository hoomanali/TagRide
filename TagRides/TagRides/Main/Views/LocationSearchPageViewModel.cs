using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Essentials;
using TagRides.Utilities;
using TagRides.Shared.Utilities;
using TagRides.Shared.Geo;
using TagRides.Places.Autocomplete;

namespace TagRides.Main.Views
{
    public class LocationSearchPageViewModel
    {
        public ObservableCollection<SearchResult> SearchResults { get; }
        public IErrorHandler ErrorHandler { get; set; }

        public LocationSearchPageViewModel(Action<NamedLocation> selectLocation)
        {
            this.selectLocation = selectLocation;

            ErrorHandler = App.Current.ErrorHandler;
            SearchResults = new ObservableCollection<SearchResult>();
        }

        public void SelectLocation(string name, GeoCoordinates location)
        {
            selectLocation(new NamedLocation(name, location));
        }

        public class SearchResult
        {
            public string Name { get; }
            public string Id { get; }

            public SearchResult(string name, string id)
            {
                Name = name;
                Id = id;
            }
        }

        readonly Action<NamedLocation> selectLocation;
    }
}
