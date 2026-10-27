using Saleling.Model.Product;
using Saleling.Repository;
using Saleling.Util;

namespace Saleling.Controller
{
    public class ProductController
    {
        private readonly ProductRepository _productRepository;

        public ProductController() : this(new ProductRepository())
        {
        }

        public ProductController(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ProductVariantModel> GetProductVariantByIDAsync(int productVariantID)
        {
            SessionUtil.Instance.RequireLogin();
            if (productVariantID <= 0)
            {
                throw new Exception("Invalid Product Variant ID");
            }

            ProductVariantModel? productVariant = await _productRepository.GetProductVariantByIDAsync(productVariantID);
            if (productVariant == null)
            {
                throw new Exception("Product Variant not found");
            }

            return productVariant;
        }

        public async Task AddProductVariantAsync(ProductVariantModel productVariant, int processedByUserID)
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            if (productVariant.ProductID <= 0)
            {
                throw new Exception("Invalid parent product ID");
            }

            if (string.IsNullOrEmpty(productVariant.SKU))
            {
                throw new Exception("Please enter a SKU for the product variant");
            }

            if (productVariant.ReorderLevel < 0)
            {
                throw new Exception("Reorder level must not be less than zero");
            }

            if (productVariant.StockQuantity < 0)
            {
                throw new Exception("Stock quantity must not be less than zero");
            }

            if (productVariant.CostPrice < 0 || productVariant.SellingPrice < 0)
            {
                throw new Exception("Prices must not be less than zero");
            }

            await _productRepository.AddProductVariantAsync(productVariant, processedByUserID);
        }

        public async Task AddProductAsync(ProductModel product)
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            if (string.IsNullOrEmpty(product.ProductName))
            {
                throw new Exception("Please enter a name for the product");
            }

            await _productRepository.AddProductAsync(product);
        }

        public async Task EditProductVariantAsync(ProductVariantModel productVariant)
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            if (productVariant.VariantID <= 0)
            {
                throw new Exception("Invalid product variant ID");
            }

            if (productVariant.ProductID <= 0)
            {
                throw new Exception("Invalid parent product ID");
            }

            if (string.IsNullOrEmpty(productVariant.SKU))
            {
                throw new Exception("Please enter a SKU for the product variant");
            }

            if (productVariant.ReorderLevel < 0)
            {
                throw new Exception("Reorder level must not be less than zero");
            }

            if (productVariant.CostPrice < 0 || productVariant.SellingPrice < 0)
            {
                throw new Exception("Prices must not be less than zero");
            }

            await _productRepository.EditProductVariantAsync(productVariant);
        }

        public async Task EditProductAsync(ProductModel product)
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            if (string.IsNullOrEmpty(product.ProductName))
            {
                throw new Exception("Please enter a name for the product");
            }

            await _productRepository.EditProductAsync(product);
        }

        public async Task<int> GetTotalCountAsync()
        {
            SessionUtil.Instance.RequireLogin();
            return await _productRepository.GetTotalCountAsync();
        }

        public async Task<List<ProductModel>> GetProductsAsync()
        {
            SessionUtil.Instance.RequireLogin();
            return await _productRepository.GetProductsAsync();
        }

        public async Task<ProductModel> GetProductByIDAsync(int productID)
        {
            SessionUtil.Instance.RequireLogin();
            if (productID <= 0)
            {
                throw new Exception("Invalid Product ID");
            }

            ProductModel? product = await _productRepository.GetProductByIDAsync(productID);
            if (product == null)
            {
                throw new Exception("Product not found");
            }

            return product;
        }

        public async Task<List<ProductVariantModel>> GetProductVariantsAsync(int productID)
        {
            SessionUtil.Instance.RequireLogin();
            if (productID <= 0)
            {
                throw new Exception("Invalid Product ID");
            }

            return await _productRepository.GetProductVariantsByProductIDAsync(productID);
        }

        public async Task DeleteProductAsync(int productID)
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            if (productID <= 0)
            {
                throw new Exception("Invalid Product ID");
            }

            await _productRepository.DeleteProductAsync(productID);
        }

        public async Task DeleteProductVariantAsync(int productVariantID)
        {
            SessionUtil.Instance.RequireRole(Roles.Admin);
            if (productVariantID <= 0)
            {
                throw new Exception("Invalid Product Variant ID");
            }

            await _productRepository.DeleteProductVariantAsync(productVariantID);
        }

        public async Task<List<ProductModel>> SearchProductsAsync(string searchTerm, string searchFilter)
        {
            SessionUtil.Instance.RequireLogin();
            if (string.IsNullOrEmpty(searchTerm) || string.IsNullOrEmpty(searchFilter))
            {
                throw new Exception("Please enter both a query and filter to search");
            }

            return await _productRepository.SearchProductsAsync(searchTerm, searchFilter);
        }

        public async Task<List<ProductStockAlertModel>> GetStockAlertsAsync()
        {
            SessionUtil.Instance.RequireLogin();
            return await _productRepository.GetStockAlertsAsync();
        }

        public async Task<List<ProductListingModel>> GetProductListingsAsync()
        {
            SessionUtil.Instance.RequireLogin();
            return await _productRepository.GetProductListingsAsync();
        }

        public async Task<List<ProductListingModel>> SearchProductListingsAsync(string searchTerm, string searchFilter)
        {
            SessionUtil.Instance.RequireLogin();
            if (string.IsNullOrEmpty(searchTerm) || string.IsNullOrEmpty(searchFilter))
            {
                throw new InvalidOperationException("Please enter both a query and filter to search");
            }

            return await _productRepository.SearchProductListingAsync(searchTerm, searchFilter);
        }
    }
}
