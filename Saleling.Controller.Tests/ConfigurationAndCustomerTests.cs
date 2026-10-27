using Saleling.Model;
using Saleling.Repository;
using Saleling.Util;

namespace Saleling.Controller.Tests
{
    public class ConfigurationUtilTests
    {
        private const string Variable = ConfigurationUtil.CONNECTION_STRING_ENVIRONMENT_VARIABLE;

        private static void WithEnvironmentVariable(string? value, Action assertions)
        {
            string? original = Environment.GetEnvironmentVariable(Variable);
            try
            {
                Environment.SetEnvironmentVariable(Variable, value);
                assertions();
            }
            finally
            {
                Environment.SetEnvironmentVariable(Variable, original);
            }
        }

        [Fact]
        public void GetConnectionString_PrefersTheEnvironmentVariable()
        {
            WithEnvironmentVariable("Server=example;Database=Other;", () =>
                Assert.Equal("Server=example;Database=Other;", ConfigurationUtil.GetConnectionString()));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetConnectionString_FallsBackToAppConfig(string? blank)
        {
            WithEnvironmentVariable(blank, () =>
                Assert.Contains("Initial Catalog=Saleling.DB", ConfigurationUtil.GetConnectionString()));
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
