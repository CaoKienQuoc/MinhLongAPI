using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IAgencyAccountRepository
    {
        Task<AgencyAccount> CreateAgencyAccountAsync(AgencyAccount account);
        Task<AgencyAccount> GetByUserIdAsync(Guid userId);
        Task<AgencyAccount?> GetByUsernameAsync(string username);
        Task<AgencyAccount?> GetByUserIdWithLevelsAsync(Guid userId);

        Task<int> CountManagedAgenciesAsync(long employeeId);
        Task UpdateAsync(AgencyAccount agencyAccount);

        Task<List<AgencyAccount>> GetAgenciesManagedByEmployeeIdAsync(long employeeId);

        Task<AgencyAccount> GetByIdAsync(long agencyId);
        Task<List<AgencyAccount>> GetAllAsync();
    }

}