using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
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

        public async Task AddAsync(DamagedStock damagedStock)
        {
            await _context.DamagedStocks.AddAsync(damagedStock);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        /*public async Task<IEnumerable<GetDamagedStockDto>> GetByUserWarehouseAsync(Guid userId)
        {
            return await _context.DamagedStocks
                .Include(ds => ds.Warehouse)
                .Include(ds => ds.Product)
                .Where(ds => ds.Warehouse.UserId == userId)
                .Select(ds => new GetDamagedStockDto
                {
                    DamagedStockId = ds.DamagedStockId,
                    WarehouseId = ds.WarehouseId,
                    WarehouseName = ds.Warehouse.WarehouseName,
                    ProductId = ds.ProductId,
                    ProductName = ds.Product.ProductName,
                    Quantity = ds.Quantity,
                    CreatedAt = ds.CreatedAt,
                    Reason = ds.Reason,
                    Status = ds.Status
                })
                .ToListAsync();
        }*/

        public async Task<List<DamagedStock>> GetDamagedStockByUserAsync(Guid userId)
        {
            return await _context.DamagedStocks
                .Include(ds => ds.Warehouse)
                .Include(ds => ds.Product)
                .Where(ds => ds.Warehouse.UserId == userId)
                .ToListAsync();
        }


        public async Task<List<DamagedStock>> GetWithBatchInfoAsync(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.DamagedStocks
                .Include(ds => ds.Batch)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(ds => ds.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(ds => ds.CreatedAt <= endDate.Value);

            return await query.ToListAsync();
        }

        public async Task<List<DamagedStock>> GetAllAsync()
        {
            return await _context.DamagedStocks
                .Include(x => x.Batch)
                .ToListAsync();
        }

        public async Task<List<DamagedStock>> GetAllByWarehousesAsync(List<long> warehouseIds, int year)
        {
            return await _context.DamagedStocks
                .Include(x => x.Batch)
                .Where(x => x.CreatedAt.Year == year && warehouseIds.Contains(x.WarehouseId))
                .ToListAsync();
        }

    }

}
