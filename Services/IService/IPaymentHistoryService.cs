using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.PaymentDTO;
using BusinessObject.Models;

namespace Services.IService
{
    public interface IPaymentHistoryService
    {
        Task<PaymentHistoryDto> GetPaymentHistoryByIdAsync(Guid id);
        Task<List<PaymentHistoryDto>> GetAllPaymentHistoriesAsync();
        Task<List<PaymentHistoryDto>> GetPaymentHistoriesByUserIdAsync(Guid userId);
        Task SendDebtRemindersAsync();

        Task<decimal> GetTotalPaidAsync(Guid userId);
        Task<decimal> GetTodayPaidAsync(Guid userId);
        Task<decimal> GetMonthPaidAsync(Guid userId);
        Task<decimal> GetRemainingDebtAsync(Guid userId);
    }
}
