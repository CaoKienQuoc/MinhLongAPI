using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.ReturnOrder;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;

namespace Repo.Repository
{
    public class DamagedStockRepository : IDamagedStockRepository
    {
        private readonly MinhLongDbContext _context;

        public DamagedStockRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(List<DamagedStock> stocks)
        {
            _context.DamagedStocks.AddRange(stocks);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<DamagedStockDto>> GetByWarehouseIdAsync(long warehouseId)
        {
            return await _context.DamagedStocks
                .Where(ds => ds.WarehouseId == warehouseId)
                .Include(ds => ds.Warehouse)
                .Include(ds => ds.Product)
                .Include(ds => ds.Batch)
                .Select(ds => new DamagedStockDto
                {
                    DamagedStockId = ds.DamagedStockId,
                    WarehouseId = ds.WarehouseId,
                    WarehouseName = ds.Warehouse.WarehouseName,
                    ProductId = ds.ProductId,
                    ProductName = ds.Product.ProductName,
                    Quantity = ds.Quantity,
                    CreatedAt = ds.CreatedAt,
                    BatchId = ds.Batch.BatchId
                })
                .ToListAsync();
        }

    }

}
