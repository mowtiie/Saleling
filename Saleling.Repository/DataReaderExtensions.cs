using Microsoft.Data.SqlClient;

namespace Saleling.Repository
{
    internal static class DataReaderExtensions
    {
        // The label the product forms and stored procedures already use for "no supplier".
        public const string NoSupplierLabel = "No Assigned Supplier";

        public static string GetSupplierName(this SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? NoSupplierLabel : reader.GetString(ordinal);
        }

        // For nullable text columns such as inventory notes.
        public static string GetStringOrEmpty(this SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }
    }
}
