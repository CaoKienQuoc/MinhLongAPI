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
    public class AgencyAccountRepository : IAgencyAccountRepository
    {
        private readonly MinhLongDbContext _context;

        public AgencyAccountRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<AgencyAccount> CreateAgencyAccountAsync(AgencyAccount account)
        {
            _context.AgencyAccounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<AgencyAccount> GetByUserIdAsync(Guid userId)
        {
            return await _context.AgencyAccounts.FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public async Task<AgencyAccount?> GetByUsernameAsync(string username)
        {
            return await _context.AgencyAccounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.User.Username == username);
        }

        public async Task<AgencyAccount?> GetByUserIdWithLevelsAsync(Guid userId)
        {
            return await _context.AgencyAccounts
                .Include(a => a.AgencyAccountLevels)
                    .ThenInclude(al => al.Level)
                .FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public async Task<int> CountManagedAgenciesAsync(long employeeId)
        {
            return await _context.AgencyAccounts
                .CountAsync(a => a.ManagedByEmployeeId == employeeId);
        }

        public async Task UpdateAsync(AgencyAccount agencyAccount)
        {
            _context.AgencyAccounts.Update(agencyAccount);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AgencyAccount>> GetAgenciesManagedByEmployeeIdAsync(long employeeId)
        {
            return await _context.AgencyAccounts
        .Where(a => a.ManagedByEmployeeId == employeeId)
        .Include(a => a.User)
        .Include(a => a.Address)
            .ThenInclude(addr => addr.Ward)
        .Include(a => a.Address)
            .ThenInclude(addr => addr.District)
        .Include(a => a.Address)
            .ThenInclude(addr => addr.Province)
        .ToListAsync();
        }

        public async Task<AgencyAccount> GetByIdAsync(long agencyId)
        {
            return await _context.AgencyAccounts
     .Include(a => a.AgencyAccountLevels)
     .FirstOrDefaultAsync(a => a.AgencyId == agencyId);

        }


        public async Task<List<AgencyAccount>> GetAllAsync()
        {
            return await _context.AgencyAccounts.ToListAsync();
        }
    }

}