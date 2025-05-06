using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IAgencyAccountLevelRepository
    {
        Task AddAsync(AgencyAccountLevel entity);
        Task SaveAsync(); // nếu cần
        Task<AgencyAccountLevel?> GetLatestLevelByAgencyIdAsync(long agencyId);

        Task UpdateAsync(AgencyAccountLevel level);

    }
}
