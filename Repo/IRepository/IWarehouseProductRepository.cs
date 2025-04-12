using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IWarehouseProductRepository
    {
        Task<List<WarehouseProduct>> GetAvailableWarehouseProductsAsync(long productId);
        Task UpdateAsync(WarehouseProduct entity);
        Task SaveChangesAsync();

        Task<long> GetTotalAvailableStockByProductIdAsync(long productId);
    }

}
