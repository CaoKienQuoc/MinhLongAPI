using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IWarehouseRequestExportRepository
    {
        Task<WarehouseRequestExport> CreateWarehouseRequestExportAsync(long warehouseId, int requestExportId);
        Task<RequestExport> GetRequestExportByIdAsync(int requestExportId);

        Task<WarehouseRequestExport> GetByIdAsync(int id);
        Task UpdateAsync(WarehouseRequestExport request);

        Task<List<WarehouseRequestExport>> GetByWarehouseAndRequestExportAsync(int warehouseId, int requestExportId);

        Task UpdateManyAsync(List<WarehouseRequestExport> requests);
        Task<List<WarehouseRequestExport>> GetByWarehouseIdAsync(long warehouseId);

        Task AddRangeAsync(IEnumerable<WarehouseRequestExport> entities);


    }
}
