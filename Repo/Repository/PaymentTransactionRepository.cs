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
    public class PaymentTransactionRepository : IPaymentTransactionRepository
    {
        private readonly MinhLongDbContext _context;

        public PaymentTransactionRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PaymentTransaction>> GetAllAsync()
        {
            return await _context.PaymentTransactions
                .Include(pt => pt.PaymentHistory)
                .OrderByDescending(re => re.PaymentDate)
                .ToListAsync();
        }

        public async Task<PaymentTransaction> GetByIdAsync(Guid id)
        {
            return await _context.PaymentTransactions
                .Include(pt => pt.PaymentHistory)
                .FirstOrDefaultAsync(pt => pt.TransactionId == id);
        }

        public async Task<IEnumerable<PaymentTransaction>> GetByUserIdAsync(Guid userId)
        {
            return await _context.PaymentTransactions
                .Include(pt => pt.PaymentHistory)
                .Where(pt => pt.PaymentHistory.UserId == userId)
                .ToListAsync();
        }

    }

}
