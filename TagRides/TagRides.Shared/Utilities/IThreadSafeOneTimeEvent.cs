using System;
using System.Collections.Generic;

namespace TagRides.Shared.Utilities
{
    /// <summary>
    /// Interface for a thread-safe one-time event.
    /// </summary>
    /// <remarks>
    /// This interface does not provide a "DidFire" property because the event
    /// may fire immediately after a DidFire check. Instead of writing code
    /// like
    /// <code>
    /// if (evt.DidFire)
    ///     a();
    /// </code>
    /// 
    /// Use the continuation pattern:
    /// <code>
    /// evt.RunWhenFired(_ => a());
    /// </code>
    /// 
    /// A <code>DidFire == false</code> result would be inconclusive, and any
    /// code that relies on it is almost certainly not thread safe.
    /// </remarks>
    public interface IThreadSafeOneTimeEvent<T>
    {
        /// <summary>
        /// If the event has already fired, runs the handler immediately. Otherwise,
        /// adds the handler to the event.
        /// </summary>
        void RunWhenFired(Action<T> handler);

        /// <summary>
        /// Removes the handler from the event.
        /// </summary>
        void Remove(Action<T> handler);

        /// <summary>
        /// Atomically swaps out the given handler for a new one. This is useful
        /// if <paramref name="previousHandler"/> acquires a lock that the current
        /// thread is about to acquire. See <see cref="EventSwapLockScope{T}"/>.
        /// If the event has already run, this does nothing.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if <paramref name="previousHandler"/> was not registered to the event.</exception>
        /// <param name="previousHandler">Previous handler.</param>
        /// <param name="newHandler">New handler.</param>
        void Swap(Action<T> previousHandler, Action<T> newHandler);
    }
}
