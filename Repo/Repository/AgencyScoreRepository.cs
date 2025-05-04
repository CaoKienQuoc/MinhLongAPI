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

        public async Task AddAsync(AgencyScoreHistory history)
        {
            _context.AgencyScoreHistory.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetTotalScoreAsync(long agencyId)
        {
            return await _context.AgencyScoreHistory
                .Where(x => x.AgencyId == agencyId)
                .SumAsync(x => x.ScoreChange);
        }
    }
}
