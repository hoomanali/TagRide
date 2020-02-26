using System;
using System.Threading;
using System.Collections.Generic;

namespace TagRides.Shared.Utilities
{
    /// <summary>
    /// Disposable class that can be used to temporarily remove an event handler
    /// from a <see cref="ThreadSafeEvent{T}"/> and optionally acquire a lock.
    /// When the class is disposed, the lock is released and the original
    /// event handler is added back in. If the event was raised while the handler
    /// was swapped out, the handler will be invoked once for each invocation
    /// in order.
    /// </summary>
    public class EventSwapLockScope<T> : IDisposable
    {
        public EventSwapLockScope(
            ThreadSafeEvent<T> evt,
            Action<T> handler,
            object lck = null)
        {
            originalHandler = handler;
            givenLock = lck;
            givenEvent = evt;

            givenEvent.Swap(originalHandler, OnEvent);

            if (givenLock != null)
                Monitor.Enter(givenLock);
        }

        void IDisposable.Dispose()
        {
            if (givenLock != null)
                Monitor.Exit(givenLock);

            givenEvent.Swap(OnEvent, originalHandler);

            foreach (var data in eventData)
                originalHandler(data);
        }

        void OnEvent(T obj)
        {
            eventData.Enqueue(obj);
        }

        readonly Queue<T> eventData = new Queue<T>();

        readonly Action<T> originalHandler;
        readonly object givenLock;
        readonly ThreadSafeEvent<T> givenEvent;
    }
}
