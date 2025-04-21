using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IWarehouseTransferRepository
    {
        Task<int> SaveChangesAsync();
        Task AddRangeAsync(IEnumerable<WarehouseTransferRequest> requests);
        Task<WarehouseTransferRequest?> GetByIdAsync(int id);
        Task UpdateAsync(WarehouseTransferRequest request);

        Task<List<WarehouseTransferRequest>> GetAllByUserIdAsync(Guid userId);
        Task<WarehouseTransferRequest?> GetByIdAndUserIdAsync(long id, Guid userId);

    }
}
