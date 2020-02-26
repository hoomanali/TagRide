using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace TagRides.Shared.RideData
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RequestBase
    {
        [JsonProperty]
        public readonly RequestGameElements GameElements;

        public RequestBase(RequestGameElements gameElements)
        {
            GameElements = gameElements;
        }
    }
}
