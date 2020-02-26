using System;
using System.Collections.Generic;

namespace TagRides.Shared.Utilities
{
    public class ThreadSafeEvent<T>
    {
        public ThreadSafeEvent(IEventProvider<T> token)
        {
            token.Occurred += Invoke;
        }

        public void Add(Action<T> handler)
        {
            lock (handlerLock)
                handlers.Add(handler);
        }

        public void Remove(Action<T> handler)
        {
            lock (handlerLock)
                handlers.Remove(handler);
        }

        /// <summary>
        /// Atomically swaps out the given handler for a new one. This is useful
        /// if <paramref name="previousHandler"/> acquires a lock that the current
        /// thread is about to acquire. See <see cref="EventSwapLockScope{T}"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if <paramref name="previousHandler"/> was not registered to the event.</exception>
        /// <param name="previousHandler">Previous handler.</param>
        /// <param name="newHandler">New handler.</param>
        public void Swap(Action<T> previousHandler, Action<T> newHandler)
        {
            lock (handlerLock)
            {
                int prevIdx = handlers.IndexOf(previousHandler);

                if (prevIdx < 0)
                    throw new ArgumentException("Cannot swap with unregistered handler.");

                handlers[prevIdx] = newHandler;
            }
        }

        void Invoke(T data)
        {
            List<Action<T>> handlersCopy;

            lock (handlerLock)
                handlersCopy = new List<Action<T>>(handlers);

            // Avoid invoking unknown methods while holding a lock to avoid
            // potential deadlock (a handler could result in another Invoke
            // call).
            foreach (var handler in handlersCopy)
                handler(data);
        }

        readonly List<Action<T>> handlers = new List<Action<T>>();
        readonly object handlerLock = new object();
    }
}
