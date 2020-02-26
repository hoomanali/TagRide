using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace TagRides.Shared.Utilities
{
    /// <summary>
    /// A class that can be used to initialize a variable asynchronously.
    /// </summary>
    /// <remarks>
    /// Inspired by https://blog.stephencleary.com/2012/08/asynchronous-lazy-initialization.html
    /// </remarks>
    public class AsyncLazy<T>
    {
        /// <summary>
        /// Gets the value synchronously. This will block until the value is loaded,
        /// and otherwise this will return immediately.
        /// </summary>
        public T Value
        {
            get => instance.Value.Result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TagRides.Shared.Utilities.AsyncLazy`1"/> class.
        /// </summary>
        /// <param name="factory">Delegate that will run on a background thread
        /// when the value is needed for the first time.</param>
        public AsyncLazy(Func<T> factory)
        {
            instance = new Lazy<Task<T>>(() => Task.Run(factory));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TagRides.Shared.Utilities.AsyncLazy`1"/> class.
        /// </summary>
        /// <param name="factory">Delegate that will run on a background thread
        /// when the value is needed for the first time.</param>
        public AsyncLazy(Func<Task<T>> factory)
        {
            instance = new Lazy<Task<T>>(() => Task.Run(factory));
        }

        /// <summary>
        /// Allows this object to be awaited.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public TaskAwaiter<T> GetAwaiter()
        {
            return instance.Value.GetAwaiter();
        }

        /// <summary>
        /// Starts initializing the value.
        /// </summary>
        public void BeginInitializingValue()
        {
            _ = instance.Value;
        }

        readonly Lazy<Task<T>> instance;
    }
}
