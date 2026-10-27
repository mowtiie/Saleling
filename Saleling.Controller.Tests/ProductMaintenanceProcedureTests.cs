using Microsoft.Data.SqlClient;
using Saleling.Model.Product;
using Saleling.Repository;

namespace Saleling.Controller.Tests
{
    // sp_AddProduct, sp_EditProduct, sp_DeleteProduct and the matching variant procedures,
    // exercised through ProductRepository against a scratch database.
    public class ProductMaintenanceProcedureTests : IClassFixture<ScratchDatabase>
    {
        private readonly ScratchDatabase _db;

        public ProductMaintenanceProcedureTests(ScratchDatabase db)
        {
            _db = db;
        }

        private ProductRepository Repository() => new ProductRepository(_db.ConnectionString);

        private static ProductModel Product(string name = "Classic Tee", string category = "Tops", string? supplier = "Acme") =>
            new ProductModel { ProductName = name, Category = category, Supplier = supplier, Status = "Active" };

        private static ProductVariantModel Variant(int productId, string sku = "TEE-1", decimal cost = 10m, decimal price = 20m, int stock = 8) =>
            new ProductVariantModel
            {
                ProductID = productId,
                SKU = sku,
                Size = "M",
                Color = "Black",
                CostPrice = cost,
                SellingPrice = price,
                StockQuantity = stock,
                ReorderLevel = 5,
                Status = "Active"
            };

        private async Task<(int UserId, int CategoryId, int SupplierId)> SeedLookupsAsync()
        {
            await _db.ResetAsync();
            return (await _db.SeedUserAsync("Admin", "admin1"), await _db.AddCategoryAsync("Tops"), await _db.AddSupplierAsync("Acme"));
        }

        // ---- products ----

        [ScratchDbFact]
        public async Task AddProduct_StoresCategoryAndSupplier()
        {
            (_, int categoryId, int supplierId) = await SeedLookupsAsync();

            await Repository().AddProductAsync(Product());

            Assert.Equal(categoryId, await _db.ScalarAsync<int>("SELECT CategoryID FROM Products WHERE ProductName = 'Classic Tee'"));
            Assert.Equal(supplierId, await _db.ScalarAsync<int>("SELECT SupplierID FROM Products WHERE ProductName = 'Classic Tee'"));
        }

        [ScratchDbFact]
        public async Task AddProduct_NoAssignedSupplier_StoresNull()
        {
            await SeedLookupsAsync();

            await Repository().AddProductAsync(Product(supplier: "No Assigned Supplier"));

            Assert.Equal(1, await _db.ScalarAsync<int>("SELECT COUNT(*) FROM Products WHERE SupplierID IS NULL"));
        }

        [ScratchDbFact]
        public async Task AddProduct_UnknownCategory_IsRejected()
        {
            await SeedLookupsAsync();

            SqlException ex = await Assert.ThrowsAsync<SqlException>(() => Repository().AddProductAsync(Product(category: "Nope")));

            Assert.Contains("Invalid Category", ex.Message);
            Assert.Equal(0, await _db.CountAsync("Products"));
        }

        [ScratchDbFact]
        public async Task AddProduct_UnknownSupplier_IsRejected()
        {
            await SeedLookupsAsync();

            SqlException ex = await Assert.ThrowsAsync<SqlException>(() => Repository().AddProductAsync(Product(supplier: "Nobody Ltd")));

            Assert.Contains("Invalid Supplier", ex.Message);
            Assert.Equal(0, await _db.CountAsync("Products"));
        }

        [ScratchDbFact]
        public async Task AddProduct_DuplicateName_IsRejected()
        {
            await SeedLookupsAsync();
            await Repository().AddProductAsync(Product());

            SqlException ex = await Assert.ThrowsAsync<SqlException>(() => Repository().AddProductAsync(Product()));

            Assert.Contains("already exists", ex.Message);
            Assert.Equal(1, await _db.CountAsync("Products"));
        }

