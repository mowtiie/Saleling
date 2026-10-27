using Saleling.Model.Inventory;
using Saleling.Model.Product;

namespace Saleling.Controller.Tests
{
    // These cases all fail validation before the controller touches its repository,
    // so they run without a database.
    public class UserControllerValidationTests
    {
        [Theory]
        [InlineData("", "password")]
        [InlineData("user", "")]
        [InlineData(null, null)]
        public async Task AuthenticateAsync_MissingCredentials_Throws(string? username, string? password)
        {
            UserController controller = new UserController();

            Exception ex = await Assert.ThrowsAsync<Exception>(
                () => controller.AuthenticateAsync(username!, password!));

            Assert.Equal("Please enter both a username and password", ex.Message);
        }
    }

    public class ProductControllerValidationTests
    {
        private readonly ProductController _controller = new ProductController();

        public ProductControllerValidationTests()
        {
            TestData.LoginAsAdmin();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetProductByIDAsync_InvalidID_Throws(int id)
        {
            await Assert.ThrowsAsync<Exception>(() => _controller.GetProductByIDAsync(id));
        }

        [Theory]
        [InlineData("", "Name")]
        [InlineData("shirt", "")]
        public async Task SearchProductsAsync_MissingArgument_Throws(string term, string filter)
        {
            await Assert.ThrowsAsync<Exception>(() => _controller.SearchProductsAsync(term, filter));
        }

        [Fact]
        public async Task AddProductAsync_BlankName_Throws()
        {
            ProductModel product = new ProductModel { ProductName = "", Category = "Denim", Status = "Active" };

            await Assert.ThrowsAsync<Exception>(() => _controller.AddProductAsync(product));
        }

        [Fact]
        public async Task AddProductVariantAsync_InvalidParentProduct_Throws()
        {
            ProductVariantModel variant = TestData.Variant();
            variant.ProductID = 0;

            await Assert.ThrowsAsync<Exception>(() => _controller.AddProductVariantAsync(variant, 1));
        }

        [Fact]
        public async Task AddProductVariantAsync_BlankSku_Throws()
        {
            ProductVariantModel variant = TestData.Variant();
            variant.SKU = "";

            await Assert.ThrowsAsync<Exception>(() => _controller.AddProductVariantAsync(variant, 1));
        }

        [Fact]
        public async Task AddProductVariantAsync_NegativeReorderLevel_Throws()
        {
            ProductVariantModel variant = TestData.Variant();
            variant.ReorderLevel = -1;

            await Assert.ThrowsAsync<Exception>(() => _controller.AddProductVariantAsync(variant, 1));
        }

        [Fact]
        public async Task AddProductVariantAsync_NegativeStock_Throws()
        {
            ProductVariantModel variant = TestData.Variant();
            variant.StockQuantity = -1;

            await Assert.ThrowsAsync<Exception>(() => _controller.AddProductVariantAsync(variant, 1));
        }

        [Theory]
        [InlineData(-1, 10)]
        [InlineData(10, -1)]
        public async Task AddProductVariantAsync_NegativePrice_Throws(double cost, double selling)
        {
            ProductVariantModel variant = TestData.Variant();
            variant.CostPrice = (decimal)cost;
            variant.SellingPrice = (decimal)selling;

            await Assert.ThrowsAsync<Exception>(() => _controller.AddProductVariantAsync(variant, 1));
        }

        [Fact]
        public async Task EditProductVariantAsync_InvalidVariantID_Throws()
        {
            ProductVariantModel variant = TestData.Variant();
            variant.VariantID = 0;

            await Assert.ThrowsAsync<Exception>(() => _controller.EditProductVariantAsync(variant));
        }

        [Fact]
        public async Task EditProductVariantAsync_NegativeReorderLevel_Throws()
        {
            ProductVariantModel variant = TestData.Variant();
            variant.ReorderLevel = -1;

            Exception ex = await Assert.ThrowsAsync<Exception>(() => _controller.EditProductVariantAsync(variant));

            Assert.Equal("Reorder level must not be less than zero", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task DeleteProductAsync_InvalidID_Throws(int id)
        {
            await Assert.ThrowsAsync<Exception>(() => _controller.DeleteProductAsync(id));
        }
    }

    public class InventoryControllerValidationTests
    {
        private readonly InventoryController _controller = new InventoryController();

        public InventoryControllerValidationTests()
        {
            TestData.LoginAsAdmin();
        }

        private static InventoryLogTransactionModel ValidLog() => new InventoryLogTransactionModel
        {
            VariantID = 1,
            QuantityChange = 5,
            Reason = "Found Stock",
            ProcessedByUserID = 1,
            Notes = "Counted extra boxes"
        };

        [Fact]
        public async Task LogTransactionAsync_InvalidVariant_Throws()
        {
            InventoryLogTransactionModel log = ValidLog();
            log.VariantID = 0;

            await Assert.ThrowsAsync<Exception>(() => _controller.LogTransactionAsync(log));
        }

        [Fact]
        public async Task LogTransactionAsync_MissingReason_Throws()
        {
            InventoryLogTransactionModel log = ValidLog();
            log.Reason = "";

            await Assert.ThrowsAsync<Exception>(() => _controller.LogTransactionAsync(log));
        }

        [Fact]
        public async Task LogTransactionAsync_MissingNotes_Throws()
        {
            InventoryLogTransactionModel log = ValidLog();
            log.Notes = null;

            await Assert.ThrowsAsync<Exception>(() => _controller.LogTransactionAsync(log));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-4)]
        public async Task LogTransactionAsync_NonPositiveQuantity_Throws(int quantity)
        {
            InventoryLogTransactionModel log = ValidLog();
            log.QuantityChange = quantity;

            await Assert.ThrowsAsync<Exception>(() => _controller.LogTransactionAsync(log));
        }

        [Theory]
        [InlineData("", "Product Name")]
        [InlineData("tee", "")]
        public async Task SearchInventoryAsync_MissingArgument_Throws(string term, string filter)
        {
            await Assert.ThrowsAsync<Exception>(() => _controller.SearchInventoryAsync(term, filter));
        }

        [Fact]
        public async Task GetLogsAsync_InvalidID_Throws()
        {
            await Assert.ThrowsAsync<Exception>(() => _controller.GetLogsAsync(0));
        }
    }
}
