using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObject.Models;
using BusinessObject.DTO.Warehouse;
using BusinessObject.DTO.Product;

namespace Services.IService
{

    public interface IWarehouseExportService
    {
        Task<ExportWarehouseReceipt> CreateExportReceiptForMainWarehouseAsync(int requestExportId, Guid currentUserId);

        Task FinalizeExportSaleAsync(int exportReceiptId, Guid currentUserId);

        Task<List<ExportWarehouseReceiptDTO>> GetAllExportsByUserAsync(Guid userId);
        Task<ExportWarehouseReceiptDTO?> GetExportByIdAsync(int exportReceiptId, Guid userId);

        Task UpdateExportFromCoordinationImportAsync(int requestExportId, List<BatchResponseDto> importedBatches);
    }

}
