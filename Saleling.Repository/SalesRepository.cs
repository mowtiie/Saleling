using Saleling.Model.Product;
using Saleling.Model.Sale;
using Saleling.Util;
using System.Data;
using System.Data.SqlClient;

namespace Saleling.Repository
{
    public class SalesRepository
    {
        private readonly string _connectionString;

        public SalesRepository(string? connectionString = null)
        {
            _connectionString = connectionString ?? ConfigurationUtil.GetConnectionString();
        }

        private DataTable ConvertItemsToDataTable(List<SalesItemModel> items)
        {
            DataTable dataTable = new DataTable("SaleItemType");
            dataTable.Columns.Add("VariantID", typeof(int));
            dataTable.Columns.Add("Quantity", typeof(int));
            dataTable.Columns.Add("UnitPrice", typeof(decimal));
            dataTable.Columns.Add("Discount", typeof(decimal));
            dataTable.Columns.Add("SubTotal", typeof(decimal));

            foreach (SalesItemModel item in items)
            {
                dataTable.Rows.Add(
                    item.VariantID,
                    item.Quantity,
                    item.UnitPrice,
                    item.Discount,
                    item.SubTotal
                );
            }

            return dataTable;
        }

        public async Task<List<POSProductModel>> GetAvailableProductsAsync()
        {
            List<POSProductModel> availableProducts = new List<POSProductModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetSaleableProducts", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            POSProductModel product = new POSProductModel
                            {
                                VariantID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("VariantID")),
                                ProductID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ProductID")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                SKU = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SKU")),
                                Size = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Size")),
                                Color = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Color")),
                                SellingPrice = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("SellingPrice")),
                                StockQuantity = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("StockQuantity")),
                                Category = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Category"))
                            };
                            availableProducts.Add(product);
                        }
                    }
                }
            }
            return availableProducts;
        }

        public async Task<int> ProcessSaleAsync(SalesTransactionModel transaction)
        {
            int newSaleID = -1;
            DataTable saleItemsTvP = ConvertItemsToDataTable(transaction.Items);

            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                using (SqlCommand sqlCommand = new SqlCommand("sp_ProcessSaleTransaction", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    sqlCommand.CommandTimeout = 30;

                    sqlCommand.Parameters.AddWithValue("@CustomerID", transaction.CustomerID);
                    sqlCommand.Parameters.AddWithValue("@ProcessedByUserID", transaction.ProcessedByUserID);
                    sqlCommand.Parameters.AddWithValue("@TotalAmount", transaction.TotalAmount);

                    SqlParameter tvpParam = sqlCommand.Parameters.AddWithValue("@SaleItems", saleItemsTvP);
                    tvpParam.SqlDbType = SqlDbType.Structured;
                    tvpParam.TypeName = "udt_SaleItemType";

                    object? result = await sqlCommand.ExecuteScalarAsync();

                    if (result != null && result != DBNull.Value)
                    {
                        newSaleID = Convert.ToInt32(result);
                    }
                    else
                    {
                        throw new InvalidOperationException("Sale processing failed or returned no Sale ID.");
                    }
                }
            }

            return newSaleID;
        }

        public async Task<SalesReportKPIModel?> GetSaleReportsKPIAsync(DateTime? startDate, DateTime? endDate, int? processedByUserID = null)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetSaleReportsKPIs", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@StartDate", startDate);
                    sqlCommand.Parameters.AddWithValue("@EndDate", endDate);
                    sqlCommand.Parameters.AddWithValue("@ProcessedByUserID", processedByUserID);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await sqlDataReader.ReadAsync())
                        {
                            SalesReportKPIModel saleReportKPI = new SalesReportKPIModel
                            {
                                TotalSalesRevenue = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("TotalSalesRevenue")),
                                TransactionCount = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("TransactionCount")),
                                ItemsSoldCount = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ItemsSoldCount")),
                                AverageSaleValue = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("AverageSaleValue"))
                            };
                            return saleReportKPI;
                        }
                    }
                }
                return null;
            }
        }

        public async Task<List<SalesReportItemModel>> GetSalesReportAsync(DateTime? startDate, DateTime? endDate, int? processedByUserID = null)
        {
            List<SalesReportItemModel> salesReport = new List<SalesReportItemModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GenerateSalesReport", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@StartDate", startDate);
                    sqlCommand.Parameters.AddWithValue("@EndDate", endDate);
                    sqlCommand.Parameters.AddWithValue("@ProcessedByUserID", processedByUserID);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            SalesReportItemModel salesReportItem = new SalesReportItemModel
                            {
                                SaleID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("SaleID")),
                                SaleDate = sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal("SaleDate")),
                                ProcessedBy = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProcessedBy")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                VariantDetails = sqlDataReader.GetString(sqlDataReader.GetOrdinal("VariantDetails")),
                                Category = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Category")),
                                SKU = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SKU")),
                                Quantity = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("Quantity")),
                                UnitPrice = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("UnitPrice")),
                                Discount = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("Discount")),
                                Revenue = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("Revenue")),
                                COGS = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("COGS")),
                                GrossProfit = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("GrossProfit")),
                                TotalAmount = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("TotalAmount"))
                            };
                            salesReport.Add(salesReportItem);
                        }
                    }
                }
            }
            return salesReport;
        }

        public async Task<decimal> GetTotalSalesTodayByUserIDAsync(int userId)
        {
            decimal totalSalesToday = 0M;
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = @"SELECT SUM(TotalAmount) FROM Sales 
                                WHERE 
                                    ProcessedByUserID = @UserID 
                                    AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE);";

                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@UserID", userId);
                    object? result = await sqlCommand.ExecuteScalarAsync();
                    totalSalesToday = result is decimal value ? value : 0M;
                }
            }
            return totalSalesToday;
        }

        public async Task<decimal> GetAvgItemsPerTransactionTodayByUserIDAsync(int userId)
        {
            decimal avgItems = 0M;

            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = @"
                        SELECT
                            CASE 
                                WHEN COUNT(DISTINCT S.SaleID) = 0 THEN 0.00
                                ELSE CAST(SUM(SI.Quantity) AS DECIMAL(10, 2)) / COUNT(DISTINCT S.SaleID)
                            END
                        FROM
                            Sales S
                        JOIN
                            SalesItems SI ON S.SaleID = SI.SaleID
                        WHERE
                            S.ProcessedByUserID = @UserID
                            AND CAST(S.SaleDate AS DATE) = CAST(GETDATE() AS DATE);";

                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@UserID", userId);
                    object? result = await sqlCommand.ExecuteScalarAsync();
                    avgItems = result is decimal avg ? avg : 0M;
                }
            }
            return avgItems;
        }

        public async Task<int> GetTotalItemsSoldTodayByUserIDAsync(int userId)
        {
            int totalItemsSold = 0;
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = @"SELECT SUM(SI.Quantity) FROM SalesItems SI
                            JOIN
                                Sales S ON SI.SaleID = S.SaleID
                            WHERE 
                                S.ProcessedByUserID = @UserID 
                                AND CAST(S.SaleDate AS DATE) = CAST(GETDATE() AS DATE);";

                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@UserID", userId);
                    object? result = await sqlCommand.ExecuteScalarAsync();
                    totalItemsSold = result is int count ? count : 0;
                }
            }
            return totalItemsSold;
        }

        public async Task<int> GetTransactionCountTodayByUserIDAsync(int userID)
        {
            int transactionCount = 0;
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = @"SELECT COUNT(SaleID) FROM Sales 
                                WHERE 
                                    ProcessedByUserID = @UserID 
                                    AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE);";

                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@UserID", userID);
                    object? result = await sqlCommand.ExecuteScalarAsync();
                    transactionCount = result is int value ? value : 0;
                }
            }
            return transactionCount;
        }

        public async Task<decimal> GetTotalSalesAsync()
        {
            decimal totalSales = 0M;
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = "SELECT SUM(TotalAmount) FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE);";
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    object? result = await sqlCommand.ExecuteScalarAsync();
                    totalSales = result is decimal ? (decimal)result : 0;
                }
            }
            return totalSales;
        }

        public async Task<int> GetTransactionCountAsync()
        {
            int transactionCount = 0;
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = "SELECT COUNT(SaleID) FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE);";
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    object? result = await sqlCommand.ExecuteScalarAsync();
                    transactionCount = result is int ? (int)result : 0;
                }
            }
            return transactionCount;
        }

        public async Task<List<SalesAlertModel>> GetRecentSalesAsync(int? processedByUserID = null)
        {
            List<SalesAlertModel> recentSales = new List<SalesAlertModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetRecentTransactions", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@ProcessedByUserID", processedByUserID);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            SalesAlertModel recentSale = new SalesAlertModel
                            {
                                SaleID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("SaleID")),
                                SaleDate = sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal("SaleDate")),
                                TotalAmount = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("TotalAmount")),
                                Cashier = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Cashier"))
                            };
                            recentSales.Add(recentSale);
                        }
                    }
                }
            }
            return recentSales;
        }
    }
}
