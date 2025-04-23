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
    }

}
