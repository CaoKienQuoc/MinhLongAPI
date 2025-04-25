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
    public class PaymentHistoryRepository : IPaymentHistoryRepository
    {
        private readonly MinhLongDbContext _context;

        public PaymentHistoryRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentHistory> GetByIdAsync(Guid id)
        {
            return await _context.PaymentHistories
                .Include(ph => ph.Order)
                    .ThenInclude(o => o.RequestProduct)
                        .ThenInclude(rp => rp.AgencyAccount)
                        .Include(ph => ph.PaymentTransactions)
                .FirstOrDefaultAsync(ph => ph.PaymentHistoryId == id);
        }

        public async Task<List<PaymentHistory>> GetAllPaymentHistoryAsync()
        {
            return await _context.PaymentHistories
                .Include(ph => ph.Order)
                    .ThenInclude(o => o.RequestProduct)
                        .ThenInclude(rp => rp.AgencyAccount)
                .Include(ph => ph.PaymentTransactions)
                .Include(ph => ph.User) // ✅ Include user trực tiếp
                    .ThenInclude(u => u.AgencyAccount) // ✅ Nếu UserType là AGENCY
                .ToListAsync();
        }

        public async Task<List<PaymentHistory>> GetAllAsync()
        {
            return await _context.PaymentHistories
                .Include(ph => ph.Order)
                    .ThenInclude(o => o.RequestProduct)
                        .ThenInclude(rp => rp.AgencyAccount)
                        .Include(ph => ph.PaymentTransactions)
                .ToListAsync();
        }

        public async Task<List<PaymentHistory>> GetPaymentHistoryByUserIdAsync(Guid userId)
        {
            return await _context.PaymentHistories
                .Include(ph => ph.Order)
                    .ThenInclude(o => o.RequestProduct)
                        .ThenInclude(rp => rp.AgencyAccount)
                        .Include(ph => ph.PaymentTransactions)
                .Where(ph => ph.UserId == userId)
                .ToListAsync();
        }

        public async Task<decimal?> GetCreditLimitByUserIdAsync(Guid userId)
        {
            var creditLimit = await _context.AgencyAccountLevels
                .Include(aal => aal.Level)
                .Include(aal => aal.Agency)
                .Where(aal => aal.Agency.UserId == userId)
                .OrderByDescending(aal => aal.ChangeDate) // Nếu có nhiều cấp, lấy mới nhất
                .Select(aal => aal.Level.CreditLimit)
                .FirstOrDefaultAsync();

            return creditLimit;
        }

        public async Task<int?> GetPaymentTermByUserIdAsync(Guid userId)
        {
            return await _context.AgencyAccounts
                .Where(aa => aa.UserId == userId)
                .SelectMany(aa => aa.AgencyAccountLevels)
                .OrderByDescending(aal => aal.ChangeDate)
                .Select(aal => aal.Level.PaymentTerm)
                .FirstOrDefaultAsync();
        }

        public async Task<decimal> GetTotalRemainingDebtAmountByUserIdAsync(Guid userId)
        {
            return await _context.PaymentHistories
                .Where(ph => ph.UserId == userId)
                .SumAsync(ph => ph.RemainingDebtAmount);
        }


        public async Task<decimal> GetTotalPaymentAmountByUserIdAsync(Guid userId)
        {
            return await _context.PaymentHistories
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.PaymentAmount);
        }

        public async Task<decimal> GetPaymentAmountByDateAsync(Guid userId, DateTime date)
        {
            return await _context.PaymentHistories
                .Where(p => p.UserId == userId && p.PaymentDate.Date == date.Date)
                .SumAsync(p => p.PaymentAmount);
        }

        public async Task<decimal> GetPaymentAmountByMonthAsync(Guid userId, int year, int month)
        {
            return await _context.PaymentHistories
                .Where(p => p.UserId == userId && p.PaymentDate.Year == year && p.PaymentDate.Month == month)
                .SumAsync(p => p.PaymentAmount);
        }

        public async Task<decimal> GetRemainingDebtByUserIdAsync(Guid userId)
        {
            return await _context.PaymentHistories
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.RemainingDebtAmount);
        }

    }
}
