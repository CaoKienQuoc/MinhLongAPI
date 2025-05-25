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
    public class WarehouseReceiptRepository : IWarehouseReceiptRepository
    {
        private readonly MinhLongDbContext _context;

        public WarehouseReceiptRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(WarehouseReceipt receipt)
        {
            await _context.WarehouseReceipts.AddAsync(receipt);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<WarehouseReceipt>> GetAllByUserIdAsync(Guid userId)
        {
            return await _context.WarehouseReceipts
                .Where(wr => wr.Warehouse.UserId == userId)
                .ToListAsync();
        }

        public async Task<WarehouseReceipt?> GetByIdAndUserIdAsync(long receiptId, Guid userId)
        {
            return await _context.WarehouseReceipts
                .FirstOrDefaultAsync(wr => wr.WarehouseReceiptId == receiptId && wr.Warehouse.UserId == userId);
        }

        public async Task AddImportTransactionAsync(ImportTransaction importTransaction)
        {
            await _context.ImportTransactions.AddAsync(importTransaction);
        }

        public async Task AddImportTransactionDetailAsync(ImportTransactionDetail importTransactionDetail)
        {
            await _context.ImportTransactionDetails.AddAsync(importTransactionDetail);
        }
        public async Task<ImportTransactionDetail> GetImportTransactionDetailByIdAsync(long id)
        {
            return await _context.Set<ImportTransactionDetail>()
                .FirstOrDefaultAsync(x => x.ImportTransactionDetailId == id);
        }

        public async Task<ImportTransaction> GetImportTransactionByIdAsync(long id)
        {
            return await _context.Set<ImportTransaction>()
                .FirstOrDefaultAsync(x => x.ImportTransactionId == id);
        }

        public async Task<List<object>> GetMonthlyReceiptStatsAllAsync()
        {
            var currentYear = DateTime.Now.Year;

            return await _context.WarehouseReceipts
                .Where(r => r.DocumentDate.Year == currentYear)
                .GroupBy(r => r.DocumentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalReceipts = g.Count(),
                    TotalImportValue = g.Sum(r => r.TotalPrice),
                    TotalQuantityImported = g.Sum(r => r.TotalQuantity)
                })
                .OrderBy(x => x.Month)
                .Select(x => (object)x)
                .ToListAsync();
        }


    }
}
