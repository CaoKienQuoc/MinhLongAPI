using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.IRepository
{
    public interface IAgencyPromotionRequestRepository
    {
        Task AddAsync(AgencyPromotionRequest request);
        Task<bool> HasPendingRequestAsync(long agencyId, long suggestedLevelId);
        Task SaveChangesAsync();

        Task<AgencyPromotionRequest> GetByIdAsync(Guid requestId);
        Task UpdateAsync(AgencyPromotionRequest request);

        Task<List<AgencyPromotionRequest>> GetRequestsByManagedEmployeeAsync(long employeeId);
        Task<AgencyPromotionRequest?> GetRequestByIdManagedAsync(Guid requestId, long employeeId);
    }
}
