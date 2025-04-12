using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IService
{
    public interface IInventoryService
    {
        Task DeductStockByWarehouseProductAsync(Guid orderId, long productId, long requiredQuantity);
    }

}
