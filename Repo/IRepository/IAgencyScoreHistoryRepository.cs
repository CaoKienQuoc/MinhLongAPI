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
        Task AddScoreAsync(AgencyScoreHistory history);
        Task<decimal> GetTotalScoreByAgencyIdAsync(long agencyId);
        Task<AgencyScoreHistory?> GetByAgencyIdAndReasonAsync(long agencyId, string reason);
        Task UpdateAsync(AgencyScoreHistory history);
        Task SaveChangesAsync();

        Task<List<AgencyScoreHistory>> GetHistoryByAgencyIdAsync(long agencyId);

    }

}
