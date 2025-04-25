using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.PaymentDTO;
using BusinessObject.Models;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;

namespace Services.Service
{
    public class PaymentHistoryService : IPaymentHistoryService
    {
        private readonly IPaymentHistoryRepository _repository;
        private readonly ICacheService _cacheService;
        private readonly IEmailService _mailService;
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;

        public PaymentHistoryService(IPaymentHistoryRepository repository, ICacheService cacheService, IEmailService mailService, IUserRepository userRepository, IOrderRepository orderRepository)
        {
            _repository = repository;
            _cacheService = cacheService;
            _mailService = mailService;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
        }

        public async Task<PaymentHistoryDto> GetPaymentHistoryByIdAsync(Guid id)
        {
            var payment = await _repository.GetByIdAsync(id);

            if (payment == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy lịch sử thanh toán với ID = {id}");
            }

            var dueDate = payment.CreatedAt.AddMonths(3);
            return new PaymentHistoryDto
            {
                PaymentHistoryId = payment.PaymentHistoryId,
                OrderId = payment.OrderId,
                OrderCode = payment.Order?.OrderCode ?? "N/A",

                AgencyId = payment.Order?.RequestProduct?.AgencyId ?? 0,
                AgencyName = payment.Order?.RequestProduct?.AgencyAccount?.AgencyName ?? "Unknown",

                PaymentMethod = payment.PaymentMethod,
                PaymentDate = payment.PaymentDate,
                SerieNumber = payment.SerieNumber,
                Status = payment.Status,
                TotalAmountPayment = payment.TotalAmountPayment,
                RemainingDebtAmount = payment.RemainingDebtAmount,
                PaymentAmount = payment.PaymentAmount,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt,
                TransactionReference = payment.PaymentTransactions?.FirstOrDefault()?.TransactionReference ?? "N/A", // 👈 Lấy TransactionReference
                DueDate = dueDate,
                DebtStatus = GetDebtStatus(dueDate)
            };
        }


        public async Task<List<PaymentHistoryDto>> GetAllPaymentHistoriesAsync()
        {
            var histories = await _repository.GetAllAsync();

            return histories.Select(ph =>
            {
                var dueDate = ph.CreatedAt.AddMonths(3);
                return new PaymentHistoryDto
                {
                    PaymentHistoryId = ph.PaymentHistoryId,
                    OrderId = ph.OrderId,
                    OrderCode = ph.Order?.OrderCode ?? "N/A",
                    AgencyId = ph.Order?.RequestProduct?.AgencyId ?? 0,
                    AgencyName = ph.Order?.RequestProduct?.AgencyAccount?.AgencyName ?? "Unknown",
                    PaymentMethod = ph.PaymentMethod,
                    PaymentDate = ph.PaymentDate,
                    SerieNumber = ph.SerieNumber,
                    Status = ph.Status,
                    TotalAmountPayment = ph.TotalAmountPayment,
                    RemainingDebtAmount = ph.RemainingDebtAmount,
                    PaymentAmount = ph.PaymentAmount,
                    CreatedAt = ph.CreatedAt,
                    UpdatedAt = ph.UpdatedAt,
                    TransactionReference = ph.PaymentTransactions?.FirstOrDefault()?.TransactionReference ?? "N/A",
                    DueDate = dueDate,
                    DebtStatus = GetDebtStatus(dueDate)
                };
            }).ToList();
        }

        public async Task<List<PaymentHistoryDto>> GetPaymentHistoriesByUserIdAsync(Guid userId)
        {
            var payments = await _repository.GetPaymentHistoryByUserIdAsync(userId);

            return payments.Select(ph =>
            {
                var dueDate = ph.CreatedAt.AddMonths(3);
                return new PaymentHistoryDto
                {
                    PaymentHistoryId = ph.PaymentHistoryId,
                    OrderId = ph.OrderId,
                    OrderCode = ph.Order?.OrderCode ?? "N/A",
                    AgencyId = ph.Order?.RequestProduct?.AgencyId ?? 0,
                    AgencyName = ph.Order?.RequestProduct?.AgencyAccount?.AgencyName ?? "Unknown",
                    PaymentMethod = ph.PaymentMethod,
                    PaymentDate = ph.PaymentDate,
                    SerieNumber = ph.SerieNumber,
                    Status = ph.Status,
                    TotalAmountPayment = ph.TotalAmountPayment,
                    RemainingDebtAmount = ph.RemainingDebtAmount,
                    PaymentAmount = ph.PaymentAmount,
                    CreatedAt = ph.CreatedAt,
                    UpdatedAt = ph.UpdatedAt,
                    TransactionReference = ph.PaymentTransactions?.FirstOrDefault()?.TransactionReference ?? "N/A",
                    DueDate = dueDate,
                    DebtStatus = GetDebtStatus(dueDate)
                };
            }).ToList();
        }

        private string GetDebtStatus(DateTime dueDate)
        {
            var daysLeft = (dueDate - DateTime.UtcNow).TotalDays;

            if (daysLeft > 10)
                return "StillValid";
            else if (daysLeft <= 10 && daysLeft >= 0)
                return "NearDue";
            else
                return "OverDue";
        }

        public async Task SendDebtRemindersAsync()
        {
            var payments = await _repository.GetAllPaymentHistoryAsync(); // Đã include User (Email)

            foreach (var payment in payments)
            {
                var dueDate = payment.PaymentDate.AddMonths(3);
                //var dueDate = new DateTime(2025, 4, 15);
                var daysLeft = (dueDate - DateTime.UtcNow.Date).TotalDays;

                if (daysLeft <= 10 && daysLeft >= 0)
                {
                    string cacheKey = $"DebtReminder:{payment.OrderId}:{DateTime.UtcNow:yyyy-MM-dd}";
                    if (!await _cacheService.ExistsAsync(cacheKey))
                    {
                        // 🔥 Lấy Email từ bảng User
                        var email = payment.User?.Email;
                        if (string.IsNullOrEmpty(email) || payment.UserId == Guid.Empty)
                            continue;

                        // 🔥 Truy vấn AgencyAccount để lấy AgencyName theo UserId
                        var agencyAccount = await _userRepository.GetAgencyAccountByUserIdAsync(payment.UserId);
                        var agencyName = agencyAccount?.AgencyName;

                        var order = await _orderRepository.GetOrderByIdAsync(payment.OrderId);
                        var orderCode = order?.OrderCode;


                        if (!string.IsNullOrEmpty(agencyName))
                        {
                            await _mailService.SendEmailDebtReminderAsync(
                                email,
                                agencyName,
                                orderCode,
                                dueDate
                            );

                            await _cacheService.SetAsync(cacheKey, true, TimeSpan.FromDays(1));
                        }
                    }
                }
            }
        }

        public Task<decimal> GetTotalPaidAsync(Guid userId) =>
        _repository.GetTotalPaymentAmountByUserIdAsync(userId);

        public Task<decimal> GetTodayPaidAsync(Guid userId) =>
            _repository.GetPaymentAmountByDateAsync(userId, DateTime.Today);

        public Task<decimal> GetMonthPaidAsync(Guid userId)
        {
            var now = DateTime.Now;
            return _repository.GetPaymentAmountByMonthAsync(userId, now.Year, now.Month);
        }

        public Task<decimal> GetRemainingDebtAsync(Guid userId) =>
            _repository.GetRemainingDebtByUserIdAsync(userId);

    }
}
