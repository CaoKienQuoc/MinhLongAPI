using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IProductRepository
    {
        Task<int> GetTotalProductsAsync(); // ✅ Thêm phương thức này
        Task<List<Product>> GetProductsAsync();
        //Task<Product> GetByIdAsync(long id);
        //Task<Product> AddAsync(Product product, List<string> imageUrls);

        Task<Product> AddAsync(Product product);
        //Task<Product> UpdateAsync(Product product, List<string> imageUrls);
        Task<Product> UpdateAsync(Product product);
        Task<bool> DeleteAsync(long id);

        Task<List<Product>> GetProductsByCategoryIdAsync(long categoryId);

        Task<Product?> GetProductByIdAsync(long productId);
        Task<Product> GetByIdAsync(long id, bool asNoTracking = false);

        Task SaveChangesAsync();
        Task UpdateAvailableStockOnlyAsync(long productId, int availableStock);
        Task<List<Product>> GetListByIdsAsync(List<long> productIds);


        Task<bool> ExistsAsync(long productId);

        // Lấy giá trị DefaultExpiration của product
        // IProductRepository.cs
        Task<int?> GetDefaultExpirationAsync(long productId);
        Task<Product> UpdatePriceAsync(Product product);


    }
}
