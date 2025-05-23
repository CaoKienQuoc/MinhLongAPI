using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;

namespace Services.IService
{
    public interface IWarehouseReceiptService
    {
        //Task<WarehouseReceipt> CreateWarehouseReceiptFromCoordinationAsync(long warehouseId);

        Task<bool> CreateReceiptAsync(WarehouseReceiptRequest request, Guid currentUserId);

        Task<List<WarehouseReceiptDTO>> GetAllReceiptsByUserAsync(Guid userId);

        Task<WarehouseReceiptDTO?> GetReceiptByIdAsync(long id, Guid userId);

        Task<bool> ImportApprovedTransferAsync(long transferRequestId, Guid currentUserId);

        Task<byte[]> GenerateReceiptPdfAsync(long warehouseReceiptId, Guid userId);
        Task<int> GetTodayReceiptCountAsync(Guid userId);
        Task<int> GetThisMonthReceiptCountAsync(Guid userId);
        Task<int> GetTodayTotalQuantityAsync(Guid userId);
        Task<int> GetThisMonthTotalQuantityAsync(Guid userId);
        Task<decimal> GetTodayTotalPriceAsync(Guid userId);
        Task<decimal> GetThisMonthTotalPriceAsync(Guid userId);

    }
}
