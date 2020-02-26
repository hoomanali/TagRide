using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace TagRides.ViewUtilities
{
    /// <summary>
    /// Page which supports sliding views in or over the base page
    /// </summary>
	public class SlideView : ContentView
    {
        //Hide base.Content
        public View MainContent
        {
            get => mainContent.Content;
            set
            {
                mainContent.Content = value;
            }
        }

        public SlideView()
        {
            base.Content = mainLayout = new AbsoluteLayout
            {
                Children = {
                    mainContent
                },
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            AbsoluteLayout.SetLayoutFlags(mainContent, AbsoluteLayoutFlags.All);
            AbsoluteLayout.SetLayoutBounds(mainContent, new Rectangle(0, 0, 1, 1));
        }

        /// <summary>
        /// Slides in a view, splitting the page between the regular <see cref="Content"/> and <paramref name="view"/>
        /// Only one view can be added in this way at a time.
        /// </summary>
        /// <param name="view">The view to insert. This view is added to the page until <see cref="ClearOtherViews"/> is called.</param>
        /// <param name="name">A unique identifier for the animation</param>
        /// <param name="split">The proportion of the screen the added view will take up.</param>
        /// <param name="fromBottom">Whether or not the view should be on the bottom or top of the screen.</param>
        /// <param name="rate">Time in millis between animation updates.</param>
        /// <param name="length">How long the slide in should take.</param>
        /// <param name="easing">Easing function to use for the animation.</param>
        /// <param name="finished">Callback for when the animation finishes.</param>
        /// <param name="cleared">Callback when the view has been cleared.</param>
        /// <returns>If a view was already added, this does nothing and returns false.</returns>
        public bool SlideInView(
            View view,
            string name,
            double split = 0.5,
            bool fromBottom = true,
            uint rate = 16,
            uint length = 250,
            Easing easing = null,
            Action<double, bool> finished = null,
            Action cleared = null)
        {
            lock (slideInLock)
            {
                if (currentSlideInView.HasValue)
                    return false;

                //Prevents any other views being added before these values are final
                currentSlideInView = new SlideViewData();
            }

            mainLayout.Children.Add(view);
            AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.All);
            if (fromBottom)
                AbsoluteLayout.SetLayoutBounds(view, new Rectangle(0, 1, 0, 0));
            else
                AbsoluteLayout.SetLayoutBounds(view, new Rectangle(0, 0, 0, 0));

            void anim(double f)
            {
                double splitProg = f * split;
                AbsoluteLayout.SetLayoutBounds(mainContent, new Rectangle(0, fromBottom ? 0 : 1, 1, 1 - splitProg));
                AbsoluteLayout.SetLayoutBounds(view, new Rectangle(0, fromBottom ? 1 : 0, 1, splitProg));
            }

            Animation animation = new Animation(anim, 0, 1);

            animation.Commit(this, name, rate, length, easing, finished);

            currentSlideInView = new SlideViewData
            {
                Name = name,
                ClearAnimation = new Animation(anim, 1, 0),
                View = view,
                OnCleared = cleared
            };

            return true;
        }

        /// <summary>
        /// Slide a view over the <see cref="MainContent"/>
        /// One can slide over many views, but they can overlap one another
        /// </summary>
        /// <param name="view">The view to slide over</param>
        /// <param name="startBounds">The proportional bounds the new view should start in.
        /// Note that this will be the end position when cleared.</param>
        /// <param name="endBounds">The proportional bounds the new view should end in</param>
        /// <param name="name">Unique name with which to identify the slide over view</param>
        /// <param name="rate">Time in millis between animation updates</param>
        /// <param name="length">How long the animation will take</param>
        /// <param name="easing">Easing function</param>
        /// <param name="finished">Call back when the animation finishes</param>
        /// <param name="cleared">Call back when the view has been cleared</param>
        /// <returns></returns>
        public bool SlideOverView(
            View view,
            Rectangle startBounds,
            Rectangle endBounds,
            string name,
            uint rate = 16,
            uint length = 250,
            Easing easing = null,
            Action<double, bool> finished = null,
            Action cleared = null)
        {
            if (currentSlideOverViews.ContainsKey(name))
                return false;

            mainLayout.Children.Add(view);
            AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.All);

            AbsoluteLayout.SetLayoutBounds(view, startBounds);

            void anim(double f)
            {
                //TODO make a utility method?
                double
                    left = (endBounds.Left - startBounds.Left) * f + startBounds.Left,
                    top = (endBounds.Top - startBounds.Top) * f + startBounds.Top,
                    right = (endBounds.Right - startBounds.Right) * f + startBounds.Right,
                    bottom = (endBounds.Bottom - startBounds.Bottom) * f + startBounds.Bottom;

                AbsoluteLayout.SetLayoutBounds(view, Rectangle.FromLTRB(left, top, right, bottom));
            }

            Animation animation = new Animation(anim, 0, 1);

            animation.Commit(this, name, rate, length, easing, finished);

            SlideViewData data = new SlideViewData
            {
                Name = name,
                ClearAnimation = new Animation(anim, 1, 0),
                View = view,
                OnCleared = cleared
            };

            currentSlideOverViews.TryAdd(name, data);

            return true;
        }

        /// <summary>
        /// Clears the view added with <see cref="SlideInView(View, string, double, bool, uint, uint, Easing, Action{double, bool})"/>
        /// </summary>
        public void ClearSlideInView(uint rate = 16, uint length = 250, Easing easing = null)
        {
            if (!currentSlideInView.HasValue)
                return;

            string handle = currentSlideInView.Value.Name;
            Animation animation = currentSlideInView.Value.ClearAnimation;
            View view = currentSlideInView.Value.View;
            Action cleared = currentSlideInView.Value.OnCleared;

            this.AbortAnimation(handle);

            animation.Commit(this, handle, rate, length, easing, finished: (t, b) =>
            {
                mainLayout.Children.Remove(view);

                AbsoluteLayout.SetLayoutBounds(mainLayout, new Rectangle(0, 0, 1, 1));

                cleared?.Invoke();
                currentSlideInView = null;
            });
        }

        /// <summary>
        /// Clears a view added with <see cref="SlideOverView(View, Rectangle, Rectangle, string, uint, uint, Easing, Action{double, bool}, Action)"/>
        /// </summary>
        /// <param name="name">the unique name that was used to slide over the view</param>
        /// <param name="rate"></param>
        /// <param name="length"></param>
        /// <param name="easing"></param>
        public void ClearSlideOverView(string name, uint rate = 16, uint length = 250, Easing easing = null)
        {
            if (!currentSlideOverViews.TryRemove(name, out SlideViewData slideViewData))
                throw new Exception($"No SlideViewData with name: {name}");

            this.AbortAnimation(slideViewData.Name);

            slideViewData.ClearAnimation.Commit(this, name, rate, length, easing, finished: (t, b) =>
            {
                mainLayout.Children.Remove(slideViewData.View);

                slideViewData.OnCleared?.Invoke();
            });
        }

        struct SlideViewData
        {
            public string Name;
            public Animation ClearAnimation;
            public View View;
            public Action OnCleared;
        }

        readonly AbsoluteLayout mainLayout;
        readonly ContentView mainContent = new ContentView();

        readonly object slideInLock = new object();
        SlideViewData? currentSlideInView = null;

        readonly ConcurrentDictionary<string, SlideViewData> currentSlideOverViews = new ConcurrentDictionary<string, SlideViewData>();
    }
}