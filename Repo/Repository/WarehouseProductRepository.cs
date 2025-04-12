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
                .Where(wp => wp.ProductId == productId && wp.Quantity > 0 && wp.Status == "Active")
                .OrderBy(wp => wp.WarehouseId)
                .ThenBy(wp => wp.BatchId)
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
    }

}
