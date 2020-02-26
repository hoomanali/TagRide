using System;
using System.Threading.Tasks;

namespace TagRides.Shared.Utilities
{
    public interface IErrorHandler
    {
        void HandleError(Exception e);
    }

    public static class TaskUtilities
    {
        /// <summary>
        /// Safely runs the given task asynchronously, passing any errors to the error handler.
        /// </summary>
        /// <remarks>
        /// Taken from https://johnthiriet.com/removing-async-void/.
        /// </remarks>
        /// <param name="task">Task.</param>
        /// <param name="errorHandler">Error handler.</param>
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        public static async void FireAndForgetAsync(this Task task, IErrorHandler errorHandler)
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                errorHandler.HandleError(e);
            }
        }

        /// <summary>
        /// If the task throws an unhandled exception, passes that exception
        /// to the error handler.
        /// </summary>
        /// <returns>The task consisting of the original task followed by
        /// the error handling continuation.</returns>
        /// <param name="task">Task.</param>
        /// <param name="handler">Error handler.</param>
        public static Task OnError(this Task task, IErrorHandler handler)
        {
            void Continuation(Task t)
            {
                handler.HandleError(t.Exception.InnerException);
            }

            return task.ContinueWith(Continuation, TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void WaitSync(Func<Task> func)
        {
            Task.Run(func).Wait();
        }
        
        public static T GetResultSync<T>(Func<Task<T>> func)
        {
            return Task.Run(func).Result;
        }

        public static T GetResultSync<T, P>(Func<P, Task<T>> func, P p1)
        {
            return Task.Run(async () => await func(p1)).Result;
        }
    }
}
