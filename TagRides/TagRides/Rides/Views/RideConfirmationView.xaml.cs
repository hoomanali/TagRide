using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace TagRides.Rides.Views
{
    public partial class RideConfirmationView : ContentView
    {
        // TODO: Update the RideConfirmationView to display route, driver, etc.
        public RideConfirmationView(RideConfirmationViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
        }
    }
}
