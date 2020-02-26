using System;
using Xunit;
using TagRides.Shared.Utilities;

namespace TagRides.Shared.Tests
{
    public class MathUtilsTests
    {
        [Fact]
        public void ModTestInRange()
        {
            Assert.InRange(MathUtils.Mod(1.0, 3.0), 1 - eps, 1 + eps);
        }

        [Fact]
        public void ModTestEqual()
        {
            Assert.InRange(MathUtils.Mod(3.0, 3.0), -eps, eps);
        }

        [Fact]
        public void ModTestZero()
        {
            Assert.InRange(MathUtils.Mod(0.0, 3.0), -eps, eps);
        }

        [Fact]
        public void ModTestGreater()
        {
            Assert.InRange(MathUtils.Mod(3.5, 3.0), 0.5 - eps, 0.5 + eps);
        }

        [Fact]
        public void ModTestNegative()
        {
            Assert.InRange(MathUtils.Mod(-0.5, 3.0), 2.5 - eps, 2.5 + eps);
        }

        [Fact]
        public void RangeTestInRange()
        {
            Assert.InRange(MathUtils.ModRange(0.0, -3.0, 5.0), -eps, eps);
        }

        [Fact]
        public void RangeTestBelowRange()
        {
            Assert.InRange(MathUtils.ModRange(-3.5, -3.0, 5.0), 4.5 - eps, 4.5 + eps);
        }

        [Fact]
        public void RangeTestAboveRange()
        {
            Assert.InRange(MathUtils.ModRange(6.5, -3.0, 5.0), -1.5 - eps, -1.5 + eps);
        }

        [Fact]
        public void RangeTestFarAboveRange()
        {
            Assert.InRange(MathUtils.ModRange(-1.0 + 8 * 8, -3.0, 5.0), -1.0 - eps, -1.0 + eps);
        }

        const double eps = 1e-10;
    }
}
