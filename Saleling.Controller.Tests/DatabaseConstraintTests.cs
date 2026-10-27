using System.Data.SqlClient;

namespace Saleling.Controller.Tests
{
    // Runs against a scratch database built from the Saleling.DB scripts.
    public class DatabaseConstraintTests : IClassFixture<ScratchDatabase>
    {
        private readonly ScratchDatabase _db;

        public DatabaseConstraintTests(ScratchDatabase db)
        {
            _db = db;
        }

        [ScratchDbFact]
        public async Task Users_AcceptKnownRoles()
        {
            await _db.ResetAsync();

            await _db.SeedUserAsync("Admin", "admin1");
            await _db.SeedUserAsync("Cashier", "cashier1");

            Assert.Equal(2, await _db.CountAsync("Users"));
        }

        [ScratchDbFact]
        public async Task Users_RejectUnknownRole()
        {
            await _db.ResetAsync();

            SqlException ex = await Assert.ThrowsAsync<SqlException>(() => _db.SeedUserAsync("Manager", "manager1"));

            Assert.Contains("CK_Users_Role", ex.Message);
            Assert.Equal(0, await _db.CountAsync("Users"));
        }

        [ScratchDbFact]
        public async Task ProductVariants_AcceptZeroStock()
        {
            await _db.ResetAsync();

            int variantId = await _db.SeedVariantAsync(stock: 0);

            Assert.Equal(0, await _db.StockAsync(variantId));
        }

        [ScratchDbFact]
        public async Task ProductVariants_RejectNegativeStockOnInsert()
        {
            await _db.ResetAsync();

            SqlException ex = await Assert.ThrowsAsync<SqlException>(() => _db.SeedVariantAsync(stock: -1));

            Assert.Contains("CK_ProductVariants_StockQuantity", ex.Message);
        }

        [ScratchDbFact]
        public async Task ProductVariants_RejectNegativeStockOnUpdate()
        {
            await _db.ResetAsync();
            int variantId = await _db.SeedVariantAsync(stock: 3);

            SqlException ex = await Assert.ThrowsAsync<SqlException>(
                () => _db.ExecuteAsync("UPDATE ProductVariants SET StockQuantity = StockQuantity - 4 WHERE VariantID = @Id", ("@Id", variantId)));

            Assert.Contains("CK_ProductVariants_StockQuantity", ex.Message);
            Assert.Equal(3, await _db.StockAsync(variantId));
        }
    }
}
