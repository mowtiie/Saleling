using Saleling.Util;
using Microsoft.Data.SqlClient;

namespace Saleling.Controller.Tests
{
    /// <summary>
    /// Marks a read-only integration test that needs the published Saleling.DB database.
    /// The test is skipped, not failed, when the database cannot be reached.
    /// </summary>
    public sealed class LocalDbFactAttribute : FactAttribute
    {
        public LocalDbFactAttribute()
        {
            try
            {
                using SqlConnection connection = new SqlConnection(ConfigurationUtil.GetConnectionString() + ";Connect Timeout=3");
                connection.Open();
            }
            catch (Exception ex)
            {
                Skip = $"Saleling.DB is not reachable: {ex.Message}";
            }
        }
    }
}
