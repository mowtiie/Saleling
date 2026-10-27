using Saleling.Util;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Saleling.Controller.Tests
{
    /// <summary>
    /// A throwaway database built from the Saleling.DB project's scripts on the same LocalDB
    /// server the app uses. Created once per test class and dropped afterwards; it never
    /// touches the real Saleling.DB database.
    /// </summary>
    public sealed class ScratchDatabase : IAsyncLifetime
    {
        // Tables first (in foreign key order), then the table type, then the procedures that use them.
        private static readonly string[] TableOrder =
        {
            "Categories", "Suppliers", "Users", "Customers", "Products",
            "ProductVariants", "Sales", "SalesItems", "Inventory"
        };

        // Children before parents.
        private static readonly string[] ClearOrder =
        {
            "Inventory", "SalesItems", "Sales", "ProductVariants", "Products",
            "Customers", "Users", "Suppliers", "Categories"
        };

        private static readonly Lazy<string?> ServerUnavailableReason = new Lazy<string?>(CheckServer);

        private string _databaseName = string.Empty;
        private int _productId;

        public string ConnectionString { get; private set; } = string.Empty;

        public static string? SkipReason => ServerUnavailableReason.Value;

        private static string MasterConnectionString()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConfigurationUtil.GetConnectionString())
            {
                InitialCatalog = "master",
                ConnectTimeout = 3
            };
            return builder.ConnectionString;
        }

        private static string? CheckServer()
        {
            try
            {
                using SqlConnection connection = new SqlConnection(MasterConnectionString());
                connection.Open();
                return null;
            }
            catch (Exception ex)
            {
                return $"LocalDB is not reachable: {ex.Message}";
            }
        }

        public async Task InitializeAsync()
        {
            if (SkipReason != null)
            {
                return;
            }

            _databaseName = $"Saleling_Test_{Guid.NewGuid():N}";

            using (SqlConnection master = new SqlConnection(MasterConnectionString()))
            {
                await master.OpenAsync();
                using SqlCommand create = new SqlCommand($"CREATE DATABASE [{_databaseName}]", master);
                await create.ExecuteNonQueryAsync();
            }

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(MasterConnectionString())
            {
                InitialCatalog = _databaseName
            };
            ConnectionString = builder.ConnectionString;

            string scripts = Path.Combine(AppContext.BaseDirectory, "DbScripts");
            foreach (string table in TableOrder)
            {
                await RunScriptAsync(Path.Combine(scripts, "Tables", $"{table}.sql"));
            }

            foreach (string file in Directory.GetFiles(Path.Combine(scripts, "Types"), "*.sql"))
            {
                await RunScriptAsync(file);
            }

            foreach (string file in Directory.GetFiles(Path.Combine(scripts, "StoredProcedures"), "*.sql"))
            {
                await RunScriptAsync(file);
            }
        }

        public async Task DisposeAsync()
        {
            if (SkipReason != null || _databaseName.Length == 0)
            {
                return;
            }

            SqlConnection.ClearAllPools();
            using SqlConnection master = new SqlConnection(MasterConnectionString());
            await master.OpenAsync();
            using SqlCommand drop = new SqlCommand(
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}];",
                master);
            await drop.ExecuteNonQueryAsync();
        }

        private async Task RunScriptAsync(string path)
        {
            string script = await File.ReadAllTextAsync(path);
            foreach (string batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                if (batch.Trim().Length == 0)
                {
                    continue;
                }

                await ExecuteAsync(batch);
            }
        }

        public async Task ResetAsync()
        {
            foreach (string table in ClearOrder)
            {
                await ExecuteAsync($"DELETE FROM {table}");
            }

            _productId = 0;
        }

        public async Task<int> ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
        {
            using SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            using SqlCommand command = new SqlCommand(sql, connection);
            foreach ((string name, object value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }
            return await command.ExecuteNonQueryAsync();
        }

        public async Task<T> ScalarAsync<T>(string sql, params (string Name, object Value)[] parameters)
        {
            using SqlConnection connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            using SqlCommand command = new SqlCommand(sql, connection);
            foreach ((string name, object value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }
            return (T)Convert.ChangeType((await command.ExecuteScalarAsync())!, typeof(T));
        }

        public Task<int> SeedUserAsync(string role = "Cashier", string username = "cashier1", string password = "password1")
        {
            return ScalarAsync<int>(
                "INSERT INTO Users (FirstName, LastName, Username, HashedPassword, Role) " +
                "OUTPUT INSERTED.UserID VALUES ('Test', 'User', @Username, @Hash, @Role)",
                ("@Username", username), ("@Hash", SecurityUtil.HashPassword(password)), ("@Role", role));
        }

        public Task<int> SeedCustomerAsync(string firstName = "Walk-In", string lastName = "Customer", string? phone = null)
        {
            return ScalarAsync<int>(
                "INSERT INTO Customers (FirstName, LastName, Phone, Email) OUTPUT INSERTED.CustomerID VALUES (@First, @Last, @Phone, @Email)",
                ("@First", firstName), ("@Last", lastName), ("@Phone", (object?)phone ?? DBNull.Value), ("@Email", phone == null ? DBNull.Value : $"{phone}@example.test"));
        }

        public async Task<int> SeedVariantAsync(decimal sellingPrice = 100m, int stock = 10, string status = "Active")
        {
            if (_productId == 0)
            {
                int categoryId = await ScalarAsync<int>(
                    "INSERT INTO Categories (CategoryName) OUTPUT INSERTED.CategoryID VALUES ('Tops')");
                int supplierId = await ScalarAsync<int>(
                    "INSERT INTO Suppliers (SupplierName) OUTPUT INSERTED.SupplierID VALUES ('Supplier')");
                _productId = await ScalarAsync<int>(
                    "INSERT INTO Products (ProductName, CategoryID, SupplierID) OUTPUT INSERTED.ProductID " +
                    "VALUES ('Test Tee', @CategoryID, @SupplierID)",
                    ("@CategoryID", categoryId), ("@SupplierID", supplierId));
            }

            return await ScalarAsync<int>(
                "INSERT INTO ProductVariants (ProductID, SKU, Size, Color, CostPrice, SellingPrice, StockQuantity, Status) " +
                "OUTPUT INSERTED.VariantID VALUES (@ProductID, @SKU, 'M', 'Black', 10, @Price, @Stock, @Status)",
                ("@ProductID", _productId), ("@SKU", $"SKU-{Guid.NewGuid():N}"),
                ("@Price", sellingPrice), ("@Stock", stock), ("@Status", status));
        }

        public Task<int> AddCategoryAsync(string name)
        {
            return ScalarAsync<int>(
                "INSERT INTO Categories (CategoryName) OUTPUT INSERTED.CategoryID VALUES (@Name)", ("@Name", name));
        }

        public Task<int> AddSupplierAsync(string name)
        {
            return ScalarAsync<int>(
                "INSERT INTO Suppliers (SupplierName) OUTPUT INSERTED.SupplierID VALUES (@Name)", ("@Name", name));
        }

        public Task<int> AddProductAsync(string name, int categoryId, int? supplierId = null, string status = "Active")
        {
            return ScalarAsync<int>(
                "INSERT INTO Products (ProductName, CategoryID, SupplierID, Status) OUTPUT INSERTED.ProductID " +
                "VALUES (@Name, @CategoryID, @SupplierID, @Status)",
                ("@Name", name), ("@CategoryID", categoryId),
                ("@SupplierID", supplierId.HasValue ? supplierId.Value : DBNull.Value), ("@Status", status));
        }

        public Task<int> AddVariantAsync(
            int productId, string sku, decimal costPrice = 10m, decimal sellingPrice = 20m,
            int stock = 10, int reorderLevel = 5, string status = "Active")
        {
            return ScalarAsync<int>(
                "INSERT INTO ProductVariants (ProductID, SKU, Size, Color, CostPrice, SellingPrice, StockQuantity, ReorderLevel, Status) " +
                "OUTPUT INSERTED.VariantID VALUES (@ProductID, @SKU, 'M', 'Black', @Cost, @Price, @Stock, @Reorder, @Status)",
                ("@ProductID", productId), ("@SKU", sku), ("@Cost", costPrice), ("@Price", sellingPrice),
                ("@Stock", stock), ("@Reorder", reorderLevel), ("@Status", status));
        }

        public Task<int> StockAsync(int variantId)
        {
            return ScalarAsync<int>("SELECT StockQuantity FROM ProductVariants WHERE VariantID = @Id", ("@Id", variantId));
        }

        public Task<int> CountAsync(string table)
        {
            return ScalarAsync<int>($"SELECT COUNT(*) FROM {table}");
        }
    }

    /// <summary>
    /// A test that needs the scratch database. It is skipped, not failed, when LocalDB is unavailable.
    /// </summary>
    public sealed class ScratchDbFactAttribute : FactAttribute
    {
        public ScratchDbFactAttribute()
        {
            Skip = ScratchDatabase.SkipReason;
        }
    }
}
