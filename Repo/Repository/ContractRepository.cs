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
    public class ContractRepository : IContractRepository
    {
        private readonly MinhLongDbContext _context;

        public ContractRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<Contract> AddAsync(Contract contract)
        {
            _context.Contract.Add(contract);
            await _context.SaveChangesAsync();
            return contract;
        }

        public async Task<List<Contract>> AddRangeAsync(List<Contract> contracts)
        {
            _context.Contract.AddRange(contracts);
            await _context.SaveChangesAsync();
            return contracts;
        }

        public async Task<List<Contract>> GetByAgencyIdAsync(long agencyId)
        {
            return await _context.Contract
                .Where(c => c.AgencyId == agencyId)
                .ToListAsync();
        }

        public async Task<List<RegisterAccountContract>> AddRangeRegisterContractsAsync(List<RegisterAccountContract> contracts)
        {
            _context.RegisterAccountContract.AddRange(contracts);
            await _context.SaveChangesAsync();
            return contracts;
        }

    }
}
