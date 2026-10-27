using Saleling.Model.Inventory;
using Saleling.Model.Product;
using Saleling.Model.Sale;
using Saleling.Repository;

namespace Saleling.Controller.Tests
{
    // Sales reports, KPIs and dashboard figures, against a scratch database with two real sales:
    //   cashier1: 2 x (price 100, cost 10), discount 20  -> revenue 180, COGS 20, profit 160
    //   cashier2: 1 x (price  50, cost 20)               -> revenue  50, COGS 20, profit  30
    public class SalesReportProcedureTests : IClassFixture<ScratchDatabase>
    {
        private readonly ScratchDatabase _db;
        private int _cashier1;
        private int _cashier2;

        public SalesReportProcedureTests(ScratchDatabase db)
        {
            _db = db;
        }

        private SalesRepository Sales() => new SalesRepository(_db.ConnectionString);

        private async Task SeedSalesAsync()
        {
            await _db.ResetAsync();
            _cashier1 = await _db.SeedUserAsync("Cashier", "cashier1");
            _cashier2 = await _db.SeedUserAsync("Cashier", "cashier2");
            int customerId = await _db.SeedCustomerAsync();
            int category = await _db.AddCategoryAsync("Tops");
            int product = await _db.AddProductAsync("Classic Tee", category);
            int shirt = await _db.AddVariantAsync(product, "TEE-1", costPrice: 10m, sellingPrice: 100m, stock: 20);
            int socks = await _db.AddVariantAsync(product, "SOCK-1", costPrice: 20m, sellingPrice: 50m, stock: 20);

            await Sales().ProcessSaleAsync(new SalesTransactionModel
            {
                CustomerID = customerId,
                ProcessedByUserID = _cashier1,
                TotalAmount = 180m,
                Items = { new SalesItemModel { VariantID = shirt, Quantity = 2, Discount = 20m } }
            });
            await Sales().ProcessSaleAsync(new SalesTransactionModel
            {
                CustomerID = customerId,
                ProcessedByUserID = _cashier2,
                TotalAmount = 50m,
                Items = { new SalesItemModel { VariantID = socks, Quantity = 1 } }
            });
        }

        private static DateTime Today => DateTime.Today;

        [ScratchDbFact]
        public async Task SalesReport_HasOneRowPerItemWithRevenueCostAndProfit()
        {
            await SeedSalesAsync();

            List<SalesReportItemModel> rows = await Sales().GetSalesReportAsync(Today, Today);

            Assert.Equal(2, rows.Count);
            SalesReportItemModel shirtRow = rows.Single(r => r.SKU == "TEE-1");
            Assert.Equal(2, shirtRow.Quantity);
            Assert.Equal(100m, shirtRow.UnitPrice);
            Assert.Equal(20m, shirtRow.Discount);
            Assert.Equal(180m, shirtRow.Revenue);
            Assert.Equal(20m, shirtRow.COGS);
            Assert.Equal(160m, shirtRow.GrossProfit);
            Assert.Equal("Test User", shirtRow.ProcessedBy);
        }

        [ScratchDbFact]
        public async Task SalesReport_CanBeLimitedToOneCashier()
        {
            await SeedSalesAsync();

            SalesReportItemModel row = Assert.Single(await Sales().GetSalesReportAsync(Today, Today, _cashier2));

            Assert.Equal("SOCK-1", row.SKU);
            Assert.Equal(30m, row.GrossProfit);
        }

        [ScratchDbFact]
        public async Task SalesReport_OutsideTheDateRange_IsEmpty()
        {
            await SeedSalesAsync();
            DateTime yesterday = Today.AddDays(-1);

            Assert.Empty(await Sales().GetSalesReportAsync(yesterday, yesterday));
        }

        [ScratchDbFact]
        public async Task SalesReport_IncludesSalesLaterOnTheLastDay()
        {
            await SeedSalesAsync();
            await _db.ExecuteAsync("UPDATE Sales SET SaleDate = DATEADD(second, -1, DATEADD(day, 1, CAST(CAST(GETDATE() AS DATE) AS DATETIME2)))");

            Assert.Equal(2, (await Sales().GetSalesReportAsync(Today, Today)).Count);
        }

