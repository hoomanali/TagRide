using System;
namespace TagRides
{
    /// <summary>
    /// Two named locations.
    /// </summary>
    public class NamedOriginAndDestination
    {
        public NamedLocation Origin { get; }
        public NamedLocation Destination { get; }

        public NamedOriginAndDestination(NamedLocation origin, NamedLocation destination)
        {
            Origin = origin;
            Destination = destination;
        }
    }
}
