using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using TagRides.Shared.Utilities;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using Newtonsoft.Json;
using TagRides.Places.Autocomplete;

namespace TagRides.Places
{
    /// <summary>
    /// A searchbar that gets autocomplete results from the Google Places API.
    /// This class has a <see cref="PredictionsUpdated"/> event that is fired
    /// whenever the autocomplete results have changed, but it does not render
    /// the results itself (it is only a searchbar).
    /// 
    /// To use this, we must either display a Google map or show the
    /// "powered by Google" logo: https://developers.google.com/places/web-service/policies#logo_requirements
    /// </summary>
    public class PlacesSearchBar : SearchBar
    {
        #region Bindable property declarations
        public static readonly BindableProperty ApiKeyProperty
            = BindableProperty.Create(
                nameof(ApiKey),
                typeof(string),
                typeof(PlacesSearchBar));

        public static readonly BindableProperty ErrorHandlerProperty
            = BindableProperty.Create(
                nameof(ErrorHandler),
                typeof(IErrorHandler),
                typeof(PlacesSearchBar));

        public static readonly BindableProperty SessionTokenProperty
            = BindableProperty.Create(
                nameof(SessionToken),
                typeof(string),
                typeof(PlacesSearchBar));
        #endregion

        /// <summary>
        /// The Google Maps API key to use.
        /// </summary>
        /// <value>The API key.</value>
        public string ApiKey
        {
            get => (string)GetValue(ApiKeyProperty);
            set => SetValue(ApiKeyProperty, value);
        }

        /// <summary>
        /// The session token to use. This can be any random string. Google
        /// recommends using a "version 4 UUID". This should be set before
        /// the user begins to type in the searchbar, and reset after it
        /// is used in the <see cref="Details.DetailsApi"/>.
        /// </summary>
        /// <value>The session token.</value>
        public string SessionToken
        {
            get => (string)GetValue(SessionTokenProperty);
            set => SetValue(SessionTokenProperty, value);
        }

        /// <summary>
        /// The <see cref="IErrorHandler"/> to use for reporting uncaught
        /// exceptions in async code.
        /// </summary>
        /// <value>The error handler.</value>
        public IErrorHandler ErrorHandler
        {
            get => (IErrorHandler)GetValue(ErrorHandlerProperty);
            set => SetValue(ErrorHandlerProperty, value);
        }

        /// <summary>
        /// Occurs when the autocomplete predictions are updated.
        /// </summary>
        public event Action<AutocompleteResponse> PredictionsUpdated;

        public PlacesSearchBar()
        {
            TextChanged += (sender, e) => SetNextUpdateCommand(new UpdateCommand(true, e.NewTextValue));
            SearchButtonPressed += (sender, e) => SetNextUpdateCommand(new UpdateCommand(false, Text));
        }

        void SetNextUpdateCommand(UpdateCommand updateCommand)
        {
            lock (updateLock)
            {
                switch (updatePhase)
                {
                    case UpdatePhase.None:
                        updatePhase = UpdatePhase.SearchInProgress;

                        BeginSearch(updateCommand).FireAndForgetAsync(ErrorHandler);

                        break;

                    case UpdatePhase.SearchInProgress:
                        nextUpdateCommand = updateCommand;
                        break;

                    case UpdatePhase.DelayInProgress:
                        nextUpdateCommand = updateCommand;

                        if (!updateCommand.delayBeforeSearch)
                            delayToken.Cancel();

                        break;
                }
            }
        }

        /// <summary>
        /// Begins an autocomplete query, starting with the given command.
        /// If <see cref="nextUpdateCommand"/> is non-null at the end of the search,
        /// this will set it to null and perform a new search.
        /// </summary>
        /// <returns>The search.</returns>
        /// <param name="command">Search term and whether there should be a delay
        /// between the previous search and this search.</param>
        async Task BeginSearch(UpdateCommand command)
        {
            while (true)
            {
                AutocompleteResponse response = await AutocompleteQuery(command.nextSearchTerm);
                PredictionsUpdated?.Invoke(response);

                lock (updateLock)
                {
                    if (nextUpdateCommand == null)
                    {
                        updatePhase = UpdatePhase.None;
                        break;
                    }

                    command = nextUpdateCommand.Value;
                    nextUpdateCommand = null;

                    if (command.delayBeforeSearch)
                    {
                        updatePhase = UpdatePhase.DelayInProgress;
                        delayToken = new CancellationTokenSource();
                    }
                    else
                        updatePhase = UpdatePhase.SearchInProgress;
                }

                if (command.delayBeforeSearch)
                {
                    try
                    {
                        // Wait a bit to reduce total number of requests.
                        await Task.Delay(100, delayToken.Token);
                    }
                    catch (TaskCanceledException)
                    {
                    }

                    lock (updateLock)
                    {
                        if (nextUpdateCommand != null)
                            command = nextUpdateCommand.Value;

                        // We already delayed, so we don't need to delay before
                        // this command.
                        updatePhase = UpdatePhase.SearchInProgress;
                    }
                }
            }
        }

        /// <summary>
        /// Performs a Google Places API autocomplete query.
        /// https://developers.google.com/places/web-service/autocomplete
        /// </summary>
        /// <returns>The result or null.</returns>
        /// <param name="term">The query.</param>
        async Task<AutocompleteResponse> AutocompleteQuery(string term)
        {
            Position currentPosition = await CrossGeolocator.Current.GetPositionAsync();

            Uri baseUri = new Uri("https://maps.googleapis.com/maps/api/place/autocomplete/json");

            Uri requestUri = baseUri
                .AddParameter("input", term)
                .AddParameter("key", ApiKey)

                // Prefer results closer to user's current location.
                .AddParameter("location", $"{currentPosition.Latitude},{currentPosition.Longitude}")
                .AddParameter("radius", $"2000"); // 2km radius

            // It is important to use a session token to reduce the amount
            // that we are charged.
            if (!string.IsNullOrWhiteSpace(SessionToken))
                requestUri = requestUri.AddParameter("sessiontoken", SessionToken);

            try
            {
                // It is recommended to use a single HttpClient for the duration of the app.
                HttpResponseMessage response = await App.Current.HttpClient.GetAsync(requestUri);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine($"Unsuccessful autocomplete query: {response.ReasonPhrase}");
                    return null;
                }

                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AutocompleteResponse>(result);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Got HTTP exception in {nameof(PlacesSearchBar)}: {e.Message}");
                return null;
            }
        }

        readonly object updateLock = new object();
        UpdateCommand? nextUpdateCommand;
        UpdatePhase updatePhase;

        // Can be used to cancel delay phase and immediately execute
        // the next update command.
        CancellationTokenSource delayToken;

        struct UpdateCommand
        {
            public readonly bool delayBeforeSearch;
            public readonly string nextSearchTerm;

            public UpdateCommand(bool delay, string nextSearchTerm)
            {
                this.delayBeforeSearch = delay;
                this.nextSearchTerm = nextSearchTerm;
            }
        }

        enum UpdatePhase
        {
            None,
            SearchInProgress,
            DelayInProgress
        }
    }
}
