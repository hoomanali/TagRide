using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagRides.Shared.Game;
using TagRides.ViewUtilities;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TagRides.Game.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GameItemView : ContentView
    {
        #region Bindable Property Declarations

        public static BindableProperty ItemProperty
            = BindableProperty.Create(
                nameof(Item),
                typeof(GameItem),
                typeof(GameItemView),
                propertyChanged: OnItemPropertyChanged);

        static void OnItemPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            GameItemView view = bindable as GameItemView;
            GameItem item = newValue as GameItem;

            view.nameLabel.Text = item.Name;
            view.image.Source = item.IconPath.ToGameItemIcon();
            if (item.EffectType != GameItem.Effect.None)
            {
                view.effectLabel.IsVisible = true;
                view.countLabel.IsVisible = true;

                view.effectLabel.Text = $"Affects {item.EffectType} by {item.EffectMultiplier.ToString("0.0")}x";
                view.countLabel.Text = $"Amount: {item.Count}";
                if (item.MaxCount != -1)
                    view.countLabel.Text += $"/{item.MaxCount}";
            }
            else
            {
                view.effectLabel.IsVisible = false;
                view.countLabel.IsVisible = false;
            }
        }

        #endregion

        public GameItem Item
        {
            get => GetValue(ItemProperty) as GameItem;
            set => SetValue(ItemProperty, value);
        }

        public GameItemView()
        {
            InitializeComponent();
        }
    }
}