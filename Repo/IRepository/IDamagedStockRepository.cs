using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
using BusinessObject.DTO.ReturnOrder;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IDamagedStockRepository
    {
        Task AddRangeAsync(List<DamagedStock> stocks);
        Task<IEnumerable<DamagedStockDto>> GetByWarehouseIdAsync(long warehouseId);

        Task AddAsync(DamagedStock damagedStock);
        Task SaveChangesAsync();

        Task<IEnumerable<GetDamagedStockDto>> GetByUserWarehouseAsync(Guid userId);
        Task<List<DamagedStock>> GetWithBatchInfoAsync(DateTime? startDate, DateTime? endDate);

        Task<List<DamagedStock>> GetAllAsync();

        Task<List<DamagedStock>> GetAllByWarehousesAsync(List<long> warehouseIds, int year);

    }

}
