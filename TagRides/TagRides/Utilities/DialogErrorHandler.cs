using System;
using TagRides.Shared.Utilities;

namespace TagRides.Utilities
{
    /// <summary>
    /// Implementation of IErrorHandler that displays the error with a popup
    /// dialog.
    /// </summary>
    public class DialogErrorHandler : IErrorHandler
    {
        public IDialogService DialogService { get; set; }

        public DialogErrorHandler(IDialogService dialogService)
        {
            DialogService = dialogService;
        }

        public void HandleError(Exception e)
        {
            DialogService?.DisplayAlert("Caught exception", e.ToString(), "Okay");
        }
    }
}
