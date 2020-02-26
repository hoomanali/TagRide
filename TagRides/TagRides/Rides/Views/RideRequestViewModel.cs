using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TagRides.Shared.Game;
using TagRides.Shared.Geo;
using TagRides.Shared.RideData;

namespace TagRides.Rides.Views
{
    public class RideRequestViewModel : RideRelatedRequestViewModel
    {
        public RideRequestViewModel(
            Func<RequestBase, Task> submit,
            Func<Task> cancel,
            Action<GeoCoordinates, Action<NamedLocation>> pickLocation,
            NamedLocation start,
            NamedLocation end)
            : base(submit, cancel, pickLocation, start, end)
        { }

        protected override RequestBase MakeRequest()
        {
            GameItem item = equippedItem.EffectType == GameItem.Effect.None ? null : equippedItem;

            return new RideRequest(
                new Trip(departureTime, start.Coordinates, end.Coordinates),
                new RequestGameElements(item));
        }
    }
}
