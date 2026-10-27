using Saleling.Model.Product;
using Saleling.Util;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Saleling.Repository
{
    public class ProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string? connectionString = null)
        {
            _connectionString = connectionString ?? ConfigurationUtil.GetConnectionString();
        }

        public async Task<ProductVariantModel?> GetProductVariantByIDAsync(int variantID)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetProductVariantByID", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@VariantID", variantID);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await sqlDataReader.ReadAsync())
                        {
                            ProductVariantModel productVariant = new ProductVariantModel
                            {
                                VariantID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("VariantID")),
                                ProductID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ProductID")),
                                SKU = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SKU")),
                                Size = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Size")),
                                Color = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Color")),
                                CostPrice = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("CostPrice")),
                                SellingPrice = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("SellingPrice")),
                                StockQuantity = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("StockQuantity")),
                                ReorderLevel = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ReorderLevel")),
                                Status = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Status"))
                            };
                            return productVariant;
                        }
                    }
                }
            }
            return null;
        }

        public async Task EditProductVariantAsync(ProductVariantModel productVariant)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_EditProductVariant", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@VariantID", productVariant.VariantID);
                    sqlCommand.Parameters.AddWithValue("@ProductID", productVariant.ProductID);
                    sqlCommand.Parameters.AddWithValue("@SKU", productVariant.SKU);
                    sqlCommand.Parameters.AddWithValue("@Size", productVariant.Size);
                    sqlCommand.Parameters.AddWithValue("@Color", productVariant.Color);
                    sqlCommand.Parameters.AddWithValue("@CostPrice", productVariant.CostPrice);
                    sqlCommand.Parameters.AddWithValue("@SellingPrice", productVariant.SellingPrice);
                    sqlCommand.Parameters.AddWithValue("@ReorderLevel", productVariant.ReorderLevel);
                    sqlCommand.Parameters.AddWithValue("@Status", productVariant.Status);
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task EditProductAsync(ProductModel product)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_EditProduct", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("ProductID", product.ProductID);
                    sqlCommand.Parameters.AddWithValue("ProductName", product.ProductName);
                    sqlCommand.Parameters.AddWithValue("Category", product.Category);
                    sqlCommand.Parameters.AddWithValue("Supplier", product.Supplier);
                    sqlCommand.Parameters.AddWithValue("Status", product.Status);
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task AddProductVariantAsync(ProductVariantModel productVariant, int processedByUserID)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_AddProductVariant", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@ProductID", productVariant.ProductID);
                    sqlCommand.Parameters.AddWithValue("@SKU", productVariant.SKU);
                    sqlCommand.Parameters.AddWithValue("@Size", productVariant.Size);
                    sqlCommand.Parameters.AddWithValue("@Color", productVariant.Color);
                    sqlCommand.Parameters.AddWithValue("@CostPrice", productVariant.CostPrice);
                    sqlCommand.Parameters.AddWithValue("@SellingPrice", productVariant.SellingPrice);
                    sqlCommand.Parameters.AddWithValue("@InitialStock", productVariant.StockQuantity);
                    sqlCommand.Parameters.AddWithValue("@ReorderLevel", productVariant.ReorderLevel);
                    sqlCommand.Parameters.AddWithValue("@Status", productVariant.Status);
                    sqlCommand.Parameters.AddWithValue("@ProcessedByUserID", processedByUserID);
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task AddProductAsync(ProductModel product)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_AddProduct", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("ProductName", product.ProductName);
                    sqlCommand.Parameters.AddWithValue("Category", product.Category);
                    sqlCommand.Parameters.AddWithValue("Supplier", product.Supplier);
                    sqlCommand.Parameters.AddWithValue("Status", product.Status);
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<ProductModel?> GetProductByIDAsync(int productID)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetProductByID", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@ProductID", productID);
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await sqlDataReader.ReadAsync())
                        {
                            int supplierOrdinal = sqlDataReader.GetOrdinal("SupplierName");

                            ProductModel product = new ProductModel
                            {
                                ProductID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ProductID")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                Category = sqlDataReader.GetString(sqlDataReader.GetOrdinal("CategoryName")),
                                Supplier = sqlDataReader.IsDBNull(supplierOrdinal) ? null : sqlDataReader.GetString(supplierOrdinal),
                                Status = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Status"))
                            };
                            return product;
                        }
                    }
                }
            }
            return null;
        }

        public async Task<List<ProductModel>> GetProductsAsync()
        {
            List<ProductModel> products = new List<ProductModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetProducts", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            int supplierOrdinal = sqlDataReader.GetOrdinal("SupplierName");

                            ProductModel product = new ProductModel
                            {
                                ProductID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ProductID")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                Category = sqlDataReader.GetString(sqlDataReader.GetOrdinal("CategoryName")),
                                Supplier = sqlDataReader.IsDBNull(supplierOrdinal) ? null : sqlDataReader.GetString(supplierOrdinal),
                                Status = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Status"))
                            };
                            products.Add(product);
                        }
                    }
                }
            }
            return products;
        }

        public async Task<List<ProductVariantModel>> GetProductVariantsByProductIDAsync(int productID)
        {
            List<ProductVariantModel> productVariants = new List<ProductVariantModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetProductVariantsByProductID", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@ProductID", productID);
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            ProductVariantModel productVariant = new ProductVariantModel
                            {
                                VariantID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("VariantID")),
                                ProductID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ProductID")),
                                SKU = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SKU")),
                                Size = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Size")),
                                Color = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Color")),
                                CostPrice = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("CostPrice")),
                                SellingPrice = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("SellingPrice")),
                                StockQuantity = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("StockQuantity")),
                                ReorderLevel = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ReorderLevel")),
                                Status = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Status"))
                            };
                            productVariants.Add(productVariant);
                        }
                    }
                }
            }
            return productVariants;
        }

        public async Task<int> GetTotalCountAsync()
        {
            int totalCount = 0;
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = "SELECT COUNT(ProductID) FROM Products WHERE Status = 'Active'";
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    object? result = await sqlCommand.ExecuteScalarAsync();
                    totalCount = result is int count ? count : 0;
                }
            }
            return totalCount;
        }

        public async Task DeleteProductAsync(int productID)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_DeleteProduct", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@ProductID", productID);
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteProductVariantAsync(int productVariantID)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_DeleteProductVariant", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@VariantID", productVariantID);
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<ProductModel>> SearchProductsAsync(string searchTerm, string searchFilter)
        {
            List<ProductModel> products = new List<ProductModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_SearchProducts", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@SearchFilter", searchFilter);
                    sqlCommand.Parameters.AddWithValue("@SearchTerm", searchTerm);
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        int supplierOrdinal = sqlDataReader.GetOrdinal("SupplierName");

                        while (await sqlDataReader.ReadAsync())
                        {
                            ProductModel product = new ProductModel
                            {
                                ProductID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ProductID")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                Category = sqlDataReader.GetString(sqlDataReader.GetOrdinal("CategoryName")),
                                Supplier = sqlDataReader.IsDBNull(supplierOrdinal)
                                            ? null
                                            : sqlDataReader.GetString(supplierOrdinal),
                                Status = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Status"))
                            };
                            products.Add(product);
                        }
                    }
                }
            }
            return products;
        }

        public async Task<List<POSProductModel>> SearchSaleableProductsAsync(string searchTerm, string searchFilter)
        {
            List<POSProductModel> searchedSaleableProducts = new List<POSProductModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_SearchSaleableProducts", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    sqlCommand.Parameters.AddWithValue("@SearchTerm", searchTerm);
                    sqlCommand.Parameters.AddWithValue("@SearchFilter", searchFilter);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            POSProductModel sealableProduct = new POSProductModel
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
                            searchedSaleableProducts.Add(sealableProduct);
                        }
                    }
                }
            }
            return searchedSaleableProducts;
        }

        public async Task<List<POSProductModel>> GetSaleableProductsAsync()
        {
            List<POSProductModel> sealableProducts = new List<POSProductModel>();
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
                            POSProductModel sealableProduct = new POSProductModel
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
                            sealableProducts.Add(sealableProduct);
                        }
                    }
                }
            }

            return sealableProducts;
        }

        public async Task<List<ProductStockAlertModel>> GetStockAlertsAsync()
        {
            List<ProductStockAlertModel> stockAlerts = new List<ProductStockAlertModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetRecentStockAlerts", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            ProductStockAlertModel productStockAlert = new ProductStockAlertModel
                            {
                                SKU = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SKU")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                Size = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Size")),
                                Color = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Color")),
                                Category = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Category")),
                                Supplier = sqlDataReader.GetSupplierName("Supplier"),
                                Status = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Status"))
                            };
                            stockAlerts.Add(productStockAlert);
                        }
                    }
                }
            }
            return stockAlerts;
        }

        public async Task<List<ProductListingModel>> SearchProductListingAsync(string searchTerm, string searchFilter)
        {
            List<ProductListingModel> searchedProductListings = new List<ProductListingModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_SearchProductListing", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@SearchFilter", searchFilter);
                    sqlCommand.Parameters.AddWithValue("@SearchTerm", searchTerm);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            ProductListingModel productListing = new ProductListingModel
                            {
                                ProductID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ProductID")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                VariantID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("VariantID")),
                                SKU = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SKU")),
                                Size = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Size")),
                                Color = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Color")),
                                StockQuantity = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("StockQuantity")),
                                ReorderLevel = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ReorderLevel")),
                                SellingPrice = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("SellingPrice")),
                                Category = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Category")),
                                Supplier = sqlDataReader.GetSupplierName("Supplier")
                            };
                            searchedProductListings.Add(productListing);
                        }
                    }
                }
            }
            return searchedProductListings;
        }

        public async Task<List<ProductListingModel>> GetProductListingsAsync()
        {
            List<ProductListingModel> productListings = new List<ProductListingModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("sp_GetProductListing", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            ProductListingModel productListing = new ProductListingModel
                            {
                                ProductID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ProductID")),
                                ProductName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ProductName")),
                                VariantID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("VariantID")),
                                SKU = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SKU")),
                                Size = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Size")),
                                Color = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Color")),
                                StockQuantity = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("StockQuantity")),
                                ReorderLevel = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("ReorderLevel")),
                                SellingPrice = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("SellingPrice")),
                                Category = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Category")),
                                Supplier = sqlDataReader.GetSupplierName("Supplier")
                            };
                            productListings.Add(productListing);
                        }
                    }
                }
            }
            return productListings;
        }
    }
}
