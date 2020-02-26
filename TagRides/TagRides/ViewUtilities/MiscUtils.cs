using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace TagRides.ViewUtilities
{
    public static class MiscUtils
    {
        /// <summary>
        /// Get the closest Page ancestor of <paramref name="element"/>
        /// null if there is no page ancestor
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static Page GetPageParent(this Element element)
        {
            Element p = element;
            while (!(p is Page))
            {
                if (p.Parent == null)
                    return null;

                p = p.Parent;
            }

            return p as Page;
        }
    }
}
