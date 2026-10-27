using Saleling.Model;
using Saleling.Repository;
using Saleling.Util;

namespace Saleling.Controller.Tests
{
    public class ConfigurationUtilTests
    {
        private const string ConnectionStringVariable = ConfigurationUtil.CONNECTION_STRING_ENVIRONMENT_VARIABLE;
        private const string ProfileVariable = ConfigurationUtil.PROFILE_ENVIRONMENT_VARIABLE;

        private static void WithEnvironment(string? connectionString, string? profile, Action assertions)
        {
            string? originalConnectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
            string? originalProfile = Environment.GetEnvironmentVariable(ProfileVariable);
            try
            {
                Environment.SetEnvironmentVariable(ConnectionStringVariable, connectionString);
                Environment.SetEnvironmentVariable(ProfileVariable, profile);
                assertions();
            }
            finally
            {
                Environment.SetEnvironmentVariable(ConnectionStringVariable, originalConnectionString);
                Environment.SetEnvironmentVariable(ProfileVariable, originalProfile);
            }
        }

        [Fact]
        public void GetConnectionString_PrefersTheEnvironmentVariable()
        {
            WithEnvironment("Server=example;Database=Other;", "SqlExpress", () =>
                Assert.Equal("Server=example;Database=Other;", ConfigurationUtil.GetConnectionString()));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetConnectionString_FallsBackToTheAppConfigProfile(string? blank)
        {
            WithEnvironment(blank, blank, () =>
                Assert.Contains(@"(localdb)\MSSQLLocalDB", ConfigurationUtil.GetConnectionString()));
        }

        [Fact]
        public void GetConnectionString_ProfileVariableSelectsAnAppConfigEntry()
        {
            WithEnvironment(null, "SqlExpress", () =>
            {
                string connectionString = ConfigurationUtil.GetConnectionString();
                Assert.Contains(@".\SQLEXPRESS", connectionString);
                Assert.Contains("Initial Catalog=Saleling.DB", connectionString);
            });
        }

        [Fact]
        public void GetConnectionString_UnknownProfile_ListsTheAvailableOnes()
        {
            WithEnvironment(null, "Nope", () =>
            {
                Exception ex = Assert.ThrowsAny<Exception>(() => ConfigurationUtil.GetConnectionString());
                Assert.Contains("Nope", ex.Message);
                Assert.Contains("LocalDb", ex.Message);
                Assert.Contains("SqlExpress", ex.Message);
            });
        }
    }

    public class CustomerControllerIntegrationTests : IClassFixture<ScratchDatabase>
    {
        private readonly ScratchDatabase _db;

        public CustomerControllerIntegrationTests(ScratchDatabase db)
        {
            _db = db;
        }

        private CustomerController Controller() => new CustomerController(new CustomerRepository(_db.ConnectionString));

        [ScratchDbFact]
        public async Task WalkInCustomer_IsFoundByNameNotByID()
        {
            await _db.ResetAsync();
            await _db.SeedCustomerAsync("Maria", "Santos");
            int walkInId = await _db.SeedCustomerAsync();
            TestData.LoginAsCashier();

            CustomerModel customer = await Controller().GetWalkInCustomerAsync();

            Assert.Equal(walkInId, customer.CustomerID);
            Assert.NotEqual(1, customer.CustomerID);
            Assert.Equal("Walk-In", customer.FirstName);
        }

        [ScratchDbFact]
        public async Task WalkInCustomer_MissingRecord_Throws()
        {
            await _db.ResetAsync();
            await _db.SeedCustomerAsync("Maria", "Santos");
            TestData.LoginAsCashier();

            await Assert.ThrowsAsync<InvalidOperationException>(() => Controller().GetWalkInCustomerAsync());
        }
    }
}
