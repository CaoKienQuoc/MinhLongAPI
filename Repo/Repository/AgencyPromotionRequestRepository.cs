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

        public async Task<bool> IsPendingRequestExistAsync(long agencyId)
        {
            return await _context.AgencyPromotionRequest
                .AnyAsync(x => x.AgencyId == agencyId && x.Status == "Pending");
        }

        public async Task CreateAsync(AgencyPromotionRequest request)
        {
            _context.AgencyPromotionRequest.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task<AgencyPromotionRequest> GetByIdAsync(Guid id)
        {
            return await _context.AgencyPromotionRequest.FindAsync(id);
        }

        public async Task UpdateAsync(AgencyPromotionRequest request)
        {
            _context.AgencyPromotionRequest.Update(request);
            await _context.SaveChangesAsync();
        }

    }
}
