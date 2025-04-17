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
        Task<WarehouseTransferRequest> CreateAsync(WarehouseTransferRequest request);
        Task<List<WarehouseTransferRequest>> GetAllAsync();
        Task<WarehouseTransferRequest?> GetByIdAsync(long id);

        Task<bool> PlanTransferRequestAsync(long requestId, long sourceWarehouseId, Guid plannerId);
        Task<List<WarehouseTransferRequest>> GetPlannedRequestsByWarehouseAsync(long sourceWarehouseId);
        Task<RequestExport?> GetRequestExportWithOrderAsync(int requestExportId);
        Task<WarehouseTransferRequest?> GetByIdWithProductsAsync(long id);
        Task UpdateAsync(WarehouseTransferRequest request);

        Task<List<WarehouseTransferRequest>> GetBySourceWarehouseAsync(long sourceWarehouseId);

        Task<List<WarehouseTransferRequest>> GetByDestinationWarehouseAsync(long destinationWarehouseId);

        Task AddRangeAsync(IEnumerable<WarehouseTransferRequest> transfers);


    }
}
