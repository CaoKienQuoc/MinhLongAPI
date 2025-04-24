using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{

    public interface IWarehouseExportRepository
    {
        Task AddRangeAsync(IEnumerable<ExportWarehouseReceipt> receipts);
        Task<List<ExportWarehouseReceipt>> GetByWarehouseIdAsync(long warehouseId);
        Task<List<ExportWarehouseReceipt>> GetByRequestExportIdAsync(int requestExportId);
        Task<ExportWarehouseReceipt?> GetByIdAsync(long receiptId);
        Task UpdateAsync(ExportWarehouseReceipt receipt);
        Task SaveChangesAsync();

        Task<ExportWarehouseReceipt?> GetByIdWithDetailsAsync(int id);

        Task<ExportWarehouseReceipt?> GetByOrderIdAsync(Guid orderId);

        Task<List<ExportWarehouseReceipt>> GetAllByUserIdAsync(Guid userId);
        Task<ExportWarehouseReceipt?> GetByIdAndUserIdAsync(int receiptId, Guid userId);

        Task<List<ExportWarehouseReceiptDetail>> GetDetailsByReceiptIdAsync(long receiptId);

        Task UpdateDetailAsync(ExportWarehouseReceiptDetail detail);

        Task AddDetailAsync(ExportWarehouseReceiptDetail detail);

        Task UpdateReceiptAsync(ExportWarehouseReceipt receipt);

        Task<Batch?> FindSourceBatchAsync(long sourceWarehouseId, long productId, string batchCode);

        Task<WarehouseTransferRequest?> GetWarehouseTransferByRequestExportIdAsync(int requestExportId);
    }

}
