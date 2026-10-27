using Saleling.Model.Product;

namespace Saleling.Controller.Tests
{
    public class SalesControllerCartTests
    {
        private static SalesController CreateController()
        {
            TestData.LoginAsCashier();
            return new SalesController();
        }

        [Fact]
        public void AddItemToCart_NewProduct_AddsLineWithQuantityOne()
        {
            SalesController controller = CreateController();

            controller.AddItemToCart(TestData.Product(price: 150m));

            Assert.Single(controller.CartItems);
            Assert.Equal(1, controller.CartItems[0].Quantity);
            Assert.Equal(150m, controller.CartItems[0].LineTotal);
        }

        [Fact]
        public void AddItemToCart_SameProductTwice_IncrementsQuantity()
        {
            SalesController controller = CreateController();
            POSProductModel product = TestData.Product(price: 50m);

            controller.AddItemToCart(product);
            controller.AddItemToCart(product);

            Assert.Single(controller.CartItems);
            Assert.Equal(2, controller.CartItems[0].Quantity);
            Assert.Equal(100m, controller.CartItems[0].LineTotal);
        }

        [Fact]
        public void AddItemToCart_BeyondStock_Throws()
        {
            SalesController controller = CreateController();
            POSProductModel product = TestData.Product(stock: 1);
            controller.AddItemToCart(product);

            Assert.Throws<InvalidOperationException>(() => controller.AddItemToCart(product));
            Assert.Equal(1, controller.CartItems[0].Quantity);
        }

        [Fact]
        public void AddItemToCart_OutOfStockProduct_Throws()
        {
            SalesController controller = CreateController();

            Assert.Throws<InvalidOperationException>(() => controller.AddItemToCart(TestData.Product(stock: 0)));
            Assert.Empty(controller.CartItems);
        }

        [Fact]
        public void RemoveItemFromCart_ExistingItem_RemovesIt()
        {
            SalesController controller = CreateController();
            controller.AddItemToCart(TestData.Product(variantId: 1));
            controller.AddItemToCart(TestData.Product(variantId: 2));

            controller.RemoveItemFromCart(1);

            Assert.Single(controller.CartItems);
            Assert.Equal(2, controller.CartItems[0].VariantID);
        }

        [Fact]
        public void RemoveItemFromCart_UnknownItem_Throws()
        {
            SalesController controller = CreateController();

            Assert.Throws<InvalidOperationException>(() => controller.RemoveItemFromCart(99));
        }

        [Fact]
        public void ClearCart_EmptiesCartAndResetsPayment()
        {
            SalesController controller = CreateController();
            controller.AddItemToCart(TestData.Product());
            controller.PaymentAmount = 500m;

            controller.ClearCart();

            Assert.Empty(controller.CartItems);
            Assert.Equal(0m, controller.PaymentAmount);
        }

        [Fact]
        public void Totals_SumAcrossLines()
        {
            SalesController controller = CreateController();
            POSProductModel first = TestData.Product(variantId: 1, price: 100m);
            controller.AddItemToCart(first);
            controller.AddItemToCart(first);
            controller.AddItemToCart(TestData.Product(variantId: 2, price: 30m));

            Assert.Equal(3, controller.GetTotalItems());
            Assert.Equal(230m, controller.GetSubTotal());
            Assert.Equal(230m, controller.RecalculateCartTotals());
        }

        [Fact]
        public void ApplyBulkDiscount_ReducesLineTotalAndTotalDiscount()
        {
            SalesController controller = CreateController();
            controller.AddItemToCart(TestData.Product(variantId: 1, price: 100m));
            controller.AddItemToCart(TestData.Product(variantId: 2, price: 80m));

            controller.ApplyBulkDiscountToSelectedItems(new List<int> { 1, 2 }, 10m);

            Assert.Equal(20m, controller.GetTotalDiscount());
            Assert.Equal(160m, controller.RecalculateCartTotals());
        }

