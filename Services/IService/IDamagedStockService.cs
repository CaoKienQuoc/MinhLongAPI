using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
using BusinessObject.DTO.ReturnOrder;
using BusinessObject.Models;

namespace Services.IService
{
    public interface IDamagedStockService
    {
        Task<IEnumerable<DamagedStockDto>> GetByWarehouseIdAsync(long warehouseId);
        Task ImportToDamagedStockAsync(long receiptId, Guid userId); // bạn đã có

        Task<IEnumerable<GetDamagedStockDto>> GetByUserWarehouseAsync(Guid userId);
        Task<object> GetTotalByStatusAndDateAsync(DateTime? startDate, DateTime? endDate);

    }
}
