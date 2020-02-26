using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using TagRides.Shared.Game;

namespace TagRides.Shared.RideData
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RequestGameElements
    {
        [JsonProperty]
        public readonly string EquippedItemName;

        public RequestGameElements(GameItem equippedItem)
        {
            if (equippedItem != null)
                EquippedItemName = equippedItem.Name;
        }
    }
}
