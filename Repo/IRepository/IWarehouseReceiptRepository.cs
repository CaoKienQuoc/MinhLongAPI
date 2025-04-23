using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IWarehouseReceiptRepository
    {
        Task AddAsync(WarehouseReceipt receipt);
        Task SaveChangesAsync();
        Task<List<WarehouseReceipt>> GetAllByUserIdAsync(Guid userId);
        Task<WarehouseReceipt?> GetByIdAndUserIdAsync(long receiptId, Guid userId);

        Task AddAsync(ImportTransaction importTransaction);

        Task AddAsync(ImportTransactionDetail importTransactionDetail);

    }
}
