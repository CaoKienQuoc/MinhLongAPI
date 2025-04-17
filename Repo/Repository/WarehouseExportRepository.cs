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
    }


}
