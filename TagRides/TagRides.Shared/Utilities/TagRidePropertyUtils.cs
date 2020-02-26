using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using TagRides.Shared.AppData;

namespace TagRides.Shared.Utilities
{
    public static class TagRidePropertyUtils
    {
        public static FactionProperties GetFaction(this TagRideProperties properties, string factionName)
        {
            return properties.Factions.FirstOrDefault((f) => f.Name == factionName);
        }

        public static Uri FactionIconUri(TagRideProperties properties, string factionName)
        {
            var faction = properties.GetFaction(factionName);

            return FactionIconUri(properties, faction);
        }

        public static Uri FactionIconUri(TagRideProperties properties, FactionProperties faction)
        {
            if (properties == null || faction == null)
                return null;

            return new Uri(properties.ThemeResourceBase + faction.IconName);
        }
    }
}
