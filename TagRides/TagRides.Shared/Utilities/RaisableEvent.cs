using System;
namespace TagRides.Shared.Utilities
{
    public class RaisableEvent<T> : IEventProvider<T>
    {
        public event Action<T> Occurred;

        public void RaiseEvent(T data)
        {
            Occurred?.Invoke(data);
        }
    }
}