        [ScratchDbFact]
        public async Task SalesKpis_CoverAllCashiers()
        {
            await SeedSalesAsync();

            SalesReportKPIModel? kpi = await Sales().GetSaleReportsKPIAsync(Today, Today);

            Assert.NotNull(kpi);
            Assert.Equal(2, kpi.TransactionCount);
            Assert.Equal(230m, kpi.TotalSalesRevenue);
            Assert.Equal(3, kpi.ItemsSoldCount);
            Assert.Equal(115m, kpi.AverageSaleValue);
        }

        [ScratchDbFact]
        public async Task SalesKpis_CanBeLimitedToOneCashier()
        {
            await SeedSalesAsync();

            SalesReportKPIModel? kpi = await Sales().GetSaleReportsKPIAsync(Today, Today, _cashier1);

            Assert.NotNull(kpi);
            Assert.Equal(1, kpi.TransactionCount);
            Assert.Equal(180m, kpi.TotalSalesRevenue);
            Assert.Equal(2, kpi.ItemsSoldCount);
        }

        [ScratchDbFact]
        public async Task SalesKpis_WithNoSales_AreZeroNotMissing()
        {
            await SeedSalesAsync();
            DateTime yesterday = Today.AddDays(-1);

            SalesReportKPIModel? kpi = await Sales().GetSaleReportsKPIAsync(yesterday, yesterday);

            Assert.NotNull(kpi);
            Assert.Equal(0, kpi.TransactionCount);
            Assert.Equal(0m, kpi.TotalSalesRevenue);
            Assert.Equal(0, kpi.ItemsSoldCount);
            Assert.Equal(0m, kpi.AverageSaleValue);
        }

        [ScratchDbFact]
        public async Task DashboardFigures_AreTodayOnlyAndPerCashierWhereAsked()
        {
            await SeedSalesAsync();
            int customerId = await _db.ScalarAsync<int>("SELECT TOP 1 CustomerID FROM Customers");
            // An old sale that must not show up in any of today's figures.
            await _db.ExecuteAsync(
                "INSERT INTO Sales (CustomerID, ProcessedByUserID, SaleDate, TotalAmount) VALUES (@C, @U, DATEADD(day, -1, SYSDATETIME()), 999)",
                ("@C", customerId), ("@U", _cashier1));

            Assert.Equal(230m, await Sales().GetTotalSalesAsync());
            Assert.Equal(2, await Sales().GetTransactionCountAsync());
            Assert.Equal(180m, await Sales().GetTotalSalesTodayByUserIDAsync(_cashier1));
            Assert.Equal(2, await Sales().GetTotalItemsSoldTodayByUserIDAsync(_cashier1));
            Assert.Equal(1, await Sales().GetTransactionCountTodayByUserIDAsync(_cashier1));
            Assert.Equal(2m, await Sales().GetAvgItemsPerTransactionTodayByUserIDAsync(_cashier1));
        }

        [ScratchDbFact]
        public async Task DashboardFigures_ForACashierWithNoSales_AreZero()
        {
            await SeedSalesAsync();
            int idle = await _db.SeedUserAsync("Cashier", "idle");

            Assert.Equal(0m, await Sales().GetTotalSalesTodayByUserIDAsync(idle));
            Assert.Equal(0, await Sales().GetTotalItemsSoldTodayByUserIDAsync(idle));
            Assert.Equal(0, await Sales().GetTransactionCountTodayByUserIDAsync(idle));
            Assert.Equal(0m, await Sales().GetAvgItemsPerTransactionTodayByUserIDAsync(idle));
        }

        [ScratchDbFact]
        public async Task RecentSales_AreTodayOnlyAndCanBeLimitedToACashier()
        {
            await SeedSalesAsync();

            Assert.Equal(2, (await Sales().GetRecentSalesAsync()).Count);
            SalesAlertModel mine = Assert.Single(await Sales().GetRecentSalesAsync(_cashier1));
            Assert.Equal(180m, mine.TotalAmount);
        }
    }

    // Stock report, KPIs, alerts and counts, plus the inventory movement report, against this catalogue:
    //   A  stock 10, cost 10, reorder 5   in stock
    //   B  stock  3, cost 20, reorder 5   low
    //   C  stock  0, cost 10, reorder 5   out of stock
    //   D  stock 50, cost 100             discontinued (ignored everywhere)
    //   E  stock  0                       discontinued (ignored everywhere)
    public class InventoryReportProcedureTests : IClassFixture<ScratchDatabase>
    {
        private readonly ScratchDatabase _db;

