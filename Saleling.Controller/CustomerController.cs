using Saleling.Model;
using Saleling.Repository;
using Saleling.Util;

namespace Saleling.Controller
{
    public class CustomerController
    {
        private readonly CustomerRepository _customerRepository;

        public CustomerController()
        {
            _customerRepository = new CustomerRepository();
        }

        public async Task<CustomerModel> GetWalkInCustomerAsync()
        {
            SessionUtil.Instance.RequireLogin();
            CustomerModel? walkInCustomer = await _customerRepository.GetWalkInCustomerAsync();
            if (walkInCustomer == null)
            {
                throw new InvalidOperationException("Walk-in customer record not found in the database.");
            }

            return walkInCustomer;
        }
    }
}
