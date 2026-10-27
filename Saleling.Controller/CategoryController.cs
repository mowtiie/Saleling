using Saleling.Repository;
using Saleling.Util;

namespace Saleling.Controller
{
    public class CategoryController
    {
        private readonly CategoryRepository _categoryRepository;

        public CategoryController()
        {
            _categoryRepository = new CategoryRepository();
        }

        public async Task<List<string>> GetAllNamesAsync()
        {
            SessionUtil.Instance.RequireLogin();
            return await _categoryRepository.GetAllNamesAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            SessionUtil.Instance.RequireLogin();
            return await _categoryRepository.GetTotalCountAsync();
        }
    }
}
