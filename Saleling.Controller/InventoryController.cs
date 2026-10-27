using Saleling.Model.Inventory;
using Saleling.Repository;
using Saleling.Util;

namespace Saleling.Controller
{
    public class InventoryController
    {
        private readonly InventoryRepository _inventoryRepository;

        public InventoryController() : this(new InventoryRepository())
        {
        }

        public InventoryController(InventoryRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<int> GetOutOfStockCount()
        {
            SessionUtil.Instance.RequireLogin();
            return await _inventoryRepository.GetOutOfStockCountAsync();
        }

        public async Task<int> GetLowStockCount()
        {
            SessionUtil.Instance.RequireLogin();
            return await _inventoryRepository.GetLowStockCountAsync();
        }

        public async Task<List<InventoryItemModel>> SearchInventoryAsync(string searchTerm, string searchFilter)
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            if (string.IsNullOrEmpty(searchTerm))
            {
                throw new Exception("Please enter a query to search the inventory");
            }

            if (string.IsNullOrEmpty(searchFilter))
            {
                throw new Exception("Please select either to search by 'Product Name' or 'SKU'");
            }

            return await _inventoryRepository.SearchInventoryAsync(searchTerm, searchFilter);
        }

        public async Task LogTransactionAsync(InventoryLogTransactionModel logTransaction)
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            if (logTransaction.VariantID <= 0)
            {
                throw new Exception("Invalid Inventory Item ID");
            }

            if (string.IsNullOrEmpty(logTransaction.Reason))
            {
                throw new Exception("Please select a reason for the stock adjustment");
            }

            if (string.IsNullOrEmpty(logTransaction.Notes))
            {
                throw new Exception("Please enter some notes for this stock adjustment");
            }

            if (logTransaction.QuantityChange <= 0)
            {
                throw new Exception("Quantity change must be greater than zero");
            }
            ;

            await _inventoryRepository.LogTransactionAsync(logTransaction);
        }

        public async Task<List<InventoryItemModel>> GetAllAsync()
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            return await _inventoryRepository.GetAllAsync();
        }

        public async Task<List<InventoryLogModel>> GetLogsAsync(int inventoryItemID)
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            if (inventoryItemID <= 0)
            {
                throw new Exception("Invalid Inventory Item ID");
            }

            return await _inventoryRepository.GetLogsAsync(inventoryItemID);
        }

        public async Task<List<InventoryStockReportModel>> GetStockReportAsync()
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            return await _inventoryRepository.GetStockReportAsync();
        }

        public async Task<List<InventoryMovementReportModel>> GenerateMovementReportAsync(
            string reportPeriod,
            DateTime? customStartDate,
            DateTime? customEndDate)
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
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

            return await _inventoryRepository.GenerateMovementReportAsync(startDate, endDate);
        }

        public async Task<InventoryMovementKPIModel> GetMovementKPIsAsync(
            string reportPeriod,
            DateTime? customStartDate,
            DateTime? customEndDate)
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
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

            InventoryMovementKPIModel? movementKPI = await _inventoryRepository.GetMovementKPIsAsync(startDate, endDate);
            if (movementKPI == null)
            {
                throw new InvalidOperationException("An error occured when retrieving the inventory movement report KPIs"); ;
            }
            else
            {
                return movementKPI;
            }
        }

        public async Task<InventoryStockKPIModel> GetStockKPIsAsync()
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            InventoryStockKPIModel? stockKPI = await _inventoryRepository.GetStockKPIsAsync();
            if (stockKPI == null)
            {
                throw new InvalidOperationException("n error occured when retrieving the inventory stock report KPIs");
            }
            else
            {
                return stockKPI;
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
    }
}
