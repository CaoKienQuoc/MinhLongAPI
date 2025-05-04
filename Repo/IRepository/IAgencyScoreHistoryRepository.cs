using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.IRepository
{
    public interface IAgencyScoreHistoryRepository
    {
        Task AddAsync(AgencyScoreHistory history);
        Task<int> GetTotalScoreAsync(long agencyId);
    }

}
