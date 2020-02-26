using System;
namespace TagRides.Shared.Utilities
{
    /// <summary>
    /// An interface for something that holds a single event. You should
    /// generally instantiate this using <see cref="RaisableEvent{T}"/>.
    /// </summary>
    public interface IEventProvider<T>
    {
        event Action<T> Occurred;
    }
}
