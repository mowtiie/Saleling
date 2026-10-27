using Saleling.Util;
using Microsoft.Data.SqlClient;

namespace Saleling.Repository
{
    public class SupplierRepository
    {
        private readonly string _connectionString;

        public SupplierRepository(string? connectionString = null)
        {
            _connectionString = connectionString ?? ConfigurationUtil.GetConnectionString();
        }

        public async Task<List<string>> GetAllNamesAsync()
        {
            List<string> supplierNames = new List<string>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = "SELECT SupplierName FROM Suppliers";
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            string supplierName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("SupplierName"));
                            supplierNames.Add(supplierName);
                        }
                    }
                }
            }
            return supplierNames;
        }
    }
}
