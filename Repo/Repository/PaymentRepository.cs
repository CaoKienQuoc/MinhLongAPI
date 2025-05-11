using BusinessObject.DTO.PaymentDTO;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Repo.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repo.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly MinhLongDbContext _context;
        private readonly PayOSSettings _payOSSettings;

        public PaymentRepository(MinhLongDbContext context, IOptions<PayOSSettings> payOSSettings)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _payOSSettings = payOSSettings.Value ?? throw new ArgumentNullException(nameof(payOSSettings));
        }
        public async Task<List<PaymentHistory>> GetPaymentsByOrderIdAsync(Guid orderId)
        {
            return await _context.PaymentHistories
                                 .Where(p => p.OrderId == orderId)
                                 .ToListAsync();
        }

        public async Task AddPaymentHistoryAsync(PaymentHistory paymentHistory)
        {
            await _context.PaymentHistories.AddAsync(paymentHistory);
        }

        public async Task AddPaymentTransactionAsync(PaymentTransaction paymentTransaction)
        {
            await _context.PaymentTransactions.AddAsync(paymentTransaction);
        }

        public async Task<string> GetPaymentDetailsAsync(long orderCode)
        {
            var checkPaymentUrl = $"https://api.payos.vn/v1/payments/{orderCode}";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_payOSSettings.ApiKey}");

            var response = await httpClient.GetAsync(checkPaymentUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var paymentInfo = JsonConvert.DeserializeObject<PayOSPaymentResponse>(responseContent);
                return paymentInfo.CheckoutUrl; // Trả về URL QR Code
            }

            throw new Exception("Không thể lấy thông tin thanh toán từ PayOS.");
        }


        public async Task<Order> GetOrderByIdAsync(Guid orderId)
        {
            return await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // 1. Lấy PaymentHistory theo OrderId
        public async Task<PaymentHistory?> GetPaymentHistoryByOrderIdAsync(Guid orderId)
        {
            return await _context.PaymentHistories
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
        }

        // 2. Thêm mới PaymentHistory
        public async Task InsertPaymentHistoryAsync(PaymentHistory history)
        {
            await _context.PaymentHistories.AddAsync(history);
        }

        // 3. Cập nhật PaymentHistory
        public async Task UpdatePaymentHistoryAsync(PaymentHistory history)
        {
            _context.PaymentHistories.Update(history);
        }

        // 4. Thêm giao dịch vào PaymentTransaction
        public async Task InsertPaymentTransactionAsync(PaymentTransaction transaction)
        {
            await _context.PaymentTransactions.AddAsync(transaction);
        }

        public async Task<PaymentTransaction?> GetTransactionByReferenceAsync(Guid paymentHistoryId)
        {
            return await _context.PaymentTransactions
                                 .FirstOrDefaultAsync(t => t.PaymentHistoryId == paymentHistoryId);
        }

            public async Task<AgencyAccountLevel?> GetAgencyAccountLevelByAgencyIdAsync(long agencyId)
            {
                return await _context.AgencyAccountLevels
                                     .FirstOrDefaultAsync(a => a.AgencyId == agencyId);
            }


        public async Task UpdateAgencyAccountLevelAsync(AgencyAccountLevel level)
        {
            _context.AgencyAccountLevels.Update(level);
        }

    }

}
