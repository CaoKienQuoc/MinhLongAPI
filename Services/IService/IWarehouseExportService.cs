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
using BusinessObject.DTO.Dashboard;

namespace Services.IService
{

    public interface IWarehouseExportService
    {
        Task<ExportWarehouseReceipt> CreateExportReceiptForMainWarehouseAsync(int requestExportId, Guid currentUserId);

        Task FinalizeExportSaleAsync(int exportReceiptId, Guid currentUserId);

        Task<List<ExportWarehouseReceiptDTO>> GetAllExportsByUserAsync(Guid userId);
        Task<ExportWarehouseReceiptDTO?> GetExportByIdAsync(int exportReceiptId, Guid userId);

        Task UpdateExportFromCoordinationImportAsync(int requestExportId, List<BatchResponseDto> importedBatches);

        Task<byte[]> GenerateExportReceiptPdfAsync(int exportReceiptId, Guid userId);

        Task<int> GetTodayExportCountAsync(Guid userId);
        Task<int> GetThisMonthExportCountAsync(Guid userId);
        Task<int> GetTodayExportQuantityAsync(Guid userId);
        Task<int> GetThisMonthExportQuantityAsync(Guid userId);
        Task<decimal> GetTodayExportValueAsync(Guid userId);
        Task<decimal> GetThisMonthExportValueAsync(Guid userId);
        Task CancelRequestExportAsync(int requestExportId, Guid userId);
        Task<List<object>> GetMonthlyExportStatsAllAsync();
        Task<ExportDashboardResponseDto> GetExportDashboardAsync(DateTime? fromDate, DateTime? toDate);
        Task<List<ProfitByMonthDto>> GetProfitStatsAsync(int? year = null, int? month = null);
        Task<ProfitByYearDto> GetAnnualProfitAsync(int? year = null);

    }

}
