using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using DataAccessLayer;
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
    }
}
