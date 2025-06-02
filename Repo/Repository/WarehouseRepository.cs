using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.Repository
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly MinhLongDbContext _context;

        public WarehouseRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public List<Warehouse> GetAllWarehouses() =>
    _context.Warehouses
        .Include(w => w.Address)
            .ThenInclude(a => a.Province)
        .Include(w => w.Address.District)
        .Include(w => w.Address.Ward)
        .ToList();

        public Warehouse GetWarehouseByUserId(Guid userId)
                => _context.Warehouses
               .Include(w => w.Address) // Load thông tin Address theo AddressId
               .FirstOrDefault(w => w.UserId == userId);


        public Warehouse GetWarehouseById(long warehouseId)
            => _context.Warehouses.Find(warehouseId);

        public async Task<Warehouse> GetWarehouseByIdAsync(long warehouseId)
        {
            return await _context.Warehouses
                .Include(w => w.Address) // Load Address để đảm bảo xóa Cascade
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId);
        }

        public void AddWarehouse(Warehouse warehouse)
        {

            _context.Warehouses.Add(warehouse);
            _context.SaveChanges();
        }

        public void UpdateWarehouse(Warehouse warehouse)
        {
            _context.Warehouses.Update(warehouse);
            _context.SaveChanges();
        }

        public async Task DeleteWarehouseAsync(Warehouse warehouse)
        {
            // Kiểm tra xem Warehouse có Address không
            if (warehouse.AddressId != null)
            {
                var address = await _context.Addresses.FindAsync(warehouse.AddressId);
                if (address != null)
                {
                    _context.Addresses.Remove(address); // 🔥 XÓA ADDRESS TRƯỚC
                }
            }

            _context.Warehouses.Remove(warehouse); // 🔥 Sau đó xóa Warehouse
            await _context.SaveChangesAsync(); // 🔥 Lưu thay đổi vào DB
        }

        public async Task<IEnumerable<WarehouseProductDto>> GetProductsByWarehouseIdAsync(long warehouseId, string sortBy = null)
        {
            var query = _context.WarehouseProduct
                .Where(wp => wp.WarehouseId == warehouseId)
                .Select(wp => new WarehouseProductDto
                {
                    WarehouseProductId = wp.WarehouseProductId,
                    ProductId = wp.ProductId,
                    ProductName = wp.Product.ProductName,
                    WarehouseId = wp.WarehouseId,
                    BatchId = wp.BatchId,
                    BatchCode = wp.Batch.BatchCode,
                    ExpirationDate = wp.ExpirationDate,
                    Quantity = wp.Quantity,
                    Status = wp.Status,
                    Price = wp.Batch.SellingPrice
                });

            // Sort logic
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "status":
                        query = query.OrderBy(wp => wp.Status);
                        break;
                    case "expirationdate_desc":
                        query = query.OrderByDescending(wp => wp.ExpirationDate);
                        break;
                    case "expirationdate_asc":
                        query = query.OrderBy(wp => wp.ExpirationDate);
                        break;
                }
            }

            return await query.ToListAsync();
        }

        public async Task<WarehouseProductDto> GetProductByIdAsync(long warehouseProductId)
        {
            return await _context.WarehouseProduct
                .Where(wp => wp.WarehouseProductId == warehouseProductId)
                .Select(wp => new WarehouseProductDto
                {
                    WarehouseProductId = wp.WarehouseProductId,
                    ProductId = wp.ProductId,
                    ProductName = wp.Product.ProductName,
                    WarehouseId = wp.WarehouseId,
                    BatchId = wp.BatchId,
                    BatchCode = wp.Batch.BatchCode,
                    ExpirationDate = wp.ExpirationDate,
                    Quantity = wp.Quantity,
                    Status = wp.Status,
                    Price = wp.Batch.SellingPrice
                })
                .FirstOrDefaultAsync();
        }

        public async Task<Guid> GetUserIdByWarehouseIdAsync(long warehouseId)
        {
            var userId = await _context.Warehouses
                .Where(w => w.WarehouseId == warehouseId)
                .Select(w => w.UserId)
                .FirstAsync(); // dùng FirstAsync vì bạn đảm bảo tồn tại

            return userId;
        }

        public async Task<List<WarehouseProductSummaryDto>> GetProductSummariesByWarehouseIdAsync(long warehouseId)
        {
            return await _context.WarehouseProduct
                .Where(wp => wp.WarehouseId == warehouseId)
                .GroupBy(wp => new { wp.ProductId, wp.Product.ProductName, wp.Status })
                .Select(g => new WarehouseProductSummaryDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Status = g.Key.Status, // ✅ Trả về Status
                    TotalQuantity = g.Sum(x => x.Quantity)
                })
                .ToListAsync();
        }

        public async Task<List<WarehouseProduct>> GetByWarehouseIdAndBatchAsync(long warehouseId, IEnumerable<(long ProductId, string BatchCode)> batchPairs)
        {
            var productIds = batchPairs.Select(x => x.ProductId).Distinct();
            var batchCodes = batchPairs.Select(x => x.BatchCode).Distinct();

            return await _context.WarehouseProduct
                .Include(wp => wp.Product)
                .Include(wp => wp.Batch)
                .Where(wp => wp.WarehouseId == warehouseId &&
                             productIds.Contains(wp.ProductId) &&
                             batchCodes.Contains(wp.Batch.BatchCode))
                .ToListAsync();
        }

        public async Task<List<Product>> GetProductsByIdsAsync(IEnumerable<long> productIds)
        {
            if (productIds == null || !productIds.Any())
            {
                return new List<Product>();
            }

            return await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();
        }

        public async Task<List<ProductWarehouseSummaryDto>> GetWarehousesByProductIdAsync(long productId)
        {
            return await _context.WarehouseProduct
                .Where(wp => wp.ProductId == productId)
                .GroupBy(wp => new
                {
                    wp.ProductId,
                    wp.Product.ProductName,
                    wp.WarehouseId,
                    wp.Warehouse.WarehouseName
                })
                .Select(g => new ProductWarehouseSummaryDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    WarehouseId = g.Key.WarehouseId,
                    WarehouseName = g.Key.WarehouseName,
                    TotalQuantity = g.Sum(x => x.Quantity)
                })
                .ToListAsync();
        }

        public async Task<List<Product>> GetProductsByIdsAsync(List<long> productIds)
        {
            return await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();
        }

        public async Task<List<Warehouse>> GetAllOrderedAsync()
        {
            return await _context.Warehouses
                .OrderBy(w => w.WarehouseId) // 👈 Hoặc bạn có thể dùng field khác nếu có cột "Priority"
                .ToListAsync();
        }

        public async Task<Warehouse?> GetByIdAsync(long warehouseId)
        {
            return await _context.Warehouses
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId);
        }

        public async Task<long> GetWarehouseIdByUserAsync(Guid userId)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (warehouse == null)
                throw new Exception("Không tìm thấy kho ứng với người dùng.");

            return warehouse.WarehouseId;
        }

        public IQueryable<WarehouseProduct> GetQueryable()
        {
            return _context.WarehouseProduct.AsQueryable();
        }

        public async Task UpdateRangeAsync(IEnumerable<WarehouseProduct> products)
        {
            _context.WarehouseProduct.UpdateRange(products);
            await _context.SaveChangesAsync();
        }

        public async Task<string> GetWarehouseNameByIdAsync(long warehouseId)
        {
            var warehouse = await _context.Warehouses
                .Where(w => w.WarehouseId == warehouseId)
                .Select(w => w.WarehouseName)
                .FirstOrDefaultAsync();

            if (warehouse == null)
                throw new Exception("Không tìm thấy kho");

            return warehouse;
        }

        public async Task<List<Warehouse>> GetWarehousesByUserIdAsync(Guid userId)
        {
            return await _context.Warehouses
                .Where(w => w.UserId == userId)
                .ToListAsync();
        }

    }
}
