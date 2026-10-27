using Saleling.Model.Sale;
using Saleling.Repository;
using System.Data.SqlClient;

namespace Saleling.Controller.Tests
{
    // sp_ProcessSaleTransaction, exercised through SalesRepository against a scratch database.
    public class SaleProcedureTests : IClassFixture<ScratchDatabase>
    {
        private readonly ScratchDatabase _db;

        public SaleProcedureTests(ScratchDatabase db)
        {
            _db = db;
        }

        private async Task<(int UserId, int CustomerId)> SeedAsync()
        {
            await _db.ResetAsync();
            return (await _db.SeedUserAsync(), await _db.SeedCustomerAsync());
        }

        private static SalesTransactionModel Sale(int userId, int customerId, decimal expectedTotal, params SalesItemModel[] items) =>
            new SalesTransactionModel
            {
                CustomerID = customerId,
                ProcessedByUserID = userId,
                TotalAmount = expectedTotal,
                Items = items.ToList()
            };

        private static SalesItemModel Item(int variantId, int quantity, decimal discount = 0m) =>
            new SalesItemModel { VariantID = variantId, Quantity = quantity, UnitPrice = 0.01m, Discount = discount, SubTotal = 0.01m };

        private Task<int> ProcessAsync(SalesTransactionModel sale) =>
            new SalesRepository(_db.ConnectionString).ProcessSaleAsync(sale);

        [ScratchDbFact]
        public async Task Sale_IgnoresClientUnitPriceAndSubTotal()
        {
            (int userId, int customerId) = await SeedAsync();
            int variantId = await _db.SeedVariantAsync(sellingPrice: 100m, stock: 10);

            int saleId = await ProcessAsync(Sale(userId, customerId, 280m, Item(variantId, 3, discount: 20m)));

            Assert.Equal(280m, await _db.ScalarAsync<decimal>("SELECT TotalAmount FROM Sales WHERE SaleID = @Id", ("@Id", saleId)));
            Assert.Equal(100m, await _db.ScalarAsync<decimal>("SELECT UnitPrice FROM SalesItems WHERE SaleID = @Id", ("@Id", saleId)));
            Assert.Equal(20m, await _db.ScalarAsync<decimal>("SELECT Discount FROM SalesItems WHERE SaleID = @Id", ("@Id", saleId)));
            Assert.Equal(280m, await _db.ScalarAsync<decimal>("SELECT SubTotal FROM SalesItems WHERE SaleID = @Id", ("@Id", saleId)));
        }

        [ScratchDbFact]
        public async Task Sale_TotalThatDoesNotMatchCurrentPrices_IsRejectedAndChangesNothing()
        {
            (int userId, int customerId) = await SeedAsync();
            int variantId = await _db.SeedVariantAsync(sellingPrice: 100m, stock: 10);

            // The cashier was shown 90.00, but the variant now costs 100.00 (or the client lied).
            SqlException ex = await Assert.ThrowsAsync<SqlException>(
                () => ProcessAsync(Sale(userId, customerId, 90m, Item(variantId, 1))));

            Assert.Contains("no longer matches current prices", ex.Message);
            Assert.Equal(10, await _db.StockAsync(variantId));
            Assert.Equal(0, await _db.CountAsync("Sales"));
            Assert.Equal(0, await _db.CountAsync("Inventory"));
        }

        [ScratchDbFact]
        public async Task Sale_DecrementsStockAndLogsInventoryMovement()
        {
            (int userId, int customerId) = await SeedAsync();
            int variantId = await _db.SeedVariantAsync(stock: 10);

            int saleId = await ProcessAsync(Sale(userId, customerId, 300m, Item(variantId, 3)));

            Assert.Equal(7, await _db.StockAsync(variantId));
            Assert.Equal(-3, await _db.ScalarAsync<int>(
                "SELECT QuantityChange FROM Inventory WHERE VariantID = @V AND Reason = 'Sale' AND TransactionReferenceID = @S",
                ("@V", variantId), ("@S", saleId)));
        }

        [ScratchDbFact]
        public async Task Sale_SellsTheLastUnit()
        {
            (int userId, int customerId) = await SeedAsync();
            int variantId = await _db.SeedVariantAsync(stock: 2);

            await ProcessAsync(Sale(userId, customerId, 200m, Item(variantId, 2)));

            Assert.Equal(0, await _db.StockAsync(variantId));
        }

        [ScratchDbFact]
        public async Task Sale_MultipleItems_SumsTheTotal()
        {
            (int userId, int customerId) = await SeedAsync();
            int shirt = await _db.SeedVariantAsync(sellingPrice: 100m, stock: 5);
            int socks = await _db.SeedVariantAsync(sellingPrice: 30m, stock: 5);

            int saleId = await ProcessAsync(Sale(userId, customerId, 230m, Item(shirt, 2), Item(socks, 1)));

            Assert.Equal(230m, await _db.ScalarAsync<decimal>("SELECT TotalAmount FROM Sales WHERE SaleID = @Id", ("@Id", saleId)));
            Assert.Equal(2, await _db.CountAsync("SalesItems"));
        }

