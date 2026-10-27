using Saleling.Model;
using Saleling.Util;
using Microsoft.Data.SqlClient;

namespace Saleling.Repository
{
    public class CustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(string? connectionString = null)
        {
            _connectionString = connectionString ?? ConfigurationUtil.GetConnectionString();
        }

        public async Task<CustomerModel?> GetWalkInCustomerAsync()
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                // The seed script identifies the walk-in customer by name, so do the same instead of assuming ID 1.
                string query = "SELECT TOP 1 CustomerID, FirstName, LastName, Phone, Email, LoyaltyPoints FROM Customers WHERE FirstName = 'Walk-In' AND LastName = 'Customer' ORDER BY CustomerID";
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await sqlDataReader.ReadAsync())
                        {
                            CustomerModel walkInCustomer = new CustomerModel
                            {
                                CustomerID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("CustomerID")),
                                FirstName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("FirstName")),
                                LastName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("LastName")),
                                Phone = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal("Phone")) ? null : sqlDataReader.GetString(sqlDataReader.GetOrdinal("Phone")),
                                Email = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal("Email")) ? null : sqlDataReader.GetString(sqlDataReader.GetOrdinal("Email")),
                                LoyaltyPoints = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal("LoyaltyPoints")) ? null : sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("LoyaltyPoints"))
                            };
                            return walkInCustomer;
                        }
                    }
                }
            }
            return null;
        }
    }
}
