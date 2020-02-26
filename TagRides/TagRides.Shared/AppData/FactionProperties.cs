using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace TagRides.Shared.AppData
{
    /// <summary>
    /// Defines some properties for a faction.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class FactionProperties
    {
        [JsonProperty]
        public readonly string Name;
        [JsonProperty]
        public readonly string IconName;

        public FactionProperties(string name, string iconName)
        {
            Name = name;
            IconName = iconName;
        }
    }
}
