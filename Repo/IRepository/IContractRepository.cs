using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IContractRepository
    {
        Task<Contract> AddAsync(Contract contract);
        Task<List<Contract>> AddRangeAsync(List<Contract> contracts);
        Task<List<Contract>> GetByAgencyIdAsync(long agencyId);

        Task<List<RegisterAccountContract>> AddRangeRegisterContractsAsync(List<RegisterAccountContract> contracts);
    }
}
