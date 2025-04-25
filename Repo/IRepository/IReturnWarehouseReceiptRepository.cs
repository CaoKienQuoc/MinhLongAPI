using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IReturnWarehouseReceiptRepository
    {
        Task<ReturnWarehouseReceipt> CreateAsync(ReturnWarehouseReceipt request);
        Task<List<ReturnWarehouseReceiptDetail>> CreateReturnWarehouseReceiptDetailAsync(List<ReturnWarehouseReceiptDetail> request);
        Task SaveChangesAsync(); // ✅ thêm dòng này
    }

}
