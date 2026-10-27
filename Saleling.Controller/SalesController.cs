using Saleling.Model;
using Saleling.Model.Product;
using Saleling.Model.Sale;
using Saleling.Repository;
using Saleling.Util;
using System.Xml.Linq;

namespace Saleling.Controller
{
    public class SalesController
    {
        private readonly SalesRepository _salesRepository;
        private readonly ProductRepository _productRepository;

        private readonly int _currentCashierID;

        private readonly List<SalesCartModel> _cartItems;

        public IReadOnlyList<SalesCartModel> CartItems => _cartItems;

        public decimal PaymentAmount { get; set; }

        public SalesController() : this(new SalesRepository(), new ProductRepository())
        {
        }

        public SalesController(SalesRepository salesRepository, ProductRepository productRepository)
        {
            _productRepository = productRepository;
            _salesRepository = salesRepository;
            _currentCashierID = SessionUtil.Instance.RequireLogin().UserID;
            _cartItems = new List<SalesCartModel>();
            PaymentAmount = 0.00m;
        }

        public void AddItemToCart(POSProductModel product)
        {
            const int quantityToAdd = 1;
            SalesCartModel? existingItem = _cartItems.FirstOrDefault(i => i.VariantID == product.VariantID);

            if (existingItem != null)
            {
                if (existingItem.Quantity + quantityToAdd > product.StockQuantity)
                {
                    int remaining = product.StockQuantity - existingItem.Quantity;
                    throw new InvalidOperationException($"Cannot add 1 unit. Only {remaining} remaining in stock.");
                }
                existingItem.Quantity += quantityToAdd;
            }
            else
            {
                if (quantityToAdd > product.StockQuantity)
                {
                    throw new InvalidOperationException($"Insufficient stock. Only {product.StockQuantity} available.");
                }

                _cartItems.Add(new SalesCartModel
                {
                    VariantID = product.VariantID,
                    ProductName = product.ProductName,
                    SKU = product.SKU,
                    Size = product.Size,
                    Color = product.Color,
                    CurrentStock = product.StockQuantity,
                    Quantity = quantityToAdd,
                    UnitPrice = product.SellingPrice,
                    Discount = 0.00m
                });
            }

            RecalculateCartTotals();
        }

        public decimal GetChangeDue()
        {
            decimal grandTotal = RecalculateCartTotals();
            decimal changeDue = PaymentAmount - grandTotal;
            return Math.Max(0.00m, changeDue);
        }

        public decimal GetTotalDiscount()
        {
            return _cartItems.Sum(i => i.Discount);
        }

        public int GetTotalItems()
        {
            return _cartItems.Sum(i => i.Quantity);
        }

        public decimal GetSubTotal()
        {
            return _cartItems.Sum(i => i.Quantity * i.UnitPrice);
        }

        public decimal GetChangeDue(decimal amountTendered)
        {
            decimal grandTotal = RecalculateCartTotals();
            return amountTendered - grandTotal;
        }

        public void RemoveItemFromCart(int variantId)
        {
            SalesCartModel? itemToRemove = _cartItems.FirstOrDefault(i => i.VariantID == variantId);

            if (itemToRemove == null)
            {
                throw new InvalidOperationException($"Item with Variant ID {variantId} not found in cart.");
            }

            _cartItems.Remove(itemToRemove);
            RecalculateCartTotals();
        }

        public void ClearCart()
        {
            _cartItems.Clear();
            PaymentAmount = 0.00m;
            RecalculateCartTotals();
        }

        public decimal RecalculateCartTotals()
        {
            if (!_cartItems.Any())
            {
                return 0.00m;
            }

            decimal grandTotal = 0.00m;

            foreach (SalesCartModel item in _cartItems)
            {
                decimal originalLineTotal = item.Quantity * item.UnitPrice;
                decimal discountedLineTotal = originalLineTotal - item.Discount;

                item.LineTotal = Math.Max(0.00m, discountedLineTotal);
                grandTotal += item.LineTotal;
            }

            return grandTotal;
        }

