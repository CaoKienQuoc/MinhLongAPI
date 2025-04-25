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
    }

}
