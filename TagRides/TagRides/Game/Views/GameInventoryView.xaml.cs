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
	public partial class GameInventoryView : ContentView
    {
        #region Bindable Property Declarations

        public static BindableProperty InventoryProperty
            = BindableProperty.Create(
                nameof(Inventory),
                typeof(GameInventory),
                typeof(GameInventoryView),
                propertyChanged: OnInventoryChanged);

        static void OnInventoryChanged(BindableObject bindable, object oldValue, object newValue)
        {
            GameInventoryView view = bindable as GameInventoryView;
            GameInventory inv = newValue as GameInventory;

            view.mainLayout.Children.Clear();

            if (inv != null && inv.Items.Count() != 0)
            {
                foreach (var item in inv.Items)
                {
                    if (!view.ShowEmpty && item.Count == 0) continue;
                    if (!view.ShowNoneEffect && item.EffectType == GameItem.Effect.None) continue;

                    TapGestureRecognizer tgr = new TapGestureRecognizer
                    {
                        NumberOfTapsRequired = 1
                    };

                    tgr.Tapped += view.OnItemTapped;

                    view.mainLayout.Children.Add(
                        new GameItemView
                        {
                            Item = item,
                            GestureRecognizers = { tgr }
                        });
                }
            }

            if (view.mainLayout.Children.Count == 0)
            {
                view.mainLayout.Children.Add(
                    new Label
                    {
                        Text = "No items"
                    });
            }
        }

        #endregion

        public event Action<GameItem> SelectionChanged;

        public GameInventory Inventory
        {
            get => GetValue(InventoryProperty) as GameInventory;
            set => SetValue(InventoryProperty, value);
        }

        public GameItemView Selected { get; private set; } = null;

        public Color SelectionColor
        {
            get => selectionColor;
            set
            {
                if (value == selectionColor) return;
                selectionColor = value;
                if (Selected != null)
                    Selected.BackgroundColor = value;
            }
        }

        public bool ShowNoneEffect
        {
            get => showNoneEffect;
            set
            {
                if (showNoneEffect == value)
                    return;

                showNoneEffect = value;

                if (!showNoneEffect && Selected != null && Selected.Item.EffectType == GameItem.Effect.None)
                    ClearSelection();

                OnInventoryChanged(this, Inventory, Inventory);
            }
        }

        public bool ShowEmpty
        {
            get => showEmpty;
            set
            {
                if (showEmpty == value)
                    return;

                showEmpty = value;

                if (!showEmpty && Selected != null && Selected.Item.Count == 0)
                    ClearSelection();

                OnInventoryChanged(this, Inventory, Inventory);
            }
        }

        public bool CanSelect
        {
            get => canSelect;
            set
            {
                if (canSelect == value)
                    return;

                if (canSelect && Selected != null)
                    ClearSelection();

                canSelect = value;
            }
        }

        public GameInventoryView()
		{
			InitializeComponent();
		}

        public void ClearSelection()
        {
            if (Selected == null)
                return;

            Selected.BackgroundColor = Color.Transparent;
            Selected = null;
            SelectionChanged?.Invoke(null);
        }

        void OnItemTapped(object sender, EventArgs e)
        {
            if (!canSelect || sender == Selected) return;
            if (Selected != null)
                Selected.BackgroundColor = Color.Transparent;

            Selected = sender as GameItemView;
            Selected.BackgroundColor = selectionColor;

            SelectionChanged?.Invoke(Selected.Item);
        }

        bool showNoneEffect = false;
        bool showEmpty = false;

        bool canSelect = false;
        Color selectionColor = Color.BurlyWood;
	}
}