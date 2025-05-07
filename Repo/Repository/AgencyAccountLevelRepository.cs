using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;

namespace Repo.Repository
{
    public class AgencyAccountLevelRepository : IAgencyAccountLevelRepository
    {
        private readonly MinhLongDbContext _context;

        public AgencyAccountLevelRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AgencyAccountLevel entity)
        {
            await _context.AgencyAccountLevels.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<AgencyAccountLevel?> GetLatestLevelByAgencyIdAsync(long agencyId)
        {
            return await _context.AgencyAccountLevels
                .Include(a => a.Level)
                .Where(a => a.AgencyId == agencyId)
                .OrderByDescending(a => a.ChangeDate)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(AgencyAccountLevel level)
        {
            _context.AgencyAccountLevels.Update(level);
            await Task.CompletedTask;
        }

        public async Task<AgencyLevel> GetLevelByIdAsync(long levelId)
        {
            return await _context.AgencyLevels.FirstOrDefaultAsync(x => x.LevelId == levelId);
        }
    }

}
