using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TagRides.ViewUtilities
{
    public static class AnimationUtilites
    {
        /// <summary>
        /// Animate a press, lowering opacity and scale briefly then returning to normal
        /// </summary>
        /// <param name="view">View to animate</param>
        /// <param name="opacity">opacity to go to before returning to 1</param>
        /// <param name="scale">scale to go to before returning to 1</param>
        /// <param name="length">how long the full animation will take</param>
        /// <param name="easing">easing function to use on all sub animations</param>
        /// <returns></returns>
        public static async Task Press(View view, double opacity = 0.5, double scale = 0.95, uint length = 200, Easing easing = null)
        {
            Task fade = view.FadeTo(opacity, length/2, easing);
            await view.ScaleTo(scale, length / 2, easing);
            await fade;
            fade = view.FadeTo(1, length / 2, easing);
            await view.ScaleTo(1, length / 2, easing);
            await fade;
        }
    }
}
