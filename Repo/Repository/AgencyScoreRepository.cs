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
    public class AgencyScoreRepository : IAgencyScoreHistoryRepository
    {
        private readonly MinhLongDbContext _context;

        public AgencyScoreRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task AddScoreAsync(AgencyScoreHistory scoreEntry)
        {
            await _context.AgencyScoreHistory.AddAsync(scoreEntry);
        }

        public async Task<int> GetTotalScoreByAgencyIdAsync(long agencyId)
        {
            return await _context.AgencyScoreHistory
                .Where(s => s.AgencyId == agencyId)
                .SumAsync(s => s.ScoreChange);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<AgencyScoreHistory?> GetByAgencyIdAndReasonAsync(long agencyId, string reason)
        {
            return await _context.AgencyScoreHistory
                .FirstOrDefaultAsync(x => x.AgencyId == agencyId && x.Reason == reason);
        }

        public async Task UpdateAsync(AgencyScoreHistory history)
        {
            _context.AgencyScoreHistory.Update(history);
            await Task.CompletedTask;
        }

    }
}