        public InventoryReportProcedureTests(ScratchDatabase db)
        {
            _db = db;
        }

        private InventoryRepository Inventory() => new InventoryRepository(_db.ConnectionString);

        private async Task<int> SeedStockAsync()
        {
            await _db.ResetAsync();
            int category = await _db.AddCategoryAsync("Tops");
            int product = await _db.AddProductAsync("Classic Tee", category);
            int a = await _db.AddVariantAsync(product, "A", costPrice: 10m, stock: 10);
            await _db.AddVariantAsync(product, "B", costPrice: 20m, stock: 3);
            await _db.AddVariantAsync(product, "C", costPrice: 10m, stock: 0);
            await _db.AddVariantAsync(product, "D", costPrice: 100m, stock: 50, status: "Discontinued");
            await _db.AddVariantAsync(product, "E", costPrice: 10m, stock: 0, status: "Discontinued");
            return a;
        }

        [ScratchDbFact]
        public async Task StockKpis_ValueTheStockAndRateTheLowSkus()
        {
            await SeedStockAsync();

            InventoryStockKPIModel? kpi = await Inventory().GetStockKPIsAsync();

            Assert.NotNull(kpi);
            Assert.Equal(160m, kpi.TotalInventoryValue);
            Assert.Equal(2, kpi.TotalSKUsInStock);
            Assert.Equal(2, kpi.TotalSKUsBelowReorderLevel);
            Assert.Equal(66.67m, kpi.LowStockRate);
        }

        [ScratchDbFact]
        public async Task StockKpis_RateNeverExceedsOneHundredPercent()
        {
            await SeedStockAsync();
            await _db.ExecuteAsync("UPDATE ProductVariants SET StockQuantity = 0 WHERE SKU = 'A'");

            InventoryStockKPIModel? kpi = await Inventory().GetStockKPIsAsync();

            Assert.NotNull(kpi);
            Assert.InRange(kpi.LowStockRate, 0m, 100m);
        }

        [ScratchDbFact]
        public async Task StockKpis_WithAThousandLowSkus_DoNotOverflow()
        {
            await SeedStockAsync();
            await _db.ExecuteAsync(
                "INSERT INTO ProductVariants (ProductID, SKU, Size, Color, CostPrice, SellingPrice, StockQuantity, ReorderLevel, Status) " +
                "SELECT TOP 1200 (SELECT TOP 1 ProductID FROM Products), 'BULK-' + CAST(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS VARCHAR(10)), " +
                "'M', 'Black', 1, 2, 0, 5, 'Active' FROM sys.all_objects a CROSS JOIN sys.all_objects b");

            InventoryStockKPIModel? kpi = await Inventory().GetStockKPIsAsync();

            Assert.NotNull(kpi);
            Assert.Equal(1202, kpi.TotalSKUsBelowReorderLevel);
            Assert.InRange(kpi.LowStockRate, 99m, 100m);
        }

        [ScratchDbFact]
        public async Task StockKpis_OnAnEmptyCatalogue_AreZero()
        {
            await _db.ResetAsync();

            InventoryStockKPIModel? kpi = await Inventory().GetStockKPIsAsync();

            Assert.NotNull(kpi);
            Assert.Equal(0m, kpi.TotalInventoryValue);
            Assert.Equal(0, kpi.TotalSKUsInStock);
            Assert.Equal(0m, kpi.LowStockRate);
        }

        [ScratchDbFact]
        public async Task StockReport_LabelsEachSkuAndOrdersByLowestStock()
        {
            await SeedStockAsync();

            List<InventoryStockReportModel> report = await Inventory().GetStockReportAsync();

            Assert.Equal(new[] { "C", "B", "A" }, report.Select(r => r.SKU).ToArray());
            Assert.Equal(new[] { "Out of Stock", "Low Stock (3 left)", "In Stock" }, report.Select(r => r.Status).ToArray());
        }

