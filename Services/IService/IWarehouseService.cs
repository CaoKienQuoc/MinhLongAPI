using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IService
{
    public interface IWarehouseService
    {
        IEnumerable<WarehouseInfoDto> GetAllWarehouseInfo();
        Warehouse GetWarehouseByUserId(Guid userId);
        void CreateWarehouse(Guid userId, string warehousName, string street, string province, string district, string ward, string note);
        void UpdateWarehouse(Guid userId, long warehouseId, string warehousName, string street, string province, string district, string ward, string note);
        Task<bool> DeleteWarehouseAsync(long warehouseId);

        Task<IEnumerable<WarehouseProductDto>> GetProductsByWarehouseIdAsync(long warehouseId, string sortBy = null);
        Task<WarehouseProductDto> GetProductByIdAsync(long warehouseProductId);

        Task<List<WarehouseProductSummaryDto>> GetProductSummariesByWarehouseIdAsync(long warehouseId);

        Task<List<ProductWarehouseSummaryDto>> GetWarehousesByProductIdAsync(long productId);



    }
}
