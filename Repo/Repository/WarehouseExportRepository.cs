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
    public class WarehouseExportRepository : IWarehouseExportRepository
    {
        private readonly MinhLongDbContext _context;

        public WarehouseExportRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<ExportWarehouseReceipt> receipts)
        {
            await _context.ExportWarehouseReceipts.AddRangeAsync(receipts);
        }

        public async Task<List<ExportWarehouseReceipt>> GetByWarehouseIdAsync(long warehouseId)
        {
            return await _context.ExportWarehouseReceipts
                .Include(r => r.ExportWarehouseReceiptDetails)
                .Where(r => r.WarehouseId == warehouseId)
                .ToListAsync();
        }

        public async Task<List<ExportWarehouseReceipt>> GetByRequestExportIdAsync(int requestExportId)
        {
            return await _context.ExportWarehouseReceipts
                .Include(r => r.ExportWarehouseReceiptDetails)
                .Where(r => r.RequestExportId == requestExportId)
                .ToListAsync();
        }

        public async Task<ExportWarehouseReceipt?> GetByIdAsync(long receiptId)
        {
            return await _context.ExportWarehouseReceipts
                .Include(r => r.ExportWarehouseReceiptDetails)
                .FirstOrDefaultAsync(r => r.ExportWarehouseReceiptId == receiptId);
        }

        public async Task UpdateAsync(ExportWarehouseReceipt receipt)
        {
            _context.ExportWarehouseReceipts.Update(receipt);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<ExportWarehouseReceipt?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.ExportWarehouseReceipts
                .Include(r => r.ExportWarehouseReceiptDetails) // load chi tiết sản phẩm
                .FirstOrDefaultAsync(r => r.ExportWarehouseReceiptId == id);
        }

        public async Task<ExportWarehouseReceipt?> GetByOrderIdAsync(Guid orderId)
        {
            return await _context.ExportWarehouseReceipts
                .Include(e => e.ExportWarehouseReceiptDetails)
                .FirstOrDefaultAsync(e => e.RequestExport.OrderId == orderId);
        }

        public async Task<List<ExportWarehouseReceipt>> GetAllByUserIdAsync(Guid userId)
        {
            return await _context.ExportWarehouseReceipts
                .Include(e => e.Warehouse)
                .Include(e => e.ExportWarehouseReceiptDetails)
                .ThenInclude(d => d.Product)
                .Include(r => r.RequestExport)
                    .ThenInclude(req => req.Order)
                        .ThenInclude(o => o.RequestProduct)
                        .ThenInclude(x => x.AgencyAccount)
                .Where(e => e.Warehouse.UserId == userId)
                .ToListAsync();
        }

        public async Task<ExportWarehouseReceipt?> GetByIdAndUserIdAsync(int receiptId, Guid userId)
        {
            return await _context.ExportWarehouseReceipts
                 .Include(e => e.Warehouse)
                .Include(e => e.ExportWarehouseReceiptDetails)
                .ThenInclude(d => d.Product)
                .Include(r => r.RequestExport)
                    .ThenInclude(req => req.Order)
                        .ThenInclude(o => o.RequestProduct)
                        .ThenInclude(x => x.AgencyAccount)
                .FirstOrDefaultAsync(e => e.ExportWarehouseReceiptId == receiptId && e.Warehouse.UserId == userId);
        }
    }


}
