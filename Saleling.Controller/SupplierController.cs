using Saleling.Repository;
using Saleling.Util;

namespace Saleling.Controller
{
    public class SupplierController
    {
        private readonly SupplierRepository _supplierRepository;
        public SupplierController()
        {
            _supplierRepository = new SupplierRepository();
        }
        public async Task<List<string>> GetAllNamesAsync()
        {
            SessionUtil.Instance.RequireLogin();
            return await _supplierRepository.GetAllNamesAsync();
        }
    }
}
