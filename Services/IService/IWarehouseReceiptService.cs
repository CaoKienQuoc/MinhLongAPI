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
    }
}
