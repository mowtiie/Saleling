using Saleling.Model;
using Saleling.Model.Product;
using Saleling.Repository;

namespace Saleling.Controller.Tests
{
    // Controllers wired to repositories that point at a scratch database.
    public class UserControllerIntegrationTests : IClassFixture<ScratchDatabase>
    {
        private readonly ScratchDatabase _db;

        public UserControllerIntegrationTests(ScratchDatabase db)
        {
            _db = db;
        }

        private UserController Controller() => new UserController(new UserRepository(_db.ConnectionString));

        [ScratchDbFact]
        public async Task Authenticate_ValidCredentials_ReturnsTheUser()
        {
            await _db.ResetAsync();
            int userId = await _db.SeedUserAsync("Admin", "boss", "s3cret-pass");

            UserModel user = await Controller().AuthenticateAsync("boss", "s3cret-pass");

            Assert.Equal(userId, user.UserID);
            Assert.Equal("Admin", user.Role);
        }

        [ScratchDbFact]
        public async Task Authenticate_WrongPassword_IsRejected()
        {
            await _db.ResetAsync();
            await _db.SeedUserAsync("Admin", "boss", "s3cret-pass");

            Exception ex = await Assert.ThrowsAsync<Exception>(() => Controller().AuthenticateAsync("boss", "wrong"));

            Assert.Equal("Invalid credentials", ex.Message);
        }

        [ScratchDbFact]
        public async Task Authenticate_UnknownUser_IsRejected()
        {
            await _db.ResetAsync();

            Exception ex = await Assert.ThrowsAsync<Exception>(() => Controller().AuthenticateAsync("nobody", "whatever"));

            Assert.Equal("The user does not exist", ex.Message);
        }

        [ScratchDbFact]
        public async Task GetAllCashier_AsAdmin_ListsOnlyCashiers()
        {
            await _db.ResetAsync();
            await _db.SeedUserAsync("Admin", "boss");
            await _db.SeedUserAsync("Cashier", "till1");
            await _db.SeedUserAsync("Cashier", "till2");
            TestData.LoginAsAdmin();

            List<UserModel> cashiers = await Controller().GetAllCashierAsync();

            Assert.Equal(2, cashiers.Count);
            Assert.All(cashiers, c => Assert.Equal("Cashier", c.Role));
        }
    }

    public class SalesControllerIntegrationTests : IClassFixture<ScratchDatabase>
    {
        private readonly ScratchDatabase _db;

        public SalesControllerIntegrationTests(ScratchDatabase db)
        {
            _db = db;
        }

        private SalesController Controller() =>
            new SalesController(new SalesRepository(_db.ConnectionString), new ProductRepository(_db.ConnectionString));

        [ScratchDbFact]
        public async Task Cashier_CompletesASaleEndToEnd()
        {
            await _db.ResetAsync();
            int cashierId = await _db.SeedUserAsync("Cashier", "till1");
            int customerId = await _db.SeedCustomerAsync();
            int variantId = await _db.SeedVariantAsync(sellingPrice: 80m, stock: 10);
            TestData.LoginAsCashier(cashierId);

            SalesController controller = Controller();
            List<POSProductModel> products = await controller.GetAvailableProductsAsync();
            POSProductModel product = Assert.Single(products);
            controller.AddItemToCart(product);
            controller.AddItemToCart(product);
            controller.PaymentAmount = 200m;

            int saleId = await controller.FinalizeSaleAsync(customerId);

            Assert.Equal(8, await _db.StockAsync(variantId));
            Assert.Equal(160m, await _db.ScalarAsync<decimal>("SELECT TotalAmount FROM Sales WHERE SaleID = @Id", ("@Id", saleId)));
            Assert.Equal(cashierId, await _db.ScalarAsync<int>("SELECT ProcessedByUserID FROM Sales WHERE SaleID = @Id", ("@Id", saleId)));
            Assert.Empty(controller.CartItems);
        }

        [ScratchDbFact]
        public async Task Cashier_PriceChangeAfterAddingToCart_RejectsSaleAndKeepsCart()
        {
            await _db.ResetAsync();
            int cashierId = await _db.SeedUserAsync("Cashier", "till1");
            int customerId = await _db.SeedCustomerAsync();
            int variantId = await _db.SeedVariantAsync(sellingPrice: 80m, stock: 10);
            TestData.LoginAsCashier(cashierId);
            SalesController controller = Controller();
            controller.AddItemToCart((await controller.GetAvailableProductsAsync()).Single());
            controller.PaymentAmount = 500m;

            // An admin raises the price while the item is already in the cart.
            await _db.ExecuteAsync("UPDATE ProductVariants SET SellingPrice = 90 WHERE VariantID = @Id", ("@Id", variantId));

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.FinalizeSaleAsync(customerId));

            Assert.Contains("no longer matches current prices", ex.Message);
            Assert.Equal(10, await _db.StockAsync(variantId));
            Assert.Equal(0, await _db.CountAsync("Sales"));
            Assert.Single(controller.CartItems);
        }

        [ScratchDbFact]
        public async Task Cashier_SeesOwnSalesButNotAnotherCashiers()
        {
            await _db.ResetAsync();
            int me = await _db.SeedUserAsync("Cashier", "till1");
            int other = await _db.SeedUserAsync("Cashier", "till2");
            int customerId = await _db.SeedCustomerAsync();
            await _db.SeedVariantAsync(sellingPrice: 50m, stock: 10);

            foreach (int cashierId in new[] { me, other })
            {
                TestData.LoginAsCashier(cashierId);
                SalesController seller = Controller();
                seller.AddItemToCart((await seller.GetAvailableProductsAsync()).Single());
                seller.PaymentAmount = 50m;
                await seller.FinalizeSaleAsync(customerId);
            }

            TestData.LoginAsCashier(me);
            SalesController controller = Controller();

            Assert.Single(await controller.GetRecentSalesAsync(me));
            Assert.Equal(50m, await controller.GetTotalSalesTodayByUserIDAsync(me));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetRecentSalesAsync(other));
        }

        [ScratchDbFact]
        public async Task Admin_SeesStoreWideTotals()
        {
            await _db.ResetAsync();
            int cashierId = await _db.SeedUserAsync("Cashier", "till1");
            int customerId = await _db.SeedCustomerAsync();
            await _db.SeedVariantAsync(sellingPrice: 50m, stock: 10);
            TestData.LoginAsCashier(cashierId);
            SalesController seller = Controller();
            seller.AddItemToCart((await seller.GetAvailableProductsAsync()).Single());
            seller.PaymentAmount = 50m;
            await seller.FinalizeSaleAsync(customerId);

            TestData.LoginAsAdmin();
            SalesController admin = Controller();

            Assert.Equal(50m, await admin.GetTotalSalesAsync());
            Assert.Equal(1, await admin.GetTransactionCountAsync());
            Assert.Single(await admin.GetRecentSalesAsync());
        }
    }
}
