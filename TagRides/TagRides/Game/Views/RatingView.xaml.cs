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
    public partial class RatingView : ContentView
    {
        #region Bindable Property Declarations

        public static readonly BindableProperty RatingProperty
            = BindableProperty.Create(
                nameof(Rating),
                typeof(Rating),
                typeof(RatingView), propertyChanged: OnRatingChanged);
        static void OnRatingChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var view = bindable as RatingView;
            view.mainStack.BindingContext = newValue;
        }

        #endregion

        public Rating Rating
        {
            get => (Rating)GetValue(RatingProperty);
            set => SetValue(RatingProperty, value);
        }

        public RatingView()
        {
            InitializeComponent();
        }
    }
}