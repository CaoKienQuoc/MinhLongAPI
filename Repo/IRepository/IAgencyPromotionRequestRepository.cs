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
        Task<bool> IsPendingRequestExistAsync(long agencyId);
        Task CreateAsync(AgencyPromotionRequest request);
        Task<AgencyPromotionRequest> GetByIdAsync(Guid id);
        Task UpdateAsync(AgencyPromotionRequest request);
    }
}
