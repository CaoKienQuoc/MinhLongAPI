using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IWarehouseProductRepository
    {
        Task<List<WarehouseProduct>> GetAvailableWarehouseProductsAsync(long productId);
        Task UpdateAsync(WarehouseProduct entity);
        Task SaveChangesAsync();
        // Thêm khai báo phương thức mới:
        Task<WarehouseProduct> GetByProductWarehouseBatchAsync(long productId, long warehouseId, long batchId);

        Task<long> GetTotalAvailableStockByProductIdAsync(long productId);
    }

}