        [ScratchDbFact]
        public async Task EditProduct_ChangesNameCategorySupplierAndStatus()
        {
            (_, int topsId, int supplierId) = await SeedLookupsAsync();
            int denimId = await _db.AddCategoryAsync("Denim");
            int productId = await _db.AddProductAsync("Classic Tee", topsId, supplierId);

            await Repository().EditProductAsync(new ProductModel
            {
                ProductID = productId,
                ProductName = "Slim Jeans",
                Category = "Denim",
                Supplier = "No Assigned Supplier",
                Status = "Archived"
            });

            Assert.Equal("Slim Jeans", await ScalarStringAsync("SELECT ProductName FROM Products WHERE ProductID = " + productId));
            Assert.Equal(denimId, await _db.ScalarAsync<int>("SELECT CategoryID FROM Products WHERE ProductID = @Id", ("@Id", productId)));
            Assert.Equal("Archived", await ScalarStringAsync("SELECT Status FROM Products WHERE ProductID = " + productId));
            Assert.Equal(1, await _db.ScalarAsync<int>("SELECT COUNT(*) FROM Products WHERE ProductID = @Id AND SupplierID IS NULL", ("@Id", productId)));
        }

        [ScratchDbFact]
        public async Task EditProduct_NameTakenByAnotherProduct_IsRejected()
        {
            (_, int categoryId, _) = await SeedLookupsAsync();
            await _db.AddProductAsync("Classic Tee", categoryId);
            int otherId = await _db.AddProductAsync("Slim Jeans", categoryId);

            SqlException ex = await Assert.ThrowsAsync<SqlException>(() => Repository().EditProductAsync(
                new ProductModel { ProductID = otherId, ProductName = "Classic Tee", Category = "Tops", Status = "Active" }));

            Assert.Contains("already exists", ex.Message);
        }

        [ScratchDbFact]
        public async Task EditProduct_KeepingItsOwnName_IsAllowed()
        {
            (_, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);

            await Repository().EditProductAsync(
                new ProductModel { ProductID = productId, ProductName = "Classic Tee", Category = "Tops", Status = "Archived" });

            Assert.Equal("Archived", await ScalarStringAsync("SELECT Status FROM Products WHERE ProductID = " + productId));
        }

        [ScratchDbFact]
        public async Task EditProduct_UnknownProduct_IsRejected()
        {
            await SeedLookupsAsync();

            await Assert.ThrowsAsync<SqlException>(() => Repository().EditProductAsync(
                new ProductModel { ProductID = 999999, ProductName = "Ghost", Category = "Tops", Status = "Active" }));
        }

        [ScratchDbFact]
        public async Task DeleteProduct_WithoutVariants_RemovesIt()
        {
            (_, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);

            await Repository().DeleteProductAsync(productId);

            Assert.Equal(0, await _db.CountAsync("Products"));
        }

        [ScratchDbFact]
        public async Task DeleteProduct_WithVariants_ArchivesProductAndDiscontinuesVariants()
        {
            (_, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);
            int variantId = await _db.AddVariantAsync(productId, "TEE-1");

            // The procedure reports the soft delete as a message raised at severity 16, so the call throws.
            SqlException ex = await Assert.ThrowsAsync<SqlException>(() => Repository().DeleteProductAsync(productId));

            Assert.Contains("soft-deleted", ex.Message);
            Assert.Equal("Archived", await ScalarStringAsync("SELECT Status FROM Products WHERE ProductID = " + productId));
            Assert.Equal("Discontinued", await ScalarStringAsync("SELECT Status FROM ProductVariants WHERE VariantID = " + variantId));
        }

        [ScratchDbFact]
        public async Task DeleteProduct_Unknown_IsRejected()
        {
            await SeedLookupsAsync();

            await Assert.ThrowsAsync<SqlException>(() => Repository().DeleteProductAsync(999999));
        }

        // ---- variants ----

