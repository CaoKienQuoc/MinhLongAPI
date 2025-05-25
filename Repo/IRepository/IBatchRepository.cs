using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.IRepository
{
    public interface IBatchRepository
    {
        Task<Batch?> GetLatestBatchByProductIdAsync(long productId);
        Task<Batch> GetByIdAsync(long batchId);
        Task<IEnumerable<Batch>> GetAllAsync();
        Task<bool> UpdateAsync(Batch batch);
        Task<IEnumerable<Batch>> GetBatchesByProductIdAsync(long productId);
        Task<int> CountBatchesByDateAsync(DateTime date);
        Task<bool> UpdateBatchAndRelatedDataAsync(Batch batch);
        Task<Product?> GetProductByIdAsync(long productId);

        Task AddAsync(Batch batch);

        Task SaveChangesAsync();
        Task<long> GetWarehouseIdByBatchIdAsync(long batchId);

        Task<List<Batch>> GetBatchesByWarehouseIdAsync(long warehouseId);

        IQueryable<Batch> GetQueryable();
        Task UpdateRangeAsync(IEnumerable<Batch> batches);

        Task<Batch> GetExpiredBatchByIdAsync(long batchId);

        Task DeleteAsync(Batch batch);

        Task<bool> UpdateSoldOutStatusAsync(long batchId);

        Task UpdateBatchAsync(Batch batch);

        Task<List<Batch>> GetBatchesByIdsAsync(List<long> batchIds);
        Task<List<Batch>> GetExpiredSoonBatchesByWarehouseAsync(long warehouseId);

        Task<List<Batch>> GetExpiredSoonBatchesAsync();

        Task<List<Batch>> GetExpiredSoonBatchesByCategoryAsync(long categoryId);
        Task<List<Batch>> GetExpiredSoonBatchesByProductIdAsync(long productId);

        Task<Dictionary<long, decimal>> GetHighestSellingPricesByProductIdsAsync(List<long> productIds);
    }
}

