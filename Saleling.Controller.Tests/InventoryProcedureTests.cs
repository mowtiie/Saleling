using Saleling.Model.Inventory;
using Saleling.Repository;
using Microsoft.Data.SqlClient;

namespace Saleling.Controller.Tests
{
    // sp_InventoryLogTransaction, exercised through InventoryRepository against a scratch database.
    public class InventoryProcedureTests : IClassFixture<ScratchDatabase>
    {
        private readonly ScratchDatabase _db;

        public InventoryProcedureTests(ScratchDatabase db)
        {
            _db = db;
        }

        private Task LogAsync(int userId, int variantId, int quantity, string reason) =>
            new InventoryRepository(_db.ConnectionString).LogTransactionAsync(new InventoryLogTransactionModel
            {
                VariantID = variantId,
                QuantityChange = quantity,
                Reason = reason,
                ProcessedByUserID = userId,
                Notes = "test"
            });

        private async Task<(int UserId, int VariantId)> SeedAsync(int stock)
        {
            await _db.ResetAsync();
            return (await _db.SeedUserAsync("Admin", "admin1"), await _db.SeedVariantAsync(stock: stock));
        }

        [ScratchDbFact]
        public async Task IncreasingReason_AddsStock()
        {
            (int userId, int variantId) = await SeedAsync(stock: 5);

            await LogAsync(userId, variantId, 7, "Found Stock");

            Assert.Equal(12, await _db.StockAsync(variantId));
            Assert.Equal(7, await _db.ScalarAsync<int>("SELECT QuantityChange FROM Inventory WHERE VariantID = @V", ("@V", variantId)));
        }

        [ScratchDbFact]
        public async Task DecreasingReason_RemovesStockAndLogsNegativeChange()
        {
            (int userId, int variantId) = await SeedAsync(stock: 5);

            await LogAsync(userId, variantId, 2, "Damaged/Scrap");

            Assert.Equal(3, await _db.StockAsync(variantId));
            Assert.Equal(-2, await _db.ScalarAsync<int>("SELECT QuantityChange FROM Inventory WHERE VariantID = @V", ("@V", variantId)));
        }

        [ScratchDbFact]
        public async Task AdjustmentBelowZero_IsRejectedAndLeavesStockAlone()
        {
            (int userId, int variantId) = await SeedAsync(stock: 2);

            SqlException ex = await Assert.ThrowsAsync<SqlException>(() => LogAsync(userId, variantId, 3, "Shrinkage/Loss"));

            Assert.Contains("negative stock", ex.Message);
            Assert.Equal(2, await _db.StockAsync(variantId));
            Assert.Equal(0, await _db.CountAsync("Inventory"));
        }

        [ScratchDbFact]
        public async Task UnknownReason_IsRejected()
        {
            (int userId, int variantId) = await SeedAsync(stock: 5);

            await Assert.ThrowsAsync<SqlException>(() => LogAsync(userId, variantId, 1, "Because I said so"));

            Assert.Equal(5, await _db.StockAsync(variantId));
        }

        [ScratchDbFact]
        public async Task ZeroQuantity_IsRejected()
        {
            (int userId, int variantId) = await SeedAsync(stock: 5);

            await Assert.ThrowsAsync<SqlException>(() => LogAsync(userId, variantId, 0, "Found Stock"));

            Assert.Equal(0, await _db.CountAsync("Inventory"));
        }

        [ScratchDbFact]
        public async Task UnknownVariant_IsRejected()
        {
            (int userId, _) = await SeedAsync(stock: 5);

            await Assert.ThrowsAsync<SqlException>(() => LogAsync(userId, 999999, 1, "Found Stock"));

            Assert.Equal(0, await _db.CountAsync("Inventory"));
        }
    }
}