        [ScratchDbFact]
        public async Task AddVariant_StoresVariantAndLogsInitialReceipt()
        {
            (int userId, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);

            await Repository().AddProductVariantAsync(Variant(productId, stock: 8), userId);

            int variantId = await _db.ScalarAsync<int>("SELECT VariantID FROM ProductVariants WHERE SKU = 'TEE-1'");
            Assert.Equal(8, await _db.StockAsync(variantId));
            Assert.Equal(8, await _db.ScalarAsync<int>(
                "SELECT QuantityChange FROM Inventory WHERE VariantID = @V AND Reason = 'Initial Receipt' AND ProcessedByUserID = @U",
                ("@V", variantId), ("@U", userId)));
        }

        [ScratchDbFact]
        public async Task AddVariant_WithZeroStock_LogsNothing()
        {
            (int userId, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);

            await Repository().AddProductVariantAsync(Variant(productId, stock: 0), userId);

            Assert.Equal(1, await _db.CountAsync("ProductVariants"));
            Assert.Equal(0, await _db.CountAsync("Inventory"));
        }

        [ScratchDbFact]
        public async Task AddVariant_SellingBelowCost_IsRejectedAndRolledBack()
        {
            (int userId, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);

            SqlException ex = await Assert.ThrowsAsync<SqlException>(
                () => Repository().AddProductVariantAsync(Variant(productId, cost: 30m, price: 20m), userId));

            Assert.Contains("Selling Price cannot be less than Cost Price", ex.Message);
            Assert.Equal(0, await _db.CountAsync("ProductVariants"));
            Assert.Equal(0, await _db.CountAsync("Inventory"));
        }

        [ScratchDbFact]
        public async Task AddVariant_DuplicateSku_IsRejected()
        {
            (int userId, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);
            await Repository().AddProductVariantAsync(Variant(productId), userId);

            SqlException ex = await Assert.ThrowsAsync<SqlException>(
                () => Repository().AddProductVariantAsync(Variant(productId), userId));

            Assert.Contains("SKU is already in use", ex.Message);
            Assert.Equal(1, await _db.CountAsync("ProductVariants"));
        }

        [ScratchDbFact]
        public async Task AddVariant_UnknownProduct_IsRejected()
        {
            (int userId, _, _) = await SeedLookupsAsync();

            await Assert.ThrowsAsync<SqlException>(() => Repository().AddProductVariantAsync(Variant(999999), userId));

            Assert.Equal(0, await _db.CountAsync("ProductVariants"));
        }

        [ScratchDbFact]
        public async Task EditVariant_UpdatesDetailsButNeverStock()
        {
            (_, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);
            int variantId = await _db.AddVariantAsync(productId, "TEE-1", stock: 12);
            ProductVariantModel edited = Variant(productId, "TEE-9", cost: 15m, price: 40m, stock: 999);
            edited.VariantID = variantId;
            edited.Size = "XL";
            edited.ReorderLevel = 2;
            edited.Status = "Discontinued";

            await Repository().EditProductVariantAsync(edited);

            Assert.Equal("TEE-9", await ScalarStringAsync("SELECT SKU FROM ProductVariants WHERE VariantID = " + variantId));
            Assert.Equal("XL", await ScalarStringAsync("SELECT Size FROM ProductVariants WHERE VariantID = " + variantId));
            Assert.Equal(40m, await _db.ScalarAsync<decimal>("SELECT SellingPrice FROM ProductVariants WHERE VariantID = @V", ("@V", variantId)));
            Assert.Equal(2, await _db.ScalarAsync<int>("SELECT ReorderLevel FROM ProductVariants WHERE VariantID = @V", ("@V", variantId)));
            Assert.Equal("Discontinued", await ScalarStringAsync("SELECT Status FROM ProductVariants WHERE VariantID = " + variantId));
            Assert.Equal(12, await _db.StockAsync(variantId));
        }

        [ScratchDbFact]
        public async Task EditVariant_SkuOfAnotherVariant_IsRejected()
        {
            (_, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);
            await _db.AddVariantAsync(productId, "TEE-1");
            int secondId = await _db.AddVariantAsync(productId, "TEE-2");
            ProductVariantModel edited = Variant(productId, "TEE-1");
            edited.VariantID = secondId;

            SqlException ex = await Assert.ThrowsAsync<SqlException>(() => Repository().EditProductVariantAsync(edited));

            Assert.Contains("SKU is already in use", ex.Message);
        }

