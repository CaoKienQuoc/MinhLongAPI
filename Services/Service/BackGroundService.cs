using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class BackGroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BackGroundService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 1) Tính thời điểm gửi debt reminder đầu tiên: 08:00 VN
            var nowVn = DateTime.UtcNow.AddHours(7);
            var nextDebtRun = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, 8, 0, 0);
            if (nowVn > nextDebtRun)
                nextDebtRun = nextDebtRun.AddDays(1);

            // 2) Vòng lặp chung
            while (!stoppingToken.IsCancellationRequested)
            {
                var vietnamNow = DateTime.UtcNow.AddHours(7);

                // 2.1) Cập nhật expired batches every 5 minutes
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var batchService = scope.ServiceProvider.GetRequiredService<IBatchService>();

                    var updatedCount = await batchService.UpdateExpiredBatchesAsync(vietnamNow);
                    Console.WriteLine(
                        $"[{vietnamNow:yyyy-MM-dd HH:mm:ss}] Expired batches updated: {updatedCount}"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[{vietnamNow:yyyy-MM-dd HH:mm:ss}] Error updating expired batches: {ex.Message}"
                    );
                }

                // 2.2) Gửi debt reminders vào 08:00 VN
                if (vietnamNow >= nextDebtRun)
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentHistoryRepository>();
                        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        //var payments = await paymentRepository.GetAllPaymentHistoryAsync();
                        var payments = (await paymentRepository.GetAllPaymentHistoryAsync())
                                         .Where(p => p.Status != "CANCELLED")
                                        .ToList();
                        foreach (var payment in payments)
                        {
                            var dueDate = payment.PaymentDate.AddMonths(3);
                            var daysLeft = (dueDate.Date - vietnamNow.Date).TotalDays;

                            if (daysLeft == 10)
                            {
                                string cacheKey = $"DebtReminder:{payment.OrderId}:{vietnamNow:yyyy-MM-dd}";
                                if (!await cacheService.ExistsAsync(cacheKey))
                                {
                                    var email = payment.User?.Email;
                                    if (string.IsNullOrEmpty(email))
                                        continue;

                                    var agency = await userRepo.GetAgencyAccountByUserIdAsync(payment.UserId);
                                    var order = await orderRepo.GetOrderByIdAsync(payment.OrderId);

                                    if (agency?.AgencyName is string agencyName
                                        && order?.OrderCode is string orderCode)
                                    {
                                        await emailService.SendEmailDebtReminderAsync(
                                            email, agencyName, orderCode, dueDate
                                        );
                                        await cacheService.SetAsync(cacheKey, true, TimeSpan.FromDays(1));
                                    }
                                }
                            }
                        }

                        Console.WriteLine(
                            $"[{vietnamNow:yyyy-MM-dd HH:mm:ss}] Debt reminders sent."
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"[{vietnamNow:yyyy-MM-dd HH:mm:ss}] Error sending debt reminders: {ex.Message}"
                        );
                    }

                    // Lên lịch lần reminder kế tiếp vào 08:00 VN ngày mai
                    nextDebtRun = nextDebtRun.AddDays(1);
                }

                // 2.3) Tính khoảng chờ: min(5 phút, thời gian đến nextDebtRun)
                var timeToNextExpired = TimeSpan.FromMinutes(5);
                var timeToDebt = nextDebtRun - vietnamNow;
                var delay = timeToDebt < timeToNextExpired ? timeToDebt : timeToNextExpired;

                // Tránh delay quá ngắn (<30s)
                if (delay < TimeSpan.FromSeconds(30))
                    delay = TimeSpan.FromSeconds(30);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Ứng dụng đang dừng, thoát nhẹ nhàng
                    break;
                }
            }
        }
    }

}