        [Fact]
        public void ApplyBulkDiscount_ExceedingLineTotal_CapsAtLineTotalAndThrows()
        {
            SalesController controller = CreateController();
            controller.AddItemToCart(TestData.Product(variantId: 1, price: 40m));

            Assert.Throws<InvalidOperationException>(
                () => controller.ApplyBulkDiscountToSelectedItems(new List<int> { 1 }, 100m));

            Assert.Equal(40m, controller.CartItems[0].Discount);
            Assert.Equal(0m, controller.CartItems[0].LineTotal);
        }

        [Fact]
        public void ApplyBulkDiscount_Negative_Throws()
        {
            SalesController controller = CreateController();
            controller.AddItemToCart(TestData.Product());

            Assert.Throws<ArgumentOutOfRangeException>(
                () => controller.ApplyBulkDiscountToSelectedItems(new List<int> { 1 }, -5m));
        }

        [Fact]
        public void RemoveItemDiscount_ResetsDiscount()
        {
            SalesController controller = CreateController();
            controller.AddItemToCart(TestData.Product(price: 100m));
            controller.ApplyBulkDiscountToSelectedItems(new List<int> { 1 }, 25m);

            controller.RemoveItemDiscount(1);

            Assert.Equal(0m, controller.GetTotalDiscount());
            Assert.Equal(100m, controller.RecalculateCartTotals());
        }

        [Fact]
        public void UpdateBulkItemQuantity_WithinStock_UpdatesQuantity()
        {
            SalesController controller = CreateController();
            controller.AddItemToCart(TestData.Product(price: 20m, stock: 10));

            controller.UpdateBulkItemQuantity(new List<int> { 1 }, 4);

            Assert.Equal(4, controller.CartItems[0].Quantity);
            Assert.Equal(80m, controller.CartItems[0].LineTotal);
        }

        [Fact]
        public void UpdateBulkItemQuantity_ExceedingStock_ThrowsAndKeepsQuantity()
        {
            SalesController controller = CreateController();
            controller.AddItemToCart(TestData.Product(stock: 3));

            Assert.Throws<InvalidOperationException>(
                () => controller.UpdateBulkItemQuantity(new List<int> { 1 }, 4));

            Assert.Equal(1, controller.CartItems[0].Quantity);
        }

        [Fact]
        public void UpdateBulkItemQuantity_BelowOne_Throws()
        {
            SalesController controller = CreateController();
            controller.AddItemToCart(TestData.Product());

            Assert.Throws<ArgumentOutOfRangeException>(
                () => controller.UpdateBulkItemQuantity(new List<int> { 1 }, 0));
        }

        [Theory]
        [InlineData(150, 100, 50)]
        [InlineData(100, 100, 0)]
        [InlineData(40, 100, 0)]
        public void GetChangeDue_ReturnsNonNegativeChange(double payment, double price, double expected)
        {
            SalesController controller = CreateController();
            controller.AddItemToCart(TestData.Product(price: (decimal)price));
            controller.PaymentAmount = (decimal)payment;

            Assert.Equal((decimal)expected, controller.GetChangeDue());
        }

        [Fact]
        public async Task FinalizeSaleAsync_EmptyCart_Throws()
        {
            SalesController controller = CreateController();

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.FinalizeSaleAsync(1));
        }

        [Fact]
        public async Task FinalizeSaleAsync_InsufficientPayment_ThrowsBeforeReachingDatabase()
        {
            SalesController controller = CreateController();
            controller.AddItemToCart(TestData.Product(price: 100m));
            controller.PaymentAmount = 50m;

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.FinalizeSaleAsync(1));
            Assert.Single(controller.CartItems);
        }

        [Theory]
        [InlineData("", "term")]
        [InlineData("term", "")]
        public async Task SearchAvailableProductsAsync_MissingArgument_Throws(string term, string filter)
        {
            SalesController controller = CreateController();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.SearchAvailableProductsAsync(term, filter));
        }
    }
}
