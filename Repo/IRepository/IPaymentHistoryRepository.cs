using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IPaymentHistoryRepository
    {
        Task<PaymentHistory> GetByIdAsync(Guid id);
        Task<List<PaymentHistory>> GetAllAsync();
        Task<List<PaymentHistory>> GetPaymentHistoryByUserIdAsync(Guid userId);

        Task<decimal?> GetCreditLimitByUserIdAsync(Guid userId);
        Task<int?> GetPaymentTermByUserIdAsync(Guid userId);

        Task<decimal> GetTotalRemainingDebtAmountByUserIdAsync(Guid userId);

        Task<List<PaymentHistory>> GetAllPaymentHistoryAsync();

        Task<decimal> GetTotalPaymentAmountByUserIdAsync(Guid userId);
        Task<decimal> GetPaymentAmountByDateAsync(Guid userId, DateTime date);
        Task<decimal> GetPaymentAmountByMonthAsync(Guid userId, int year, int month);
        Task<decimal> GetRemainingDebtByUserIdAsync(Guid userId);

    }

}
