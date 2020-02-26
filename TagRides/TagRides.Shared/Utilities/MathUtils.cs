using System;
namespace TagRides.Shared.Utilities
{
    public static class MathUtils
    {
        /// <summary>
        /// Computes the non-negative remainder of dividing a by b. The C# %
        /// operator can return negative numbers. Math.IEEERemainder can also
        /// return negative numbers.
        /// </summary>
        /// <returns>The remainder of a / b.</returns>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor (should be positive).</param>
        public static double Mod(double a, double b)
        {
            return a - b * Math.Floor(a / b);
        }

        /// <summary>
        /// Gets the element of {a + (rangeMax - rangeMin) * n | n integer} that
        /// lies in the range [rangeMin, rangeMax).
        /// </summary>
        /// <returns>The range.</returns>
        /// <param name="a">The alpha component.</param>
        /// <param name="rangeMin">Range minimum.</param>
        /// <param name="rangeMax">Range max.</param>
        public static double ModRange(double a, double rangeMin, double rangeMax)
        {
            return Mod(a - rangeMin, rangeMax - rangeMin) + rangeMin;
        }
    }
}
