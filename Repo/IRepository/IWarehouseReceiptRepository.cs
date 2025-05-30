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

        Task AddImportTransactionAsync(ImportTransaction importTransaction);

        Task AddImportTransactionDetailAsync(ImportTransactionDetail importTransactionDetail);

        Task<ImportTransactionDetail> GetImportTransactionDetailByIdAsync(long id);
        Task<ImportTransaction> GetImportTransactionByIdAsync(long id);
        Task<List<object>> GetMonthlyReceiptStatsAllAsync();
        Task<List<WarehouseReceipt>> GetReceiptsByDateRangeAsync(DateTime startDate, DateTime endDate);

        Task<List<WarehouseReceipt>> GetAllByYearAsync(int year);
        Task<List<WarehouseReceipt>> GetAllByYearMonthAsync(int year, int month);



    }
}