        [ScratchDbFact]
        public async Task Sale_DiscountLargerThanLine_IsCappedAtLineTotal()
        {
            (int userId, int customerId) = await SeedAsync();
            int variantId = await _db.SeedVariantAsync(sellingPrice: 50m, stock: 5);

            int saleId = await ProcessAsync(Sale(userId, customerId, 0m, Item(variantId, 2, discount: 500m)));

            Assert.Equal(100m, await _db.ScalarAsync<decimal>("SELECT Discount FROM SalesItems WHERE SaleID = @Id", ("@Id", saleId)));
            Assert.Equal(0m, await _db.ScalarAsync<decimal>("SELECT TotalAmount FROM Sales WHERE SaleID = @Id", ("@Id", saleId)));
        }

        [ScratchDbFact]
        public async Task Sale_InsufficientStock_FailsAndChangesNothing()
        {
            (int userId, int customerId) = await SeedAsync();
            int variantId = await _db.SeedVariantAsync(stock: 2);

            SqlException ex = await Assert.ThrowsAsync<SqlException>(
                () => ProcessAsync(Sale(userId, customerId, 300m, Item(variantId, 3))));

            Assert.Contains("Insufficient stock", ex.Message);
            Assert.Equal(2, await _db.StockAsync(variantId));
            Assert.Equal(0, await _db.CountAsync("Sales"));
            Assert.Equal(0, await _db.CountAsync("SalesItems"));
            Assert.Equal(0, await _db.CountAsync("Inventory"));
        }

        [ScratchDbFact]
        public async Task Sale_OneBadLine_RollsBackTheGoodLinesToo()
        {
            (int userId, int customerId) = await SeedAsync();
            int plentiful = await _db.SeedVariantAsync(stock: 10);
            int scarce = await _db.SeedVariantAsync(stock: 1);

            await Assert.ThrowsAsync<SqlException>(
                () => ProcessAsync(Sale(userId, customerId, 600m, Item(plentiful, 4), Item(scarce, 2))));

            Assert.Equal(10, await _db.StockAsync(plentiful));
            Assert.Equal(1, await _db.StockAsync(scarce));
            Assert.Equal(0, await _db.CountAsync("Sales"));
        }

        [ScratchDbFact]
        public async Task Sale_DiscontinuedVariant_IsRejected()
        {
            (int userId, int customerId) = await SeedAsync();
            int variantId = await _db.SeedVariantAsync(stock: 10, status: "Discontinued");

            await Assert.ThrowsAsync<SqlException>(() => ProcessAsync(Sale(userId, customerId, 100m, Item(variantId, 1))));

            Assert.Equal(10, await _db.StockAsync(variantId));
            Assert.Equal(0, await _db.CountAsync("Sales"));
        }

        [ScratchDbFact]
        public async Task Sale_UnknownVariant_IsRejected()
        {
            (int userId, int customerId) = await SeedAsync();

            await Assert.ThrowsAsync<SqlException>(() => ProcessAsync(Sale(userId, customerId, 100m, Item(999999, 1))));

            Assert.Equal(0, await _db.CountAsync("Sales"));
        }

        [ScratchDbFact]
        public async Task Sale_NoItems_IsRejected()
        {
            (int userId, int customerId) = await SeedAsync();

            await Assert.ThrowsAsync<SqlException>(() => ProcessAsync(Sale(userId, customerId, 0m)));

            Assert.Equal(0, await _db.CountAsync("Sales"));
        }

        [ScratchDbFact]
        public async Task Sale_NonPositiveQuantityOrNegativeDiscount_IsRejected()
        {
            (int userId, int customerId) = await SeedAsync();
            int variantId = await _db.SeedVariantAsync(stock: 10);

            await Assert.ThrowsAsync<SqlException>(() => ProcessAsync(Sale(userId, customerId, 0m, Item(variantId, 0))));
            await Assert.ThrowsAsync<SqlException>(() => ProcessAsync(Sale(userId, customerId, 0m, Item(variantId, -2))));
            await Assert.ThrowsAsync<SqlException>(() => ProcessAsync(Sale(userId, customerId, 0m, Item(variantId, 1, discount: -5m))));

            Assert.Equal(10, await _db.StockAsync(variantId));
            Assert.Equal(0, await _db.CountAsync("Sales"));
        }

        [ScratchDbFact]
        public async Task Sale_DuplicateVariantLines_AreRejected()
        {
            (int userId, int customerId) = await SeedAsync();
            int variantId = await _db.SeedVariantAsync(stock: 10);

            await Assert.ThrowsAsync<SqlException>(
                () => ProcessAsync(Sale(userId, customerId, 200m, Item(variantId, 1), Item(variantId, 1))));

            Assert.Equal(10, await _db.StockAsync(variantId));
        }
    }
}
