using Saleling.Model.Inventory;
using Saleling.Util;
using System.Data;
using System.Data.SqlClient;

namespace Saleling.Repository
{
    public class InventoryRepository
    {
        private readonly string _connectionString;

        public InventoryRepository(string? connectionString = null)
        {
            _connectionString = connectionString ?? ConfigurationUtil.GetConnectionString();
        }

        public async Task<int> GetLowStockCountAsync()
        {
            int lowStockCount = 0;
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = "SELECT COUNT(VariantID) FROM ProductVariants WHERE StockQuantity <= ReorderLevel AND Status = 'Active'";
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    object? result = await sqlCommand.ExecuteScalarAsync();
                    lowStockCount = result is int count ? count : 0;
                }
            }
            return lowStockCount;
        }

        public async Task<int> GetOutOfStockCountAsync()
        {
            int outOfStockCount = 0;
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = "SELECT COUNT(VariantID) FROM ProductVariants WHERE StockQuantity = 0;";
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    object? result = await sqlCommand.ExecuteScalarAsync();
                    outOfStockCount = result is int count ? count : 0;
                }
            }
            return outOfStockCount;
        }

        public async Task<List<InventoryItemModel>> SearchInventoryAsync(string searchTerm, string searchFilter)
        {
            List<InventoryItemModel> searchedInventory = new List<InventoryItemModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_SearchInventory", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@SearchTerm", searchTerm);
                    sqlCommand.Parameters.AddWithValue("@SearchFilter", searchFilter);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            InventoryItemModel inventoryItem = new InventoryItemModel
                            {
                                ProductID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ProductID")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                ProductStatus = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductStatus")),
                                Category = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Category")),
                                Supplier = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Supplier")),
                                VariantID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("VariantID")),
                                SKU = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SKU")),
                                Size = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Size")),
                                Color = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Color")),
                                StockQuantity = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("StockQuantity")),
                                ReorderLevel = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ReorderLevel")),
                                VariantStatus = sqlDataReader.GetString(sqlDataReader.GetOrdinal("VariantStatus"))
                            };
                            searchedInventory.Add(inventoryItem);
                        }
                    }
                }
            }
            return searchedInventory;
        }

        public async Task LogTransactionAsync(InventoryLogTransactionModel logTransactionModel)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_InventoryLogTransaction", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@VariantID", logTransactionModel.VariantID);
                    sqlCommand.Parameters.AddWithValue("@QuantityChange", logTransactionModel.QuantityChange);
                    sqlCommand.Parameters.AddWithValue("@Reason", logTransactionModel.Reason);
                    sqlCommand.Parameters.AddWithValue("@ProcessedByUserID", logTransactionModel.ProcessedByUserID);
                    sqlCommand.Parameters.AddWithValue("@Notes", logTransactionModel.Notes);
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
            ;
        }

        public async Task<List<InventoryLogModel>> GetLogsAsync(int inventoryItemID)
        {
            List<InventoryLogModel> inventoryLogs = new List<InventoryLogModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetInventoryLogs", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@VariantID", inventoryItemID);
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            InventoryLogModel inventoryLog = new InventoryLogModel
                            {
                                InventoryID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("InventoryID")),
                                VariantID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("VariantID")),
                                QuantityChange = Math.Abs(sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("QuantityChange"))),
                                ChangeDate = sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal("ChangeDate")),
                                Reason = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Reason")),
                                TransactionReferenceID = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal("TransactionReferenceID")) ? null : sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("TransactionReferenceID")),
                                Notes = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Notes")),
                                Processor = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Processor")),
                                Role = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Role"))
                            };
                            inventoryLogs.Add(inventoryLog);
                        }
                    }
                }
            }
            return inventoryLogs;
        }

        public async Task<List<InventoryItemModel>> GetAllAsync()
        {
            List<InventoryItemModel> inventoryDetails = new List<InventoryItemModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetInventoryItems", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            InventoryItemModel inventoryDetail = new InventoryItemModel
                            {
                                ProductID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ProductID")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                ProductStatus = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductStatus")),
                                Category = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Category")),
                                Supplier = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Supplier")),
                                VariantID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("VariantID")),
                                SKU = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SKU")),
                                Size = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Size")),
                                Color = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Color")),
                                StockQuantity = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("StockQuantity")),
                                ReorderLevel = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ReorderLevel")),
                                VariantStatus = sqlDataReader.GetString(sqlDataReader.GetOrdinal("VariantStatus"))
                            };
                            inventoryDetails.Add(inventoryDetail);
                        }
                    }
                }
            }
            return inventoryDetails;
        }

        public async Task<List<InventoryMovementReportModel>> GenerateMovementReportAsync(DateTime? startDate, DateTime? endDate)
        {
            List<InventoryMovementReportModel> movementReport = new List<InventoryMovementReportModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GenerateInventoryMovementReport", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@StartDate", startDate);
                    sqlCommand.Parameters.AddWithValue("@EndDate", endDate);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        int transactionReferenceIDOrdinal = sqlDataReader.GetOrdinal("TransactionReferenceID");

                        while (await sqlDataReader.ReadAsync())
                        {
                            InventoryMovementReportModel movementReportItem = new InventoryMovementReportModel
                            {
                                ChangeDate = sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal("ChangeDate")),
                                SKU = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SKU")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                Category = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Category")),
                                VariantDetails = sqlDataReader.GetString(sqlDataReader.GetOrdinal("VariantDetails")),
                                Reason = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Reason")),
                                QuantityChange = Math.Abs(sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("QuantityChange"))),
                                ProcessedBy = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProcessedBy")),
                                TransactionReferenceID = sqlDataReader.IsDBNull(transactionReferenceIDOrdinal)
                                                        ? null
                                                        : sqlDataReader.GetInt32(transactionReferenceIDOrdinal),
                                Notes = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Notes"))
                            };
                            movementReport.Add(movementReportItem);
                        }
                    }
                }
            }
            return movementReport;
        }

        public async Task<InventoryMovementKPIModel?> GetMovementKPIsAsync(DateTime? startDate, DateTime? endDate)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetInventoryMovementKPIs", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@StartDate", startDate);
                    sqlCommand.Parameters.AddWithValue("@EndDate", endDate);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await sqlDataReader.ReadAsync())
                        {
                            InventoryMovementKPIModel movementKPI = new InventoryMovementKPIModel
                            {
                                TotalUnitsSold = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("TotalUnitsSold")),
                                TotalUnitsReceived = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("TotalUnitsReceived")),
                                NetInventoryChange = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("NetInventoryChange")),
                                TotalAdjustments = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("TotalAdjustments"))
                            };
                            return movementKPI;
                        }
                    }
                }
            }
            return null;
        }

        public async Task<InventoryStockKPIModel?> GetStockKPIsAsync()
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetInventoryStockKPIs", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await sqlDataReader.ReadAsync())
                        {
                            InventoryStockKPIModel stockKPI = new InventoryStockKPIModel
                            {
                                TotalInventoryValue = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("TotalInventoryValue")),
                                TotalSKUsInStock = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("TotalSKUsInStock")),
                                TotalSKUsBelowReorderLevel = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("TotalSKUsBelowReorderLevel")),
                                LowStockRate = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("LowStockRate"))
                            };
                            return stockKPI;
                        }
                    }
                }
            }
            return null;
        }

        public async Task<List<InventoryStockReportModel>> GetStockReportAsync()
        {
            List<InventoryStockReportModel> stockReport = new List<InventoryStockReportModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GenerateInventoryStockReport", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            InventoryStockReportModel stockReportItem = new InventoryStockReportModel
                            {
                                SKU = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SKU")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                CategoryName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("CategoryName")),
                                VariantDetails = sqlDataReader.GetString(sqlDataReader.GetOrdinal("VariantDetails")),
                                StockQuantity = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("StockQuantity")),
                                ReorderLevel = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ReorderLevel")),
                                Status = sqlDataReader.GetString(sqlDataReader.GetOrdinal("StockStatus"))
                            };
                            stockReport.Add(stockReportItem);
                        }
                    }
                }
                return stockReport;
            }
        }
    }
}
