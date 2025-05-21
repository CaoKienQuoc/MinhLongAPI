using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface ITemporaryWarehouseExportRepository
    {
        Task AddAsync(TemporaryStockExport entity);
        Task SaveChangesAsync();

        // Thêm khai báo phương thức GetByOrderIdAsync:
        Task<List<TemporaryStockExport>> GetByOrderIdAsync(Guid orderId);

        // Nếu cần: phương thức xoá các bản ghi theo OrderId
        Task DeleteByOrderIdAsync(Guid orderId);
        Task UpdateAsync(TemporaryStockExport entity); // <-- Thêm định nghĩa này

        Task<List<TemporaryStockExport>> GetByOrderIdsAsync(List<Guid> orderIds);

        Task<long> GetReservedStockByProductIdAsync(long productId);
        Task<List<TemporaryStockExport>> GetByConditionAsync(Expression<Func<TemporaryStockExport, bool>> predicate);

        Task<TemporaryStockExport?> GetByProductAndBatchAsync(long productId, long batchId, long warehouseId);

        Task DeleteByTemporaryExportIdsAsync(List<long> tempExportIds);

        Task<List<TemporaryStockExport>> GetByWarehouseIdAsync(long warehouseId);

        Task<List<TemporaryStockExport>> GetByBatchIdsAsync(List<long> batchIds);

        Task<List<(WarehouseProduct, Batch)>> GetWarehouseProductsAndBatchesByOrderIdAsync(Guid orderId);

        Task UpdateRangeAsync(IEnumerable<TemporaryStockExport> tempExports);
    }
}
