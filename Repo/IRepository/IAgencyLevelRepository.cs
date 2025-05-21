using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IAgencyLevelRepository
    {
        Task<IEnumerable<AgencyLevel>> GetAllAsync();
        Task<AgencyLevel?> GetByIdAsync(long id);
        Task AddAsync(AgencyLevel level);
        Task UpdateAsync(AgencyLevel level);
        Task DeleteAsync(long id);
        Task<int?> GetPaymentTermByLevelIdAsync(long levelId);
        Task<long> GetCurrentLevelByAgencyIdAsync(long agencyId);
        Task<AgencyLevel> GetLowestLevelAsync();
    }

}
