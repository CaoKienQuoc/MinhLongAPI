using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Services.IService
{

    public interface IWarehouseExportService
    {
        Task<List<ExportWarehouseReceipt>> CreateInternalTransferReceiptsAsync(int requestExportId, Guid currentUserId);

        /*Task<List<ExportWarehouseReceipt>> GetByWarehouseIdAsync(long warehouseId);

        Task<List<ExportWarehouseReceipt>> GetInternalTransfersByWarehouseAsync(long warehouseId);

        Task<bool> CompleteInternalTransferAsync(long receiptId);

        Task<ExportWarehouseReceipt?> GetMainWarehouseReceiptAsync(int requestExportId);*/
    }

}
