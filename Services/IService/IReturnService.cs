using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Dashboard;
using BusinessObject.DTO.ReturnOrder;
using BusinessObject.Models;
using Microsoft.AspNetCore.Http;

namespace Services.IService
{
    public interface IReturnService
    {
        Task<ReturnRequest> CreateReturnRequestWithImagesAsync(
    Guid orderId,
    List<(Guid orderDetailId, int quantity, string reason)> itemDetails,
    Guid userId,
    List<IFormFile> images);
        Task ApproveReturnRequestAsync(Guid returnRequestId, Guid warehouseUserId);
        Task<List<ReturnRequest>> GetPendingReturnsAsync();

        Task<List<ReturnRequestProdductDto>> GetAllReturnRequestsAsync();
        Task<ReturnRequestProdductDto> GetReturnRequestByIdAsync(Guid id);

        Task<List<ReturnRequestProdductDto>> GetAllReturnRequestsAsyncForSales(Guid userId);

        Task<ReturnRequestProdductDto> GetReturnRequestByIdAsyncForSales(Guid id, Guid userId);

        Task<List<ReturnRequestProdductDto>> GetApprovedReturnRequestsAsync();
        Task<ReturnRequestProdductDto> GetApprovedReturnRequestByIdAsync(Guid id);

        Task<List<ReturnWarehouseReceiptDto>> GetAllReturnWarehouseReceiptsAsync();
        Task<ReturnWarehouseReceiptDto> GetReturnWarehouseReceiptByIdAsync(long id);
        Task<IEnumerable<ReturnWarehouseReceiptDto>> GetByWarehouseIdAsync(long warehouseId);

        Task<IEnumerable<ReturnRequestProdductDto>> GetReturnRequestsByUserIdAsync(Guid userId);
        Task<ReturnRequestProdductDto> GetReturnRequestByIdAsync(Guid returnRequestId, Guid userId);
        Task RejectReturnRequestAsync(Guid returnRequestId, Guid userId, string rejectReason);
        Task<ReturnWarehouseReceiptDashboardDto> GetReturnWarehouseReceiptDashboardAsync(DateTime? fromDate, DateTime? toDate);
        Task<ReturnWarehouseReceiptDashboardDto> GetReturnWarehouseDashboardByUserWarehouseAsync(Guid userId, DateTime? fromDate, DateTime? toDate);

    }

}
