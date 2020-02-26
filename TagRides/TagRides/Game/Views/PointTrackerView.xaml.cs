using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TagRides.Shared.Game;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TagRides.Game.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PointTrackerView : ContentView
    {
        #region Bindable Property Declarations

        public static readonly BindableProperty PointTrackerProperty
            = BindableProperty.Create(
                nameof(PointTracker),
                typeof(PointTracker),
                typeof(PointTrackerView), propertyChanged: OnPointTrackerChanged);
        static void OnPointTrackerChanged(BindableObject bindable, object oldValue, object newValue)
        {
            PointTrackerView view = bindable as PointTrackerView;
            view.mainStack.BindingContext = newValue;
        }

        #endregion

        public PointTracker PointTracker
        {
            get => (PointTracker)GetValue(PointTrackerProperty);
            set => SetValue(PointTrackerProperty, value);
        }

        public PointTrackerView()
        {
            InitializeComponent();
        }

        public string LabelText
        {
            get => TrackerName.Text;
            set
            {
                TrackerName.Text = value;
            }
        }

        public Color BarColor
        {
            get => progressBar.ProgressColor;
            set
            {
                progressBar.ProgressColor = value;
            }
        }
    }
}