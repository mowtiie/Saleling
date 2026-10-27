using Microsoft.Data.SqlClient;

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
        public async Task Customers_ManyCanLackAPhoneAndEmail()
        {
            await _db.ResetAsync();

            await _db.SeedCustomerAsync("Walk-In", "Customer");
            await _db.SeedCustomerAsync("Maria", "Santos");
            await _db.SeedCustomerAsync("Jose", "Reyes");

            Assert.Equal(3, await _db.CountAsync("Customers"));
        }

        [ScratchDbFact]
        public async Task Customers_RejectDuplicatePhone()
        {
            await _db.ResetAsync();
            await _db.SeedCustomerAsync("Maria", "Santos", phone: "09170000001");

            SqlException ex = await Assert.ThrowsAsync<SqlException>(
                () => _db.ExecuteAsync("INSERT INTO Customers (FirstName, LastName, Phone) VALUES ('Jose', 'Reyes', '09170000001')"));

            Assert.Contains("UX_Customers_Phone", ex.Message);
            Assert.Equal(1, await _db.CountAsync("Customers"));
        }

        [ScratchDbFact]
        public async Task Customers_RejectDuplicateEmail()
        {
            await _db.ResetAsync();
            await _db.ExecuteAsync("INSERT INTO Customers (FirstName, LastName, Email) VALUES ('Maria', 'Santos', 'a@example.test')");

            SqlException ex = await Assert.ThrowsAsync<SqlException>(
                () => _db.ExecuteAsync("INSERT INTO Customers (FirstName, LastName, Email) VALUES ('Jose', 'Reyes', 'a@example.test')"));

            Assert.Contains("UX_Customers_Email", ex.Message);
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
