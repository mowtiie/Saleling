using Saleling.Model.Inventory;
using Saleling.Model.Product;
using Saleling.Repository;

namespace Saleling.Controller.Tests
{
    // The search and listing procedures, exercised through the repositories against a scratch database.
    //
    // Catalogue used by every test:
    //   Classic Tee  (Tops,  Acme, Active)    TEE-1 stock 10 | TEE-2 stock 0 | TEE-3 stock 5 (Discontinued)
    //   Slim Jeans   (Denim, Zeta, Active)    JEAN-1 stock 7
    //   Old Parka    (Tops,  no supplier, Archived)  PARKA-1 stock 3 (Discontinued)
    public class SearchProcedureTests : IClassFixture<ScratchDatabase>
    {
        private readonly ScratchDatabase _db;

        public SearchProcedureTests(ScratchDatabase db)
        {
            _db = db;
        }

        private ProductRepository Products() => new ProductRepository(_db.ConnectionString);

        private InventoryRepository Inventory() => new InventoryRepository(_db.ConnectionString);

        private async Task SeedCatalogueAsync()
        {
            await _db.ResetAsync();
            int tops = await _db.AddCategoryAsync("Tops");
            int denim = await _db.AddCategoryAsync("Denim");
            int acme = await _db.AddSupplierAsync("Acme");
            int zeta = await _db.AddSupplierAsync("Zeta");

            int tee = await _db.AddProductAsync("Classic Tee", tops, acme);
            await _db.AddVariantAsync(tee, "TEE-1", stock: 10);
            await _db.AddVariantAsync(tee, "TEE-2", stock: 0);
            await _db.AddVariantAsync(tee, "TEE-3", stock: 5, status: "Discontinued");

            int jeans = await _db.AddProductAsync("Slim Jeans", denim, zeta);
            await _db.AddVariantAsync(jeans, "JEAN-1", stock: 7);

            int parka = await _db.AddProductAsync("Old Parka", tops, null, "Archived");
            await _db.AddVariantAsync(parka, "PARKA-1", stock: 3, status: "Discontinued");
        }

        private static string[] Names(IEnumerable<ProductModel> products) => products.Select(p => p.ProductName).OrderBy(n => n).ToArray();

        private static string[] Skus(IEnumerable<POSProductModel> products) => products.Select(p => p.SKU).OrderBy(s => s).ToArray();

        private static string[] Skus(IEnumerable<ProductListingModel> products) => products.Select(p => p.SKU).OrderBy(s => s).ToArray();

        private static string[] Skus(IEnumerable<InventoryItemModel> items) => items.Select(i => i.SKU).OrderBy(s => s).ToArray();

        // ---- product maintenance search (includes archived products) ----

        [ScratchDbFact]
        public async Task ProductSearch_ByName()
        {
            await SeedCatalogueAsync();

            Assert.Equal(new[] { "Classic Tee" }, Names(await Products().SearchProductsAsync("tee", "Name")));
        }

        [ScratchDbFact]
        public async Task ProductSearch_ByCategory_IncludesArchivedProducts()
        {
            await SeedCatalogueAsync();

            Assert.Equal(new[] { "Classic Tee", "Old Parka" }, Names(await Products().SearchProductsAsync("tops", "Category")));
        }

        [ScratchDbFact]
        public async Task ProductSearch_BySupplier()
        {
            await SeedCatalogueAsync();

            Assert.Equal(new[] { "Slim Jeans" }, Names(await Products().SearchProductsAsync("zeta", "Supplier")));
        }

        [ScratchDbFact]
        public async Task ProductSearch_NoMatch_ReturnsNothing()
        {
            await SeedCatalogueAsync();

            Assert.Empty(await Products().SearchProductsAsync("zzz", "Name"));
        }

        [ScratchDbFact]
        public async Task ProductSearch_TermIsTrimmedAndCaseInsensitive()
        {
            await SeedCatalogueAsync();

            Assert.Equal(new[] { "Slim Jeans" }, Names(await Products().SearchProductsAsync("  JEANS  ", "Name")));
        }

        // ---- product listing (active products and variants only) ----

        [ScratchDbFact]
        public async Task ProductListing_ShowsOnlyActiveVariantsOfActiveProducts()
        {
            await SeedCatalogueAsync();

            Assert.Equal(new[] { "JEAN-1", "TEE-1", "TEE-2" }, Skus(await Products().GetProductListingsAsync()));
        }

        [ScratchDbFact]
        public async Task ProductListingSearch_BySku_SkipsDiscontinuedVariants()
        {
            await SeedCatalogueAsync();

            Assert.Equal(new[] { "TEE-1", "TEE-2" }, Skus(await Products().SearchProductListingAsync("tee", "SKU")));
        }

