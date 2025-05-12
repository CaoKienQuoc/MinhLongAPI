using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.DTO.Warehouse;
using BusinessObject.Models;

namespace Services.IService
{
    public interface IBatchService
    {
        Task<Batch> GetBatchByIdAsync(long batchId);
        Task<IEnumerable<Batch>> GetAllBatchesAsync();
        Task<Batch> UpdateBatchAsync(UpdateBatchDto dto, Guid userId, long batchId);
        Task<IEnumerable<Batch>> GetBatchesByProductIdAsync(long productId);

        Task<(bool Success, string Message, object? Data)> UpdateProfitMarginAsync(long batchId, decimal profitMarginPercent);
        Task<ProductInfoByBatchDto?> GetProductInfoByBatchIdAsync(long batchId);

        Task<List<BatchDisplayDto>> GetBatchesByWarehouseIdAsync(long warehouseId);

        Task<int> UpdateExpiredBatchesAsync(DateTime nowVietnamTime);

        Task<bool> CancelExpiredBatchAsync(long batchId, string? reason = null);

        Task<bool> UpdateSoldOutStatusAsync(long batchId);
        Task<bool> UpdateSoldOutStatusForAllBatchesAsync(IEnumerable<long> batchIds);
    }
}