        public async Task<int> FinalizeSaleAsync(int customerID)
        {
            SessionUtil.Instance.RequireRole(Roles.Cashier);

            if (!_cartItems.Any())
            {
                throw new InvalidOperationException("Cannot process sale: The cart is empty.");
            }

            decimal finalTotal = RecalculateCartTotals();
            if (PaymentAmount < finalTotal)
            {
                throw new InvalidOperationException($"Cannot finalize sale: Insufficient payment. Amount due: {finalTotal - PaymentAmount:C2}");
            }

            List<SalesItemModel> itemDataModels = _cartItems.Select(item => new SalesItemModel
            {
                VariantID = item.VariantID,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                SubTotal = item.LineTotal
            }).ToList();

            SalesTransactionModel transactionModel = new SalesTransactionModel
            {
                CustomerID = customerID,
                ProcessedByUserID = _currentCashierID,
                TotalAmount = finalTotal,
                Items = itemDataModels
            };

            int newSaleId = await _salesRepository.ProcessSaleAsync(transactionModel);
            ClearCart();

            return newSaleId;
        }

        public async Task<List<POSProductModel>> GetAvailableProductsAsync()
        {
            SessionUtil.Instance.RequireLogin();
            return await _productRepository.GetSaleableProductsAsync();
        }

        public async Task<List<POSProductModel>> SearchAvailableProductsAsync(string searchTerm, string searchFilter)
        {
            SessionUtil.Instance.RequireLogin();
            if (string.IsNullOrEmpty(searchTerm) || string.IsNullOrEmpty(searchFilter))
            {
                throw new InvalidOperationException("Please select both a search term and a search filter");
            }

            return await _productRepository.SearchSaleableProductsAsync(searchTerm, searchFilter);
        }

        public void RemoveItemDiscount(int variantId)
        {
            SalesCartModel? item = _cartItems.FirstOrDefault(i => i.VariantID == variantId);

            if (item == null) return;

            item.Discount = 0.00m;
            RecalculateCartTotals();
        }

        public void UpdateBulkItemQuantity(List<int> variantIds, int newQuantity)
        {
            if (newQuantity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(newQuantity), "Quantity must be at least 1.");
            }

            if (!variantIds.Any())
            {
                return;
            }

            List<string> failedItemNames = new List<string>();

            foreach (int variantId in variantIds)
            {
                SalesCartModel? item = _cartItems.FirstOrDefault(i => i.VariantID == variantId);

                if (item == null) continue;

                if (newQuantity > item.CurrentStock)
                {
                    failedItemNames.Add($"{item.ProductName} (Max: {item.CurrentStock})");
                }
                else
                {
                    item.Quantity = newQuantity;
                }
            }

            RecalculateCartTotals();

            if (failedItemNames.Any())
            {
                string itemList = string.Join(", ", failedItemNames.Distinct().Take(3));

                if (failedItemNames.Distinct().Count() > 3)
                {
                    itemList += ", and others";
                }

                throw new InvalidOperationException(
                    $"Quantity update failed for item(s): {itemList}. " +
                    "The requested quantity exceeds the available stock for these items."
                );
            }
        }

        public void ApplyBulkDiscountToSelectedItems(List<int> variantIds, decimal discountAmount)
        {
            if (discountAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(discountAmount), "Discount amount cannot be negative.");
            }

            if (!variantIds.Any())
            {
                return;
            }

            List<string> cappedItemNames = new List<string>();

            foreach (int variantId in variantIds)
            {
                SalesCartModel? item = _cartItems.FirstOrDefault(i => i.VariantID == variantId);

                if (item == null) continue;

                decimal originalLineTotal = item.Quantity * item.UnitPrice;

                if (discountAmount > originalLineTotal)
                {
                    item.Discount = originalLineTotal;
                    cappedItemNames.Add(item.ProductName);
                }
                else
                {
                    item.Discount = discountAmount;
                }
            }

            RecalculateCartTotals();

