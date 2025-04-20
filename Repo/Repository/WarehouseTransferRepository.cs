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

    }
}

