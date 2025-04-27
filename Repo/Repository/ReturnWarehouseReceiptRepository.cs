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
    public class ReturnWarehouseReceiptRepository : IReturnWarehouseReceiptRepository
    {
        private readonly MinhLongDbContext _context;

        public ReturnWarehouseReceiptRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<ReturnWarehouseReceipt> CreateAsync(ReturnWarehouseReceipt request)
        {
            _context.ReturnWarehouseReceipts.Add(request);
            // KHÔNG gọi SaveChanges ở đây nếu bạn muốn kiểm soát ở ngoài
            return request;
        }

        public async Task<List<ReturnWarehouseReceiptDetail>> CreateReturnWarehouseReceiptDetailAsync(List<ReturnWarehouseReceiptDetail> request)
        {
            _context.ReturnWarehouseReceiptDetails.AddRange(request);
            // KHÔNG gọi SaveChanges ở đây nếu bạn muốn kiểm soát Save ở ngoài
            return request;
        }


        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<ReturnWarehouseReceipt?> GetByIdWithDetailsAsync(long receiptId)
        {
            return await _context.ReturnWarehouseReceipts
                .Include(r => r.Details)  // ensure you have nav prop Details
                .ThenInclude(d => d.Product)
                .Include(r => r.ReturnRequest)
                .ThenInclude(r => r.Order)
                    .ThenInclude(r => r.RequestProduct)
                    .ThenInclude(r => r.AgencyAccount)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(r => r.ReturnWarehouseReceiptId == receiptId);
        }

        public async Task UpdateStatusAsync(long receiptId, string newStatus)
        {
            var receipt = await _context.ReturnWarehouseReceipts.FindAsync(receiptId);
            if (receipt == null) throw new Exception("Phiếu trả hàng không tồn tại");
            receipt.Status = newStatus;
            await _context.SaveChangesAsync();
        }
        public async Task<List<ReturnWarehouseReceipt>> GetAllAsync()
        {
            return await _context.ReturnWarehouseReceipts
                .Include(r => r.Details)
                .ThenInclude(d => d.Product)
                .Include(r => r.ReturnRequest)
                .ThenInclude(r => r.Order)
                    .ThenInclude(r => r.RequestProduct)
                    .ThenInclude(r => r.AgencyAccount)
                    .ThenInclude(r => r.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<ReturnWarehouseReceipt>> GetByWarehouseIdAsync(long warehouseId)
        {
            return await _context.ReturnWarehouseReceipts
                .Where(r => r.WarehouseId == warehouseId)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Product) // Include thêm Product cho mỗi Detail
                .ToListAsync();
        }


    }

}