            if (cappedItemNames.Any())
            {
                string itemList = string.Join(", ", cappedItemNames.Distinct().Take(3));

                if (cappedItemNames.Distinct().Count() > 3)
                {
                    itemList += ", and others";
                }

                throw new InvalidOperationException(
                    $"The applied discount amount exceeded the total price for item(s): {itemList}. " +
                    "The discount was automatically adjusted to the maximum allowed (the line total) for these items."
                );
            }
        }

        public async Task<List<SalesReportItemModel>> GetSalesReportAsync(
            string reportPeriod,
            DateTime? customStartDate,
            DateTime? customEndDate,
            int? processedByUserID = null)
        {
            RequireOwnDataUnlessAdmin(processedByUserID);

            DateTime? startDate = null;
            DateTime? endDate = null;

            if (!reportPeriod.Equals("Custom"))
            {
                (startDate, endDate) = CalculateDateRange(reportPeriod);
            }
            else
            {
                startDate = customStartDate?.Date;
                endDate = customEndDate?.Date;
            }

            return await _salesRepository.GetSalesReportAsync(startDate, endDate, processedByUserID);
        }

        public async Task<SalesReportKPIModel> GetKPIsAsync(
            string reportPeriod,
            DateTime? customStartDate,
            DateTime? customEndDate,
            int? processedByUserID = null)
        {
            RequireOwnDataUnlessAdmin(processedByUserID);

            DateTime? startDate = null;
            DateTime? endDate = null;

            if (!reportPeriod.Equals("Custom"))
            {
                (startDate, endDate) = CalculateDateRange(reportPeriod);
            }
            else
            {
                startDate = customStartDate?.Date;
                endDate = customEndDate?.Date;
            }

            SalesReportKPIModel? saleReportsKPI = await _salesRepository.GetSaleReportsKPIAsync(startDate, endDate, processedByUserID);
            if (saleReportsKPI == null)
            {
                throw new Exception("An error occured when retrieving sale reports KPIs.");
            }
            else
            {
                return saleReportsKPI;
            }
        }

        private (DateTime? startDate, DateTime? endDate) CalculateDateRange(string reportPeriod)
        {
            DateTime today = DateTime.Today;
            DateTime? startDate = null;
            DateTime? endDate = today;

            switch (reportPeriod)
            {
                case "Daily":
                    startDate = today;
                    break;

                case "Weekly":
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                    startDate = today.AddDays(-1 * diff).Date;
                    break;

                case "Monthly":
                    startDate = new DateTime(today.Year, today.Month, 1);
                    break;

                case "Yearly":
                    startDate = new DateTime(today.Year, 1, 1);
                    break;

                default:
                    break;
            }

            return (startDate, endDate);
        }

        public async Task<decimal> GetTotalSalesAsync()
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            return await _salesRepository.GetTotalSalesAsync();
        }

        public async Task<int> GetTransactionCountAsync()
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            return await _salesRepository.GetTransactionCountAsync();
        }

        public async Task<decimal> GetTotalSalesTodayByUserIDAsync(int userID)
        {
            RequireOwnDataUnlessAdmin(userID);
            return await _salesRepository.GetTotalSalesTodayByUserIDAsync(userID);
        }

        public async Task<decimal> GetAvgItemsPerTransactionTodayByUserIDAsync(int userID)
        {
            RequireOwnDataUnlessAdmin(userID);
            return await _salesRepository.GetAvgItemsPerTransactionTodayByUserIDAsync(userID);
        }

        public async Task<int> GetTotalItemsSoldTodayByUserIDAsync(int userID)
        {
            RequireOwnDataUnlessAdmin(userID);
            return await _salesRepository.GetTotalItemsSoldTodayByUserIDAsync(userID);
        }

        public async Task<int> GetTransactionCountTodayByUserIDAsync(int userID)
        {
            RequireOwnDataUnlessAdmin(userID);
            return await _salesRepository.GetTransactionCountTodayByUserIDAsync(userID);
        }

        public async Task<List<SalesAlertModel>> GetRecentSalesAsync(int? processedByUserID = null)
        {
            RequireOwnDataUnlessAdmin(processedByUserID);
            return await _salesRepository.GetRecentSalesAsync(processedByUserID);
        }

        // Admins may look at any cashier's data (or everyone's, with null); cashiers only at their own.
        private static void RequireOwnDataUnlessAdmin(int? requestedUserID)
        {
            UserModel user = SessionUtil.Instance.RequireLogin();
            if (user.Role == Roles.Admin)
            {
                return;
            }

            if (requestedUserID != user.UserID)
            {
                throw new UnauthorizedAccessException("You can only view your own sales data.");
            }
        }
    }
}
