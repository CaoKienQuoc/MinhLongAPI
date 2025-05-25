using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;

namespace Repo.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly MinhLongDbContext _context;

        public ProductRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetTotalProductsAsync() // ✅ Triển khai phương thức này
        {
            return await _context.Products.CountAsync();
        }


        public async Task<List<Product>> GetProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Creator)
                    .ThenInclude(u => u.Employee) // 👈 Include nhân viên tạo
                .Include(p => p.Updater)
                    .ThenInclude(u => u.Employee) // 👈 Include nhân viên cập nhật
                .ToListAsync();
        }


        /*public async Task<Product> GetByIdAsync(long id)
        {
            return await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Creator)
                    .ThenInclude(u => u.Employee)
                .Include(p => p.Updater)
                    .ThenInclude(u => u.Employee)
                .FirstOrDefaultAsync(p => p.ProductId == id);
        }*/

        public async Task<Product> GetByIdAsync(long id, bool asNoTracking = false)
        {
            var query = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Creator)
                    .ThenInclude(u => u.Employee)
                .Include(p => p.Updater)
                    .ThenInclude(u => u.Employee)
                .Where(p => p.ProductId == id);

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            var product = await query.FirstOrDefaultAsync();
            if (product == null) return null;

            // ✅ JOIN WarehouseProduct -> Batch để lấy giá của lô hàng mới nhất (còn tồn kho)
            var latestSellingPrice = await (
                from wp in _context.WarehouseProduct
                join b in _context.Batches on wp.BatchId equals b.BatchId
                where wp.ProductId == product.ProductId
                      && wp.Status == "ACTIVE"
                      && b.Status == "ACTIVE"
                      && wp.Quantity > 0 // ✅ Bỏ qua lô hết hàng
                orderby b.BatchId descending // hoặc b.DateOfManufacture descending nếu muốn theo ngày
                select b.SellingPrice
            ).FirstOrDefaultAsync();


            if (latestSellingPrice > 0)
            {
                product.Price = latestSellingPrice;
            }

            return product;
        }



        public async Task<Product> AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync(); // 🔥 Lưu để có ProductId

            return product;
        }


        public async Task<Product> UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            

            await _context.SaveChangesAsync();

            return product;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return false;
            }
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Product>> GetProductsByCategoryIdAsync(long categoryId)
        {
            return await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Images) // Load danh sách ảnh
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(long productId) // ✅ Đổi tên cho dễ hiểu
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.TaxConfig)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAvailableStockOnlyAsync(long productId, int availableStock)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Product SET AvailableStock = {0} WHERE ProductId = {1}",
                availableStock, productId);
        }

        public async Task<List<Product>> GetListByIdsAsync(List<long> productIds)
        {
            return await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();
        }

        public Task<bool> ExistsAsync(long productId) =>
        _context.Products
                .AsNoTracking()
                .AnyAsync(p => p.ProductId == productId);

        public Task<int?> GetDefaultExpirationAsync(long productId) =>
    _context.Products
            .AsNoTracking()
            .Where(p => p.ProductId == productId)
            .Select(p => p.DefaultExpiration)
            .FirstOrDefaultAsync();  // trả về int? tương ứng

    }
}
