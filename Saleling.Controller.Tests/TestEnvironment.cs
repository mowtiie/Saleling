using Saleling.Util;
using System.Runtime.CompilerServices;

namespace Saleling.Controller.Tests
{
    internal static class TestEnvironment
    {
        // The tests create and drop databases, so they must always use the LocalDB connection string in
        // this project's App.config, even if SALELING_CONNECTION_STRING is set on the machine.
        [ModuleInitializer]
        internal static void IgnoreConnectionStringOverride()
        {
            Environment.SetEnvironmentVariable(ConfigurationUtil.CONNECTION_STRING_ENVIRONMENT_VARIABLE, null);
        }
    }
}
