using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.Models;

namespace Services.IService
{
    public interface IBatchService
    {
        Task<Batch> GetBatchByIdAsync(long batchId);
        Task<IEnumerable<Batch>> GetAllBatchesAsync();
        Task<bool> UpdateBatchAsync(Batch batch);
        Task<IEnumerable<Batch>> GetBatchesByProductIdAsync(long productId);

        Task<(bool Success, string Message, object? Data)> UpdateProfitMarginAsync(long batchId, decimal profitMarginPercent);
        Task<ProductInfoByBatchDto?> GetProductInfoByBatchIdAsync(long batchId);

        Task<List<BatchDisplayDto>> GetBatchesByWarehouseIdAsync(long warehouseId);

        Task<int> UpdateExpiredBatchesAsync(DateTime nowVietnamTime);

    }
}
