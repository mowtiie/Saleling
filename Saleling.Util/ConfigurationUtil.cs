using System.Configuration;

namespace Saleling.Util
{
    public class ConfigurationUtil
    {
        public const string CONNECTION_STRING_ENVIRONMENT_VARIABLE = "SALELING_CONNECTION_STRING";

        private static readonly string CONNECTION_STRING_NAME = "SalelingDB";

        // The environment variable wins over App.config so a deployment can point at its own
        // database without editing files.
        public static string GetConnectionString()
        {
            string? fromEnvironment = Environment.GetEnvironmentVariable(CONNECTION_STRING_ENVIRONMENT_VARIABLE);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return fromEnvironment;
            }

            ConnectionStringSettings? fromConfig = ConfigurationManager.ConnectionStrings[CONNECTION_STRING_NAME];
            if (fromConfig == null)
            {
                throw new ConfigurationErrorsException(
                    $"No connection string found. Add a '{CONNECTION_STRING_NAME}' entry to App.config " +
                    $"or set the {CONNECTION_STRING_ENVIRONMENT_VARIABLE} environment variable.");
            }

            return fromConfig.ConnectionString;
        }
    }
}
