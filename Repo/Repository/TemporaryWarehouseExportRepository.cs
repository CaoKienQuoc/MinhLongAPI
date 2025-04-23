using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;

namespace Repo.Repository
{
    public class TemporaryWarehouseExportRepository : ITemporaryWarehouseExportRepository
    {
        private readonly MinhLongDbContext _context;

        public TemporaryWarehouseExportRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<List<TemporaryStockExport>> GetByConditionAsync(Expression<Func<TemporaryStockExport, bool>> predicate)
        {
            return await _context.TemporaryStockExports
                .Where(predicate)
                .ToListAsync();
        }

        public async Task AddAsync(TemporaryStockExport entity)
        {
            await _context.TemporaryStockExports.AddAsync(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<TemporaryStockExport>> GetByOrderIdAsync(Guid orderId)
        {
            return await _context.TemporaryStockExports
                                 .Include(t => t.Batch) // ✅ Include bảng Batch
                                 .Where(t => t.OrderId == orderId && !t.IsReverted)
                                 .ToListAsync();
        }


        public async Task DeleteByOrderIdAsync(Guid orderId)
        {
            var exports = await GetByOrderIdAsync(orderId);
            if (exports != null && exports.Any())
            {
                _context.TemporaryStockExports.RemoveRange(exports);
            }
        }

        public async Task UpdateAsync(TemporaryStockExport entity)
        {
            // Cập nhật entity trong DbContext
            _context.TemporaryStockExports.Update(entity);
            // Không cần await ở đây nếu không có xử lý bất đồng bộ riêng
            await Task.CompletedTask;
        }

        public async Task<List<TemporaryStockExport>> GetByOrderIdsAsync(List<Guid> orderIds)
        {
            return await _context.TemporaryStockExports
                .Where(t => orderIds.Contains(t.OrderId))
                .ToListAsync();
        }

        public async Task<long> GetReservedStockByProductIdAsync(long productId)
        {
            return await _context.TemporaryStockExports
                .Where(t => t.ProductId == productId && !t.IsReverted)
                .SumAsync(t => (long?)t.Quantity) ?? 0;
        }

        public async Task<TemporaryStockExport?> GetByProductAndBatchAsync(long productId, long batchId, long warehouseId)
        {
            return await _context.TemporaryStockExports
                .FirstOrDefaultAsync(t =>
                    t.ProductId == productId &&
                    t.BatchId == batchId &&
                    t.WarehouseId == warehouseId);
        }

        public async Task DeleteByTemporaryExportIdsAsync(List<long> tempExportIds)
        {
            var stocksToDelete = await _context.TemporaryStockExports
                .Where(s => tempExportIds.Contains(s.TemporaryStockExportId))
                .ToListAsync();

            _context.TemporaryStockExports.RemoveRange(stocksToDelete);
        }

        public async Task<List<TemporaryStockExport>> GetByWarehouseIdAsync(long warehouseId)
        {
            return await _context.TemporaryStockExports
                .Where(t => t.WarehouseId == warehouseId)
                .ToListAsync();
        }
        public async Task<List<TemporaryStockExport>> GetByBatchIdsAsync(List<long> batchIds)
        {
            return await _context.TemporaryStockExports
                .Where(t => batchIds.Contains(t.BatchId))
                .ToListAsync();
        }
    }

}
