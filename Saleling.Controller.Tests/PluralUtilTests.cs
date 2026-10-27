using Saleling.Util;

namespace Saleling.Controller.Tests
{
    public class PluralUtilTests
    {
        [Theory]
        [InlineData(0, "0 transactions")]
        [InlineData(1, "1 transaction")]
        [InlineData(2, "2 transactions")]
        public void Count_UsesSingularOnlyForOne(long count, string expected)
        {
            Assert.Equal(expected, PluralUtil.Count(count, "transaction", "transactions"));
        }

        [Fact]
        public void Count_SupportsIrregularPlurals()
        {
            Assert.Equal("1 category", PluralUtil.Count(1, "category", "categories"));
            Assert.Equal("3 categories", PluralUtil.Count(3, "category", "categories"));
        }
    }
}
