using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.Repository
{
    public class AgencyPromotionRequestRepository : IAgencyPromotionRequestRepository
    {
        private readonly MinhLongDbContext _context;

        public AgencyPromotionRequestRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AgencyPromotionRequest request)
        {
            await _context.AgencyPromotionRequest.AddAsync(request);
        }

        public async Task<bool> HasPendingRequestAsync(long agencyId, long suggestedLevelId)
        {
            return await _context.AgencyPromotionRequest.AnyAsync(x =>
                x.AgencyId == agencyId &&
                x.SuggestedLevelId == suggestedLevelId &&
                x.Status == "Pending");
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<AgencyPromotionRequest> GetByIdAsync(Guid requestId)
        {
            return await _context.AgencyPromotionRequest
                .FirstOrDefaultAsync(x => x.AgencyPromotionRequestId == requestId);
        }

        public async Task UpdateAsync(AgencyPromotionRequest request)
        {
            _context.AgencyPromotionRequest.Update(request);
            await Task.CompletedTask;
        }

        public async Task<List<AgencyPromotionRequest>> GetRequestsByManagedEmployeeAsync(long employeeId)
        {
            return await _context.AgencyPromotionRequest
                .Include(x => x.Agency)
                .Where(x => x.Agency.ManagedByEmployeeId == employeeId)
                .ToListAsync();
        }

        public async Task<AgencyPromotionRequest?> GetRequestByIdManagedAsync(Guid requestId, long employeeId)
        {
            return await _context.AgencyPromotionRequest
                .Include(x => x.Agency)
                .FirstOrDefaultAsync(x =>
                    x.AgencyPromotionRequestId == requestId &&
                    x.Agency.ManagedByEmployeeId == employeeId);
        }


    }
}
