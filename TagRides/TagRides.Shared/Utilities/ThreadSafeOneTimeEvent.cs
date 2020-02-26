using System;
using System.Collections.Generic;

namespace TagRides.Shared.Utilities
{
    public class ThreadSafeOneTimeEvent<T> : IThreadSafeOneTimeEvent<T>
    {
        public ThreadSafeOneTimeEvent(IEventProvider<T> token)
        {
            token.Occurred += Invoke;
        }

        public void RunWhenFired(Action<T> handler)
        {
            if (handler == null)
                throw new ArgumentNullException();

            bool runHandler = false;

            lock (didRunLock)
            {
                if (didRun)
                {
                    runHandler = true;
                }
                else
                {
                    // handlers is not null because didRun is false
                    handlers.Add(handler);
                }
            }

            if (runHandler)
                handler(data);
        }

        public void Remove(Action<T> handler)
        {
            lock (didRunLock)
            {
                if (didRun)
                    return;

                handlers?.Remove(handler);
            }
        }

        public void Swap(Action<T> previousHandler, Action<T> newHandler)
        {
            lock (didRunLock)
            {
                if (didRun)
                    return;

                int prevIdx = handlers.IndexOf(previousHandler);

                if (prevIdx < 0)
                    throw new ArgumentException("Cannot swap with unregistered handler.");

                handlers[prevIdx] = newHandler;
            }
        }

        void Invoke(T obj)
        {
            bool runHandlers = false;

            lock (didRunLock)
            {
                if (!didRun)
                {
                    didRun = true;
                    data = obj;

                    runHandlers = true;
                }
            }

            if (runHandlers)
            {
                // Avoid invoking unknown methods while holding a lock to avoid
                // potential deadlock (a handler could result in another Invoke
                // call).
                // NOTE: handlers will not change because didRun is true.
                foreach (var handler in handlers)
                    handler(data);

                handlers = null;
            }
        }

        bool didRun;
        T data;

        List<Action<T>> handlers = new List<Action<T>>();
        readonly object didRunLock = new object();
    }
}
