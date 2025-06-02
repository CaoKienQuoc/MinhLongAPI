using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.IRepository
{
    public interface IWarehouseRepository
    {
        List<Warehouse> GetAllWarehouses();
        Warehouse GetWarehouseByUserId(Guid userId);
        Warehouse GetWarehouseById(long warehouseId);
        Task<Warehouse> GetWarehouseByIdAsync(long warehouseId);
        void AddWarehouse(Warehouse warehouse);
        void UpdateWarehouse(Warehouse warehouse);
        Task DeleteWarehouseAsync(Warehouse warehouse);

        Task<IEnumerable<WarehouseProductDto>> GetProductsByWarehouseIdAsync(long warehouseId, string sortBy = null);

        Task<WarehouseProductDto> GetProductByIdAsync(long warehouseProductId);
        Task<Guid> GetUserIdByWarehouseIdAsync(long warehouseId);

        Task<List<WarehouseProductSummaryDto>> GetProductSummariesByWarehouseIdAsync(long warehouseId);

        Task<List<WarehouseProduct>> GetByWarehouseIdAndBatchAsync(long warehouseId, IEnumerable<(long ProductId, string BatchCode)> batchPairs);

        Task<List<Product>> GetProductsByIdsAsync(IEnumerable<long> productIds);

        Task<List<ProductWarehouseSummaryDto>> GetWarehousesByProductIdAsync(long productId);

        Task<List<Product>> GetProductsByIdsAsync(List<long> productIds);

        Task<List<Warehouse>> GetAllOrderedAsync(); // theo thứ tự ưu tiên

        Task<Warehouse?> GetByIdAsync(long warehouseId);

        Task<long> GetWarehouseIdByUserAsync(Guid userId);

        IQueryable<WarehouseProduct> GetQueryable();
        Task UpdateRangeAsync(IEnumerable<WarehouseProduct> products);

        Task<string> GetWarehouseNameByIdAsync(long warehouseId);

        Task<List<Warehouse>> GetWarehousesByUserIdAsync(Guid userId);


    }
}
