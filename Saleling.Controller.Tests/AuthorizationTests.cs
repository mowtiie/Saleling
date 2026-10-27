using Saleling.Model.Inventory;
using Saleling.Model.Product;
using Saleling.Util;

namespace Saleling.Controller.Tests
{
    // Every case here is rejected before the controller reaches its repository, so no database is needed.
    public class AuthorizationTests
    {
        private static async Task AssertAllDeniedAsync(params (string Name, Func<Task> Action)[] actions)
        {
            foreach ((string name, Func<Task> action) in actions)
            {
                Exception? ex = await Record.ExceptionAsync(action);
                Assert.True(
                    ex is UnauthorizedAccessException,
                    $"{name} should have been denied but {(ex == null ? "succeeded" : "threw " + ex.GetType().Name)}.");
            }
        }

        private static ProductModel NewProduct() =>
            new ProductModel { ProductID = 1, ProductName = "Tee", Category = "Tops", Status = "Active" };

        private static InventoryLogTransactionModel NewLog() => new InventoryLogTransactionModel
        {
            VariantID = 1,
            QuantityChange = 1,
            Reason = "Found Stock",
            ProcessedByUserID = 1,
            Notes = "note"
        };

        [Fact]
        public void RequireLogin_WhenLoggedOut_Throws()
        {
            TestData.Logout();

            Assert.Throws<UnauthorizedAccessException>(() => SessionUtil.Instance.RequireLogin());
        }

        [Fact]
        public void RequireRole_AllowsListedRoleAndRejectsOthers()
        {
            TestData.LoginAsCashier(7);

            Assert.Equal(7, SessionUtil.Instance.RequireRole(Roles.Cashier, Roles.Admin).UserID);
            Assert.Throws<UnauthorizedAccessException>(() => SessionUtil.Instance.RequireRole(Roles.Admin));
        }

        [Fact]
        public async Task LoggedOut_IsDeniedEverywhere()
        {
            TestData.Logout();

            await AssertAllDeniedAsync(
                ("ProductController.AddProductAsync", () => new ProductController().AddProductAsync(NewProduct())),
                ("ProductController.GetProductsAsync", () => new ProductController().GetProductsAsync()),
                ("ProductController.GetProductListingsAsync", () => new ProductController().GetProductListingsAsync()),
                ("InventoryController.GetLowStockCount", () => new InventoryController().GetLowStockCount()),
                ("InventoryController.LogTransactionAsync", () => new InventoryController().LogTransactionAsync(NewLog())),
                ("UserController.GetAllCashierAsync", () => new UserController().GetAllCashierAsync()),
                ("CategoryController.GetAllNamesAsync", () => new CategoryController().GetAllNamesAsync()),
                ("SupplierController.GetAllNamesAsync", () => new SupplierController().GetAllNamesAsync()),
                ("CustomerController.GetWalkInCustomerAsync", () => new CustomerController().GetWalkInCustomerAsync()),
                ("SalesController constructor", () => { _ = new SalesController(); return Task.CompletedTask; }));
        }

        [Fact]
        public async Task Cashier_CannotMaintainProducts()
        {
            TestData.LoginAsCashier();
            ProductController controller = new ProductController();

            await AssertAllDeniedAsync(
                ("AddProductAsync", () => controller.AddProductAsync(NewProduct())),
                ("EditProductAsync", () => controller.EditProductAsync(NewProduct())),
                ("DeleteProductAsync", () => controller.DeleteProductAsync(1)),
                ("AddProductVariantAsync", () => controller.AddProductVariantAsync(TestData.Variant(), 7)),
                ("EditProductVariantAsync", () => controller.EditProductVariantAsync(TestData.Variant())),
                ("DeleteProductVariantAsync", () => controller.DeleteProductVariantAsync(1)));
        }

        [Fact]
        public async Task Cashier_CannotUseInventoryManagement()
        {
            TestData.LoginAsCashier();
            InventoryController controller = new InventoryController();

            await AssertAllDeniedAsync(
                ("LogTransactionAsync", () => controller.LogTransactionAsync(NewLog())),
                ("GetAllAsync", () => controller.GetAllAsync()),
                ("SearchInventoryAsync", () => controller.SearchInventoryAsync("tee", "Product Name")),
                ("GetLogsAsync", () => controller.GetLogsAsync(1)),
                ("GetStockReportAsync", () => controller.GetStockReportAsync()),
                ("GetStockKPIsAsync", () => controller.GetStockKPIsAsync()),
                ("GenerateMovementReportAsync", () => controller.GenerateMovementReportAsync("Daily", null, null)),
                ("GetMovementKPIsAsync", () => controller.GetMovementKPIsAsync("Daily", null, null)));
        }

        [Fact]
        public async Task Cashier_CannotListUsers()
        {
            TestData.LoginAsCashier();

            await AssertAllDeniedAsync(
                ("UserController.GetAllCashierAsync", () => new UserController().GetAllCashierAsync()));
        }

        [Fact]
        public async Task Cashier_CannotSeeStoreWideOrOtherCashiersSales()
        {
            TestData.LoginAsCashier(7);
            SalesController controller = new SalesController();

            await AssertAllDeniedAsync(
                ("GetTotalSalesAsync", () => controller.GetTotalSalesAsync()),
                ("GetTransactionCountAsync", () => controller.GetTransactionCountAsync()),
                ("GetRecentSalesAsync(all)", () => controller.GetRecentSalesAsync()),
                ("GetRecentSalesAsync(other)", () => controller.GetRecentSalesAsync(8)),
                ("GetTotalSalesTodayByUserIDAsync(other)", () => controller.GetTotalSalesTodayByUserIDAsync(8)),
                ("GetAvgItemsPerTransactionTodayByUserIDAsync(other)", () => controller.GetAvgItemsPerTransactionTodayByUserIDAsync(8)),
                ("GetTotalItemsSoldTodayByUserIDAsync(other)", () => controller.GetTotalItemsSoldTodayByUserIDAsync(8)),
                ("GetTransactionCountTodayByUserIDAsync(other)", () => controller.GetTransactionCountTodayByUserIDAsync(8)),
                ("GetSalesReportAsync(all)", () => controller.GetSalesReportAsync("Daily", null, null)),
                ("GetSalesReportAsync(other)", () => controller.GetSalesReportAsync("Daily", null, null, 8)),
                ("GetKPIsAsync(all)", () => controller.GetKPIsAsync("Daily", null, null)),
                ("GetKPIsAsync(other)", () => controller.GetKPIsAsync("Daily", null, null, 8)));
        }

        [Fact]
        public async Task Admin_CannotProcessSales()
        {
            TestData.LoginAsAdmin();
            SalesController controller = new SalesController();
            controller.AddItemToCart(TestData.Product());
            controller.PaymentAmount = 1000m;

            await AssertAllDeniedAsync(("FinalizeSaleAsync", () => controller.FinalizeSaleAsync(1)));
        }

        [Fact]
        public async Task Admin_PassesTheGuardAndReachesValidation()
        {
            TestData.LoginAsAdmin();

            // A plain Exception (not UnauthorizedAccessException) means the role check passed
            // and the controller moved on to validating the input.
            Exception ex = await Assert.ThrowsAsync<Exception>(
                () => new ProductController().AddProductAsync(
                    new ProductModel { ProductName = "", Category = "Tops", Status = "Active" }));

            Assert.Equal("Please enter a name for the product", ex.Message);
        }
    }
}
