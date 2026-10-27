using Saleling.Model;
using Saleling.Util;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Saleling.Repository
{
    public class UserRepository
    {
        private readonly string connectionString;

        public UserRepository(string? connectionString = null)
        {
            this.connectionString = connectionString ?? ConfigurationUtil.GetConnectionString();
        }

        public async Task<List<UserModel>> GetAllCashierAsync()
        {
            List<UserModel> cashiers = new List<UserModel>();
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = "SELECT * FROM Users WHERE Role = 'Cashier'";
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            UserModel cashier = new UserModel
                            {
                                UserID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("UserID")),
                                FirstName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("FirstName")),
                                LastName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("LastName")),
                                Username = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Username")),
                                HashedPassword = sqlDataReader.GetString(sqlDataReader.GetOrdinal("HashedPassword")),
                                Role = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Role"))
                            };
                            cashiers.Add(cashier);
                        }
                    }
                }
            }
            return cashiers;
        }

        public async Task<UserModel?> GetByUsernameAsync(string username)
        {
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();

                string query = "SELECT * FROM Users WHERE Username = @Username";
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@Username", username);
                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await sqlDataReader.ReadAsync())
                        {
                            UserModel user = new UserModel
                            {
                                UserID = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("UserID")),
                                FirstName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("FirstName")),
                                LastName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("LastName")),
                                Username = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Username")),
                                HashedPassword = sqlDataReader.GetString(sqlDataReader.GetOrdinal("HashedPassword")),
                                Role = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Role"))
                            };
                            return user;
                        }
                    }
                }
            }
            return null;
        }
    }
}