        [ScratchDbFact]
        public async Task StockAlerts_ListOnlyActiveLowAndOutOfStockSkus()
        {
            await SeedStockAsync();
            ProductRepository products = new ProductRepository(_db.ConnectionString);

            List<ProductStockAlertModel> alerts = await products.GetStockAlertsAsync();

            Assert.Equal(new[] { "C", "B" }, alerts.Select(a => a.SKU).ToArray());
            Assert.Equal(new[] { "Out of Stock", "3 left" }, alerts.Select(a => a.Status).ToArray());
        }

        [ScratchDbFact]
        public async Task StockAlerts_ForAProductWithoutASupplier_DoNotCrash()
        {
            await SeedStockAsync();
            ProductRepository products = new ProductRepository(_db.ConnectionString);

            List<ProductStockAlertModel> alerts = await products.GetStockAlertsAsync();

            Assert.All(alerts, a => Assert.Equal("No Assigned Supplier", a.Supplier));
        }

        [ScratchDbFact]
        public async Task StockCounts_IgnoreDiscontinuedVariants()
        {
            await SeedStockAsync();

            Assert.Equal(2, await Inventory().GetLowStockCountAsync());
            Assert.Equal(1, await Inventory().GetOutOfStockCountAsync());
        }

        [ScratchDbFact]
        public async Task MovementReport_AndKpis_CoverTheRequestedDaysOnly()
        {
            int variantId = await SeedStockAsync();
            int admin = await _db.SeedUserAsync("Admin", "admin1");
            await _db.ExecuteAsync(
                "INSERT INTO Inventory (VariantID, QuantityChange, Reason, ProcessedByUserID) VALUES (@V, 10, 'Initial Receipt', @U), (@V, -3, 'Sale', @U)",
                ("@V", variantId), ("@U", admin));
            await Inventory().LogTransactionAsync(new InventoryLogTransactionModel
            { VariantID = variantId, QuantityChange = 5, Reason = "Found Stock", ProcessedByUserID = admin, Notes = "recount" });
            await Inventory().LogTransactionAsync(new InventoryLogTransactionModel
            { VariantID = variantId, QuantityChange = 2, Reason = "Damaged/Scrap", ProcessedByUserID = admin, Notes = "torn" });
            // Yesterday's movement must be left out of today's report.
            await _db.ExecuteAsync(
                "INSERT INTO Inventory (VariantID, QuantityChange, ChangeDate, Reason, ProcessedByUserID) VALUES (@V, -9, DATEADD(day, -1, SYSDATETIME()), 'Sale', @U)",
                ("@V", variantId), ("@U", admin));
            DateTime today = DateTime.Today;

            InventoryMovementKPIModel? kpi = await Inventory().GetMovementKPIsAsync(today, today);
            List<InventoryMovementReportModel> rows = await Inventory().GenerateMovementReportAsync(today, today);

            Assert.NotNull(kpi);
            Assert.Equal(3, kpi.TotalUnitsSold);
            Assert.Equal(10, kpi.TotalUnitsReceived);
            Assert.Equal(10, kpi.NetInventoryChange);
            Assert.Equal(7, kpi.TotalAdjustments);
            Assert.Equal(4, rows.Count);
            Assert.DoesNotContain(rows, r => r.QuantityChange == -9);
        }

        [ScratchDbFact]
        public async Task ItemLog_WithNoNotes_StillLoads()
        {
            int variantId = await SeedStockAsync();
            int admin = await _db.SeedUserAsync("Admin", "admin1");
            await _db.ExecuteAsync(
                "INSERT INTO Inventory (VariantID, QuantityChange, Reason, ProcessedByUserID) VALUES (@V, 4, 'Found Stock', @U)",
                ("@V", variantId), ("@U", admin));

            InventoryLogModel log = Assert.Single(await Inventory().GetLogsAsync(variantId));

            Assert.Equal(string.Empty, log.Notes);
            Assert.Equal(4, log.QuantityChange);
        }

        [ScratchDbFact]
        public async Task MovementKpis_WithNoMovement_AreZero()
        {
            await SeedStockAsync();
            DateTime today = DateTime.Today;

            InventoryMovementKPIModel? kpi = await Inventory().GetMovementKPIsAsync(today, today);

            Assert.NotNull(kpi);
            Assert.Equal(0, kpi.TotalUnitsSold);
            Assert.Equal(0, kpi.NetInventoryChange);
        }
    }
}
