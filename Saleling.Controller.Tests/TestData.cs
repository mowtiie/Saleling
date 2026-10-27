using Saleling.Model;
using Saleling.Model.Product;
using Saleling.Util;

namespace Saleling.Controller.Tests
{
    internal static class TestData
    {
        public static void LoginAsCashier(int userId = 7)
        {
            LoginAs(Roles.Cashier, userId);
        }

        public static void LoginAsAdmin(int userId = 1)
        {
            LoginAs(Roles.Admin, userId);
        }

        public static void Logout()
        {
            SessionUtil.Instance.Logout();
        }

        private static void LoginAs(string role, int userId)
        {
            SessionUtil.Instance.SetLoggedInUser(new UserModel
            {
                UserID = userId,
                FirstName = "Test",
                LastName = role,
                Username = $"test{role.ToLowerInvariant()}",
                HashedPassword = string.Empty,
                Role = role
            });
        }

        public static POSProductModel Product(int variantId = 1, decimal price = 100.00m, int stock = 10)
        {
            return new POSProductModel
            {
                VariantID = variantId,
                ProductID = 1,
                ProductName = $"Tee {variantId}",
                SKU = $"SKU-{variantId}",
                Size = "M",
                Color = "Black",
                SellingPrice = price,
                StockQuantity = stock,
                Category = "T-Shirts & Polos"
            };
        }

        public static ProductVariantModel Variant()
        {
            return new ProductVariantModel
            {
                VariantID = 1,
                ProductID = 1,
                SKU = "SKU-1",
                Size = "M",
                Color = "Black",
                CostPrice = 10m,
                SellingPrice = 20m,
                StockQuantity = 5,
                ReorderLevel = 5,
                Status = "Active"
            };
        }
    }
}
