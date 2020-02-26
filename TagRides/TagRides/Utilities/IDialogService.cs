using System;
using System.Threading.Tasks;

namespace TagRides.Utilities
{
    /// <summary>
    /// Abstracts the ability to display a dialog popup to the user. This is useful
    /// to display dialogs from view models.
    /// </summary>
    public interface IDialogService
    {
        Task<bool> DisplayAlert(string title, string message, string accept, string cancel);
        Task DisplayAlert(string title, string message, string cancel);
    }

    /// <summary>
    /// Implements <see cref="IDialogService"/> by returning as if the user pressed cancel.
    /// </summary>
    public class NoDialogService : IDialogService
    {
        public Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        {
            return Task.FromResult(false);
        }

        public Task DisplayAlert(string title, string message, string cancel)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Holds a weak reference to an IDialogService. If the target of the
    /// reference expires, this acts like <see cref="NoDialogService"/>.
    /// </summary>
    public class DialogServiceWeakWrapper : IDialogService
    {
        public DialogServiceWeakWrapper(IDialogService dialogService)
        {
            dialogServiceRef = new WeakReference<IDialogService>(dialogService);
        }

        public Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        {
            if (dialogServiceRef.TryGetTarget(out var dialogService))
                return dialogService.DisplayAlert(title, message, accept, cancel);

            return Task.FromResult(false);
        }

        public Task DisplayAlert(string title, string message, string cancel)
        {
            if (dialogServiceRef.TryGetTarget(out var dialogService))
                return dialogService.DisplayAlert(title, message, cancel);

            return Task.CompletedTask;
        }

        WeakReference<IDialogService> dialogServiceRef;
    }
}
