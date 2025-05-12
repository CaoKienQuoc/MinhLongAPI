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
    public class WarehouseProductRepository : IWarehouseProductRepository
    {
        private readonly MinhLongDbContext _context;

        public WarehouseProductRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<List<WarehouseProduct>> GetAvailableWarehouseProductsAsync(long productId)
        {
            return await _context.WarehouseProduct
                .Include(wp => wp.Batch) // ✅ Thêm dòng này
                .Where(wp => wp.ProductId == productId
                             && wp.Quantity > 0
                             && wp.Status == "ACTIVE"
                             && wp.ExpirationDate > DateTime.UtcNow)
                .OrderBy(wp => wp.ExpirationDate)
                .ThenBy(wp => wp.WarehouseId)
                .ToListAsync();
        }

        public async Task<List<WarehouseProduct>> GetByProductIdAsync(long productId)
        {
            return await _context.WarehouseProduct
                .Where(wp => wp.ProductId == productId)
                .Include(wp => wp.Batch) // Đảm bảo Batch được load
                .ToListAsync();
        }


        public async Task UpdateAsync(WarehouseProduct entity)
        {
            _context.WarehouseProduct.Update(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<long> GetTotalAvailableStockByProductIdAsync(long productId)
        {
            return await _context.WarehouseProduct
                .Where(wp => wp.ProductId == productId && wp.Status == "ACTIVE")
                .SumAsync(wp => (long?)wp.Quantity) ?? 0;
        }

        public async Task<WarehouseProduct> GetByProductWarehouseBatchAsync(long productId, long warehouseId, long batchId)
        {
            return await _context.WarehouseProduct
                .FirstOrDefaultAsync(wp => wp.ProductId == productId
                                        && wp.WarehouseId == warehouseId
                                        && wp.BatchId == batchId);

        }

        public async Task<WarehouseProduct?> GetByProductAndBatchAsync(long productId, long batchId)
        {
            return await _context.WarehouseProduct
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.BatchId == batchId);
        }

        public async Task AddAsync(WarehouseProduct entity)
        {
            await _context.WarehouseProduct.AddAsync(entity);
        }

        public async Task<WarehouseProduct?> GetByIdAsync(long warehouseProductId)
        {
            return await _context.WarehouseProduct
                .Include(wp => wp.Batch)  // ✅ Include Batch để lấy thông tin Batch
                .FirstOrDefaultAsync(wp => wp.WarehouseProductId == warehouseProductId);
        }

        public async Task<List<WarehouseProduct>> GetByBatchIdAsync(long batchId)
        {
            return await _context.WarehouseProduct
                .Where(wp => wp.BatchId == batchId)
                .ToListAsync();
        }
    }
}
