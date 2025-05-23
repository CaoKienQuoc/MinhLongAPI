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
    public class WarehouseTransferRepository : IWarehouseTransferRepository
    {
        private readonly MinhLongDbContext _context;

        public WarehouseTransferRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<WarehouseTransferRequest> requests)
        {
            await _context.WarehouseTransferRequests.AddRangeAsync(requests);
        }

        public async Task<WarehouseTransferRequest?> GetByIdAsync(int id)
        {
            return await _context.WarehouseTransferRequests
                .Include(w => w.TransferProducts) // 👈 nếu bạn cần load TransferProducts
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task UpdateAsync(WarehouseTransferRequest request)
        {
            try
            {
                _context.WarehouseTransferRequests.Update(request);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Xem chi tiết lỗi
                Console.WriteLine("DbUpdateException: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                }

                throw; // hoặc throw lại để đẩy lên phía trên xử lý
            }
        }

        public async Task<List<WarehouseTransferRequest>> GetAllByUserIdAsync(Guid userId)
        {
            return await _context.WarehouseTransferRequests
                .Include(r => r.SourceWarehouse)
                .Include(r => r.DestinationWarehouse)
                .Include(r => r.TransferProducts)
                .ThenInclude(p => p.Product)
                .Include(r => r.TransferProducts)
                .ThenInclude(p => p.Batch)
                .Where(r => r.SourceWarehouse.UserId == userId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        public async Task<WarehouseTransferRequest?> GetByIdAndUserIdAsync(long id, Guid userId)
        {
            return await _context.WarehouseTransferRequests
                .Include(r => r.SourceWarehouse)
                .Include(r => r.DestinationWarehouse)
                .Include(r => r.TransferProducts)
                .ThenInclude(p => p.Product)
                .Include(r => r.TransferProducts)
                .ThenInclude(p => p.Batch)
                .FirstOrDefaultAsync(r => r.Id == id && r.SourceWarehouse.UserId == userId);
        }

        public async Task<List<WarehouseTransferRequest>> GetBySourceWarehouseAsync(long sourceWarehouseId)
        {
            return await _context.WarehouseTransferRequests
                .Include(r => r.TransferProducts)
                .ThenInclude(p => p.Product)
                .Include(r => r.SourceWarehouse)            // 🔹 Kho nguồn
                .Include(r => r.DestinationWarehouse)       // 🔸 Kho đích — thêm dòng này!
                .Where(r => r.SourceWarehouseId == sourceWarehouseId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        public async Task<List<WarehouseTransferRequest>> GetByDestinationWarehouseAsync(long destinationWarehouseId)
        {
            return await _context.WarehouseTransferRequests
                .Include(r => r.TransferProducts)
                .ThenInclude(p => p.Product)
                .Include(r => r.SourceWarehouse)            // 🔹 Kho nguồn — thêm dòng này!
                .Include(r => r.DestinationWarehouse)       // 🔸 Kho đích
                .Where(r => r.DestinationWarehouseId == destinationWarehouseId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        public async Task<WarehouseTransferRequest> GetApprovedTransferByIdAsync(long id)
        {
            return await _context.WarehouseTransferRequests
                .Include(x => x.TransferProducts)
                    .ThenInclude(tp => tp.Batch)
                .FirstOrDefaultAsync(x => x.Id == id && x.Status == "Approved");
        }



    }
}