        [ScratchDbFact]
        public async Task EditVariant_SellingBelowCost_IsRejectedAndChangesNothing()
        {
            (_, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);
            int variantId = await _db.AddVariantAsync(productId, "TEE-1", costPrice: 10m, sellingPrice: 20m);
            ProductVariantModel edited = Variant(productId, "TEE-1", cost: 50m, price: 20m);
            edited.VariantID = variantId;

            await Assert.ThrowsAsync<SqlException>(() => Repository().EditProductVariantAsync(edited));

            Assert.Equal(10m, await _db.ScalarAsync<decimal>("SELECT CostPrice FROM ProductVariants WHERE VariantID = @V", ("@V", variantId)));
        }

        [ScratchDbFact]
        public async Task DeleteVariant_NoStockAndNoSales_RemovesItAndItsLog()
        {
            (int userId, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);
            await Repository().AddProductVariantAsync(Variant(productId, stock: 3), userId);
            int variantId = await _db.ScalarAsync<int>("SELECT VariantID FROM ProductVariants");
            await _db.ExecuteAsync("UPDATE ProductVariants SET StockQuantity = 0 WHERE VariantID = @V", ("@V", variantId));

            await Repository().DeleteProductVariantAsync(variantId);

            Assert.Equal(0, await _db.CountAsync("ProductVariants"));
            Assert.Equal(0, await _db.CountAsync("Inventory"));
        }

        [ScratchDbFact]
        public async Task DeleteVariant_WithStock_IsRejected()
        {
            (_, int categoryId, _) = await SeedLookupsAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);
            int variantId = await _db.AddVariantAsync(productId, "TEE-1", stock: 4);

            SqlException ex = await Assert.ThrowsAsync<SqlException>(() => Repository().DeleteProductVariantAsync(variantId));

            Assert.Contains("Stock must be zero", ex.Message);
            Assert.Equal(1, await _db.CountAsync("ProductVariants"));
        }

        [ScratchDbFact]
        public async Task DeleteVariant_WithSalesHistory_IsRejected()
        {
            (int userId, int categoryId, _) = await SeedLookupsAsync();
            int customerId = await _db.SeedCustomerAsync();
            int productId = await _db.AddProductAsync("Classic Tee", categoryId);
            int variantId = await _db.AddVariantAsync(productId, "TEE-1", stock: 0);
            int saleId = await _db.ScalarAsync<int>(
                "INSERT INTO Sales (CustomerID, ProcessedByUserID, TotalAmount) OUTPUT INSERTED.SaleID VALUES (@C, @U, 20)",
                ("@C", customerId), ("@U", userId));
            await _db.ExecuteAsync(
                "INSERT INTO SalesItems (SaleID, VariantID, Quantity, UnitPrice, Discount, SubTotal) VALUES (@S, @V, 1, 20, 0, 20)",
                ("@S", saleId), ("@V", variantId));

            SqlException ex = await Assert.ThrowsAsync<SqlException>(() => Repository().DeleteProductVariantAsync(variantId));

            Assert.Contains("sales records", ex.Message);
            Assert.Equal(1, await _db.CountAsync("ProductVariants"));
        }

        [ScratchDbFact]
        public async Task DeleteVariant_Unknown_IsRejected()
        {
            await SeedLookupsAsync();

            await Assert.ThrowsAsync<SqlException>(() => Repository().DeleteProductVariantAsync(999999));
        }

        private async Task<string> ScalarStringAsync(string sql)
        {
            using SqlConnection connection = new SqlConnection(_db.ConnectionString);
            await connection.OpenAsync();
            using SqlCommand command = new SqlCommand(sql, connection);
            return (string)(await command.ExecuteScalarAsync())!;
        }
    }
}
