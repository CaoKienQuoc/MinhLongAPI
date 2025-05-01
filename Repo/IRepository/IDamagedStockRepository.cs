using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }

}
