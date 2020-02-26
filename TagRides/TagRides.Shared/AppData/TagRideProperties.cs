using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace TagRides.Shared.AppData
{
    /// <summary>
    /// Properties for the app that can be changed and updated live
    /// </summary>
    [JsonObject]
    public class TagRideProperties
    {
        /// <summary>
        /// The URI base for theme related resources, including a trailing slash
        /// </summary>
        [JsonProperty]
        public readonly string ThemeResourceBase = "https://tagrides.blob.core.windows.net/public-resources/fantasy/";

        [JsonProperty]
        public readonly string GameItemDefaultIcon = "default.png";

        [JsonProperty]
        public readonly FactionProperties[] Factions =
        {
            new FactionProperties("Slugs", "slugIcon.png"),
            new FactionProperties("Deer", "deerIcon.png"),
            new FactionProperties("Turkey", "turkeyIcon.png")
        };
    }
}
