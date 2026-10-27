using System.Configuration;

namespace Saleling.Util
{
    public class ConfigurationUtil
    {
        public const string CONNECTION_STRING_ENVIRONMENT_VARIABLE = "SALELING_CONNECTION_STRING";
        public const string PROFILE_ENVIRONMENT_VARIABLE = "SALELING_DB_PROFILE";

        private const string PROFILE_SETTING_NAME = "DatabaseProfile";

        // Picks the connection string in this order, so a deployment or a developer can point at
        // their own database without editing files:
        //   1. SALELING_CONNECTION_STRING: a full connection string.
        //   2. SALELING_DB_PROFILE: the name of a connection string in App.config.
        //   3. The DatabaseProfile setting in App.config.
        public static string GetConnectionString()
        {
            string? fromEnvironment = Environment.GetEnvironmentVariable(CONNECTION_STRING_ENVIRONMENT_VARIABLE);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return fromEnvironment;
            }

            string? profile = Environment.GetEnvironmentVariable(PROFILE_ENVIRONMENT_VARIABLE);
            if (string.IsNullOrWhiteSpace(profile))
            {
                profile = ConfigurationManager.AppSettings[PROFILE_SETTING_NAME];
            }
            if (string.IsNullOrWhiteSpace(profile))
            {
                throw new ConfigurationErrorsException(
                    $"No database profile selected. Set '{PROFILE_SETTING_NAME}' in App.config " +
                    $"or set the {PROFILE_ENVIRONMENT_VARIABLE} environment variable.");
            }

            profile = profile.Trim();
            ConnectionStringSettings? fromConfig = ConfigurationManager.ConnectionStrings[profile];
            if (fromConfig == null)
            {
                string available = string.Join(", ", ConfigurationManager.ConnectionStrings
                    .Cast<ConnectionStringSettings>()
                    .Select(settings => settings.Name)
                    .Where(name => name != "LocalSqlServer"));
                throw new ConfigurationErrorsException(
                    $"Unknown database profile '{profile}'. Available profiles: {available}.");
            }

            return fromConfig.ConnectionString;
        }
    }
}
