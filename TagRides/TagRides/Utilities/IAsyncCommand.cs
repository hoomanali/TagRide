/*
 * The code here is inspired by and heavily borrowed from https://johnthiriet.com/mvvm-going-async-with-async-command/
 * */

using System;
using System.Threading.Tasks;
using System.Windows.Input;
using TagRides.Shared.Utilities;

namespace TagRides.Utilities
{
    /// <summary>
    /// An ICommand with asynchronous support.
    /// </summary>
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
    }

    /// <summary>
    /// An ICommand with asynchronous support that accepts an argument.
    /// </summary>
    public interface IAsyncCommand<T> : ICommand
    {
        Task ExecuteAsync(T parameter);
        bool CanExecute(T parameter);
    }

    /// <summary>
    /// Implementation of <see cref="IAsyncCommand"/>. Executes the given task
    /// in a fire-and-forget fashion.
    /// </summary>
    public class AsyncCommand : IAsyncCommand
    {
        public event EventHandler CanExecuteChanged;

        public AsyncCommand(Func<Task> action, Func<bool> canExecute = null, IErrorHandler errorHandler = null)
        {
            this.action = action;
            this.canExecute = canExecute;
            this.errorHandler = errorHandler;

            isExecuting = false;
        }

        public async Task ExecuteAsync()
        {
            if (CanExecute())
            {
                try
                {
                    isExecuting = true;
                    await action();
                }
                finally
                {
                    isExecuting = false;
                }
            }

            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute()
        {
            return !isExecuting && (canExecute?.Invoke() ?? true);
        }

        public void ChangeCanExecute()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Explicit Implementations
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            ExecuteAsync().FireAndForgetAsync(errorHandler);
        }
        #endregion

        readonly Func<Task> action;
        readonly Func<bool> canExecute;
        readonly IErrorHandler errorHandler;
        bool isExecuting;
    }

    /// <summary>
    /// Implementation of <see cref="IAsyncCommand{T}"/>. Executes the given task
    /// in a fire-and-forget fashion.
    /// </summary>
    public class AsyncCommand<T> : IAsyncCommand<T>
    {
        public event EventHandler CanExecuteChanged;

        public AsyncCommand(Func<T, Task> action, Func<T, bool> canExecute = null, IErrorHandler errorHandler = null)
        {
            this.action = action;
            this.canExecute = canExecute;
            this.errorHandler = errorHandler;

            isExecuting = false;
        }

        public async Task ExecuteAsync(T parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    isExecuting = true;
                    await action(parameter);
                }
                finally
                {
                    isExecuting = false;
                }
            }

            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(T parameter)
        {
            return !isExecuting && (canExecute?.Invoke(parameter) ?? true);
        }

        public void ChangeCanExecute()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Explicit Implementations
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute((T)parameter);
        }

        void ICommand.Execute(object parameter)
        {
            ExecuteAsync((T)parameter).FireAndForgetAsync(errorHandler);
        }
        #endregion

        readonly Func<T, Task> action;
        readonly Func<T, bool> canExecute;
        readonly IErrorHandler errorHandler;
        bool isExecuting;
    }

    public class DelegatedAsyncCommand : IAsyncCommand
    {
        public IAsyncCommand Other
        {
            get => other;
            set
            {
                if (other != null)
                    other.CanExecuteChanged -= HandleCanExecuteChanged;

                other = value;

                if (other != null)
                    other.CanExecuteChanged += HandleCanExecuteChanged;

                HandleCanExecuteChanged(this, EventArgs.Empty);
            }
        }

        public DelegatedAsyncCommand()
        {
            Other = null;
        }

        public DelegatedAsyncCommand(IAsyncCommand other)
        {
            Other = other;
        }

        public event EventHandler CanExecuteChanged;

        public Task ExecuteAsync()
        {
            return Other?.ExecuteAsync() ?? Task.CompletedTask;
        }

        public bool CanExecute()
        {
            return Other?.CanExecute() ?? false;
        }

        bool ICommand.CanExecute(object parameter)
        {
            return Other?.CanExecute(parameter) ?? false;
        }

        void ICommand.Execute(object parameter)
        {
            Other?.Execute(parameter);
        }

        void HandleCanExecuteChanged(object sender, EventArgs e)
        {
            CanExecuteChanged?.Invoke(sender, e);
        }

        IAsyncCommand other;
    }

    public class DelegatedAsyncCommand<T> : IAsyncCommand<T>
    {
        public IAsyncCommand<T> Other
        {
            get => other;
            set
            {
                if (other != null)
                    other.CanExecuteChanged -= HandleCanExecuteChanged;

                other = value;

                if (other != null)
                    other.CanExecuteChanged += HandleCanExecuteChanged;

                HandleCanExecuteChanged(this, EventArgs.Empty);
            }
        }

        public DelegatedAsyncCommand()
        {
            Other = null;
        }

        public DelegatedAsyncCommand(IAsyncCommand<T> other)
        {
            Other = other;
        }

        public event EventHandler CanExecuteChanged;

        public Task ExecuteAsync(T param)
        {
            return Other?.ExecuteAsync(param) ?? Task.CompletedTask;
        }

        public bool CanExecute(T param)
        {
            return Other?.CanExecute(param) ?? false;
        }

        bool ICommand.CanExecute(object parameter)
        {
            return Other?.CanExecute(parameter) ?? false;
        }

        void ICommand.Execute(object parameter)
        {
            Other?.Execute(parameter);
        }

        void HandleCanExecuteChanged(object sender, EventArgs e)
        {
            CanExecuteChanged?.Invoke(sender, e);
        }

        IAsyncCommand<T> other;
    }
}
