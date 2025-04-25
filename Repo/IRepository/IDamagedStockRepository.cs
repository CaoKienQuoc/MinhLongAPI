using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IDamagedStockRepository
    {
        Task AddRangeAsync(List<DamagedStock> stocks);
    }

}
