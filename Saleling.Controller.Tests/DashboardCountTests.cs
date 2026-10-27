using Saleling.Util;
using Microsoft.Data.SqlClient;

namespace Saleling.Controller.Tests
{
    // Read-only checks against the published Saleling.DB database.
    public class DashboardCountTests
    {
        private static async Task<int> ScalarAsync(string query)
        {
            using SqlConnection connection = new SqlConnection(ConfigurationUtil.GetConnectionString());
            await connection.OpenAsync();
            using SqlCommand command = new SqlCommand(query, connection);
            return (int)(await command.ExecuteScalarAsync())!;
        }

        [LocalDbFact]
        public async Task ProductTotalCount_CountsActiveProducts()
        {
            TestData.LoginAsAdmin();
            int expected = await ScalarAsync("SELECT COUNT(*) FROM Products WHERE Status = 'Active'");

            int actual = await new ProductController().GetTotalCountAsync();

            Assert.Equal(expected, actual);
        }

        [LocalDbFact]
        public async Task ProductTotalCount_IsNotTheVariantCount()
        {
            TestData.LoginAsAdmin();
            int variants = await ScalarAsync("SELECT COUNT(*) FROM ProductVariants");
            int products = await ScalarAsync("SELECT COUNT(*) FROM Products");
            Assert.True(variants > products, "Seed data should have more variants than products for this check to mean anything.");

            int actual = await new ProductController().GetTotalCountAsync();

            Assert.NotEqual(variants, actual);
        }
    }
}
