using Saleling.Util;
using System.Data.SqlClient;

namespace Saleling.Repository
{
    public class CategoryRepository
    {
        private readonly string _connectionString;

        public CategoryRepository(string? connectionString = null)
        {
            _connectionString = connectionString ?? ConfigurationUtil.GetConnectionString();
        }

        public async Task<int> GetTotalCountAsync()
        {
            int totalCount = 0;
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = "SELECT COUNT(CategoryID) FROM Categories";
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    object? result = await sqlCommand.ExecuteScalarAsync();
                    totalCount = result is int count ? count : 0;
                }
            }
            return totalCount;
        }

        public async Task<List<string>> GetAllNamesAsync()
        {
            List<string> categoryNames = new List<string>();
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = "SELECT CategoryName FROM Categories";
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            string categoryName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("CategoryName"));
                            categoryNames.Add(categoryName);
                        }
                    }
                }
            }

            return categoryNames;
        }
    }
}
