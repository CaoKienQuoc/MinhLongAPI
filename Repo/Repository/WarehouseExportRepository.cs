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

        public async Task<ExportWarehouseReceipt> GetMainExportReceiptByRequestExportIdAsync(long requestExportId)
        {
            return await _context.ExportWarehouseReceipts
                .Include(r => r.ExportWarehouseReceiptDetails)
                .Where(r => r.RequestExportId == requestExportId
                    && r.ExportType != "ExportCoordination")
                .OrderByDescending(r => r.DocumentDate) // lấy phiếu mới nhất nếu có nhiều phiếu
                .FirstOrDefaultAsync();
        }


        public async Task<ExportWarehouseReceiptDetail> GetDetailAsync(long exportWarehouseReceiptId, long productId, string batchNumber)
        {
            return await _context.ExportWarehouseReceiptDetail
                .FirstOrDefaultAsync(d =>
                    d.ExportWarehouseReceiptId == exportWarehouseReceiptId
                    && d.ProductId == productId
                    && d.BatchNumber == batchNumber);
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

        public async Task<ExportWarehouseReceipt> GetByIdWithDetailsAsync(int id)
        {
            var et = _context.Model.FindEntityType(typeof(ExportWarehouseReceipt));
            var props = et.GetProperties().Select(p => p.Name);
            Console.WriteLine("ExportWarehouseReceipt properties: "
                + string.Join(", ", props));


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
                .Include(r => r.RequestExport)
            .ThenInclude(re => re.RequestExportDetails)
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
                .Include(r => r.RequestExport)
            .ThenInclude(re => re.RequestExportDetails)
                .FirstOrDefaultAsync(e => e.ExportWarehouseReceiptId == receiptId && e.Warehouse.UserId == userId);
        }

        public async Task<List<ExportWarehouseReceiptDetail>> GetDetailsByReceiptIdAsync(long receiptId)
        {
            return await _context.ExportWarehouseReceiptDetail
                                 .Where(x => x.ExportWarehouseReceiptId == receiptId)
                                 .ToListAsync();
        }

        public async Task UpdateDetailAsync(ExportWarehouseReceiptDetail detail)
        {
            _context.ExportWarehouseReceiptDetail.Update(detail);
            await Task.CompletedTask;
        }

        public async Task AddDetailAsync(ExportWarehouseReceiptDetail detail)
        {
            await _context.ExportWarehouseReceiptDetail.AddAsync(detail);
        }

        public async Task UpdateReceiptAsync(ExportWarehouseReceipt receipt)
        {
            _context.ExportWarehouseReceipts.Update(receipt);
            await Task.CompletedTask;
        }

        public async Task<Batch?> FindSourceBatchAsync(long sourceWarehouseId, long productId, string batchCode)
        {
            return await _context.Batches
                .Include(b => b.ImportTransactionDetail)
                .ThenInclude(d => d.ImportTransaction)
                .Where(b =>
                    b.ProductId == productId &&
                    b.BatchCode == batchCode &&
                    b.ImportTransactionDetail != null &&
                    b.ImportTransactionDetail.ImportTransaction.WarehouseId == sourceWarehouseId
                )
                .FirstOrDefaultAsync();
        }

        public async Task<WarehouseTransferRequest?> GetWarehouseTransferByRequestExportIdAsync(int requestExportId)
        {
            return await _context.WarehouseTransferRequests
                .FirstOrDefaultAsync(x => x.RequestExportId == requestExportId);
        }

        public async Task<ExportWarehouseReceipt> GetExportSaleByOrderIdAsync(Guid orderId)
        {
            var requestExportId = await _context.RequestExports
                .Where(x => x.OrderId == orderId)
                .Select(x => x.RequestExportId)
                .FirstOrDefaultAsync();

            if (requestExportId == 0)
                return null;

            var exportReceipt = await _context.ExportWarehouseReceipts
                .Where(x => x.RequestExportId == requestExportId && x.ExportType == "ExportSale")
                .Include(x => x.ExportWarehouseReceiptDetails)
                .FirstOrDefaultAsync();

            return exportReceipt;
        }

        public async Task<long> GetWarehouseIdFromOrderAsync(Guid orderId)
        {
            // 1. Lấy RequestExportId từ OrderId
            var requestExportId = await _context.RequestExports
                .Where(x => x.OrderId == orderId)
                .Select(x => x.RequestExportId)
                .FirstOrDefaultAsync();

            // 2. Tìm ExportWarehouseReceipt có ExportType = 'ExportSale'
            var exportReceipt = await _context.ExportWarehouseReceipts
                .Where(x => x.RequestExportId == requestExportId && x.ExportType == "ExportSale")
                .FirstOrDefaultAsync();

            return exportReceipt.WarehouseId;
        }

        public async Task<List<object>> GetMonthlyExportStatsAllAsync()
        {
            var currentYear = DateTime.Now.Year;

            return await _context.ExportWarehouseReceipts
                .Where(r => r.DocumentDate.Year == currentYear)
                .GroupBy(r => r.DocumentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalExports = g.Count(),
                    TotalExportValue = g.Sum(x => x.TotalAmount),
                    TotalQuantityExported = g.SelectMany(x => x.ExportWarehouseReceiptDetails).Sum(d => d.Quantity)
                })
                .OrderBy(x => x.Month)
                .Select(x => (object)x)
                .ToListAsync();
        }

        public async Task<List<ExportWarehouseReceipt>> GetAllAsync()
        {
            return await _context.ExportWarehouseReceipts
                .Include(e => e.Warehouse)
                .Include(e => e.ExportWarehouseReceiptDetails)
                .ToListAsync();
        }

        public async Task<List<ExportWarehouseReceipt>> GetAllByYearAsync(int year)
        {
            return await _context.ExportWarehouseReceipts
                .Where(r => r.DocumentDate.Year == year)
                .ToListAsync();
        }

        public async Task<List<ExportWarehouseReceipt>> GetAllByYearMonthAsync(int year, int month)
        {
            return await _context.ExportWarehouseReceipts
                .Where(r => r.DocumentDate.Year == year && r.DocumentDate.Month == month)
                .ToListAsync();
        }

        public async Task<ExportWarehouseReceipt> GetExportWarehouseReceiptByIdAsync(long warehouseRequestExportId)
        {
            return await _context.ExportWarehouseReceipts
                .Include(re => re.ExportWarehouseReceiptDetails)
                .FirstOrDefaultAsync(re => re.ExportWarehouseReceiptId == warehouseRequestExportId);
        }
    }


}