        [ScratchDbFact]
        public async Task ProductListingSearch_ByCategoryAndSupplier()
        {
            await SeedCatalogueAsync();

            Assert.Equal(new[] { "TEE-1", "TEE-2" }, Skus(await Products().SearchProductListingAsync("tops", "Category")));
            Assert.Equal(new[] { "JEAN-1" }, Skus(await Products().SearchProductListingAsync("zeta", "Supplier")));
        }

        [ScratchDbFact]
        public async Task ProductListing_ShowsActiveProductsWithoutASupplier()
        {
            await SeedCatalogueAsync();
            int tops = await _db.ScalarAsync<int>("SELECT CategoryID FROM Categories WHERE CategoryName = 'Tops'");
            int loner = await _db.AddProductAsync("Supplierless Hat", tops, null);
            await _db.AddVariantAsync(loner, "HAT-1", stock: 4);

            ProductListingModel hat = Assert.Single(
                await Products().GetProductListingsAsync(), p => p.SKU == "HAT-1");
            ProductListingModel searched = Assert.Single(await Products().SearchProductListingAsync("hat", "SKU"));

            Assert.Equal("No Assigned Supplier", hat.Supplier);
            Assert.Equal("HAT-1", searched.SKU);
        }

        // ---- saleable products (what the cashier can put in a cart) ----

        [ScratchDbFact]
        public async Task SaleableProducts_ExcludeOutOfStockDiscontinuedAndArchived()
        {
            await SeedCatalogueAsync();

            Assert.Equal(new[] { "JEAN-1", "TEE-1" }, Skus(await Products().GetSaleableProductsAsync()));
        }

        [ScratchDbFact]
        public async Task SaleableSearch_ByNameAndCategory_StaysWithinSaleableStock()
        {
            await SeedCatalogueAsync();

            Assert.Equal(new[] { "TEE-1" }, Skus(await Products().SearchSaleableProductsAsync("tee", "Name")));
            Assert.Equal(new[] { "TEE-1" }, Skus(await Products().SearchSaleableProductsAsync("tops", "Category")));
            Assert.Equal(new[] { "JEAN-1" }, Skus(await Products().SearchSaleableProductsAsync("denim", "Category")));
        }

        [ScratchDbFact]
        public async Task SaleableSearch_WithoutAFilter_StillExcludesUnsaleableVariants()
        {
            await SeedCatalogueAsync();

            // Out-of-stock TEE-2, discontinued TEE-3 and the archived parka must never appear,
            // whichever way the procedure is called.
            Assert.Equal(new[] { "TEE-1" }, Skus(await Products().SearchSaleableProductsAsync("tee", null!)));
            Assert.Equal(new[] { "TEE-1" }, Skus(await Products().SearchSaleableProductsAsync("tops", null!)));
        }

        // ---- inventory search (every variant, whatever its status) ----

        [ScratchDbFact]
        public async Task InventoryGetAll_ReturnsEveryVariant()
        {
            await SeedCatalogueAsync();

            Assert.Equal(new[] { "JEAN-1", "PARKA-1", "TEE-1", "TEE-2", "TEE-3" }, Skus(await Inventory().GetAllAsync()));
        }

        [ScratchDbFact]
        public async Task InventorySearch_ByEachFilter()
        {
            await SeedCatalogueAsync();

            Assert.Equal(new[] { "JEAN-1" }, Skus(await Inventory().SearchInventoryAsync("jean", "SKU")));
            Assert.Equal(new[] { "TEE-1", "TEE-2", "TEE-3" }, Skus(await Inventory().SearchInventoryAsync("classic", "Name")));
            Assert.Equal(new[] { "JEAN-1" }, Skus(await Inventory().SearchInventoryAsync("denim", "Category")));
            Assert.Equal(new[] { "TEE-1", "TEE-2", "TEE-3" }, Skus(await Inventory().SearchInventoryAsync("acme", "Supplier")));
        }

        [ScratchDbFact]
        public async Task InventorySearch_ReportsStatusesAndMissingSupplier()
        {
            await SeedCatalogueAsync();

            InventoryItemModel parka = Assert.Single(await Inventory().SearchInventoryAsync("parka", "SKU"));

            Assert.Equal("Archived", parka.ProductStatus);
            Assert.Equal("Discontinued", parka.VariantStatus);
            Assert.Equal(3, parka.StockQuantity);
            Assert.Equal("No Assigned Supplier", parka.Supplier);
        }
    }
}
