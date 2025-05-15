using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;

namespace Services.Service
{
    public class BackGroundService : BackgroundService
    {
        /*private readonly IServiceScopeFactory _serviceScopeFactory;

        public BackGroundService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var batchService = scope.ServiceProvider.GetRequiredService<IBatchService>();

                    // ✅ Tính giờ Việt Nam (UTC+7)
                    var nowUtc = DateTime.UtcNow;
                    var nowVietnam = nowUtc.AddHours(7);

                    // ✅ Gọi hàm update expired batches
                    var updatedCount = await batchService.UpdateExpiredBatchesAsync(nowVietnam);
                    Console.WriteLine($"[{nowVietnam:yyyy-MM-dd HH:mm:ss}] Updated {updatedCount} expired batches.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.UtcNow.AddHours(7):yyyy-MM-dd HH:mm:ss}] Error updating expired batches: {ex.Message}");
                }

                // ✅ Đợi 5 phút trước khi kiểm tra lại
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }*/


        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BackGroundService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // ✅ Tính giờ Việt Nam (UTC+7)
            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

            // ✅ Tính thời điểm gửi debt reminder đầu tiên: 08:00 VN
            DateTime vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            DateTime nextDebtRun = new DateTime(vnNow.Year, vnNow.Month, vnNow.Day, 8, 0, 0, DateTimeKind.Unspecified);
            if (vnNow > nextDebtRun)
                nextDebtRun = nextDebtRun.AddDays(1);

            // 2) Vòng lặp chung
            while (!stoppingToken.IsCancellationRequested)
            {
                vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

                // 2.1) Cập nhật expired batches every 5 minutes
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();

                    //Batch
                    var batchService = scope.ServiceProvider.GetRequiredService<IBatchService>();

                    var updatedCount = await batchService.UpdateExpiredBatchesAsync(vnNow);
                    Console.WriteLine(
                        $"[{vnNow:yyyy-MM-dd HH:mm:ss}] Expired batches updated: {updatedCount}"
                    );

                    // 🔄 Cập nhật trạng thái ExpiredSoon nếu còn dưới 6 tháng
                    var expiredSoonCount = await batchService.UpdateExpiredSoonBatchesAsync(vnNow);
                    Console.WriteLine(
                        $"[{vnNow:yyyy-MM-dd HH:mm:ss}] ExpiredSoon batches updated: {expiredSoonCount}"
                    );

                    //WarehouseProduct
                    // ✅ WarehouseProduct Update
                    var warehouseProductService = scope.ServiceProvider.GetRequiredService<IWarehouseService>();
                    var expiredProductCount = await warehouseProductService.UpdateExpiredWarehouseProductsAsync(vnNow);
                    Console.WriteLine(
                        $"[{vnNow:yyyy-MM-dd HH:mm:ss}] Expired warehouse products updated: {expiredProductCount}");

                    var expiredSoonProductCount = await warehouseProductService.UpdateExpiredSoonWarehouseProductsAsync(vnNow);
                    Console.WriteLine(
                        $"[{vnNow:yyyy-MM-dd HH:mm:ss}] ExpiredSoon warehouse products updated: {expiredSoonProductCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[{vnNow:yyyy-MM-dd HH:mm:ss}] Error updating expired batches: {ex.Message}"
                    );
                }

                // 2.2) Gửi debt reminders vào 08:00 VN
                if (vnNow >= nextDebtRun)
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentHistoryRepository>();
                        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        var agencyLevelRepo = scope.ServiceProvider.GetRequiredService<IAgencyLevelRepository>();
                        var agencyScoreRepo = scope.ServiceProvider.GetRequiredService<IAgencyScoreHistoryRepository>();
                        var agencyRepo = scope.ServiceProvider.GetRequiredService<IAgencyAccountRepository>();

                        var payments = await paymentRepository.GetAllPaymentHistoryAsync();
                        foreach (var payment in payments)
                        {
                            var dueDate = payment.PaymentDate.AddMonths(3);
                            var daysLeft = (dueDate.Date - vnNow.Date).TotalDays;

                            // Gửi email nhắc nợ trước hạn 10 ngày
                            if (daysLeft == 10)
                            {
                                string cacheKey = $"DebtReminder:{payment.OrderId}:{vnNow:yyyy-MM-dd}";
                                if (!await cacheService.ExistsAsync(cacheKey))
                                {
                                    var email = payment.User?.Email;
                                    if (!string.IsNullOrEmpty(email))
                                    {
                                        var agency = await userRepo.GetAgencyAccountByUserIdAsync(payment.UserId);
                                        var order = await orderRepo.GetOrderByIdAsync(payment.OrderId);

                                        if (agency?.AgencyName is string agencyName && order?.OrderCode is string orderCode)
                                        {
                                            await emailService.SendEmailDebtReminderAsync(email, agencyName, orderCode, dueDate);
                                            await cacheService.SetAsync(cacheKey, true, TimeSpan.FromDays(1));
                                            Console.WriteLine($"[{vnNow:yyyy-MM-dd HH:mm:ss}] Sent debt reminder for order {orderCode}");
                                        }
                                    }
                                }
                            }

                            // ✅ Trừ điểm nếu quá hạn
                            if (vnNow.Date > dueDate.Date)
                            {
                                int overdueDays = (vnNow.Date - dueDate.Date).Days;
                                string penaltyKey = $"Penalty:{payment.OrderId}:{vnNow:yyyy-MM-dd}";

                                if (!await cacheService.ExistsAsync(penaltyKey))
                                {
                                    var agency = await userRepo.GetAgencyAccountByUserIdAsync(payment.UserId);
                                    var order = await orderRepo.GetOrderByIdAsync(payment.OrderId);

                                    if (agency != null)
                                    {
                                        var currentLevel = await agencyLevelRepo.GetCurrentLevelByAgencyIdAsync(agency.AgencyId);
                                        decimal deductionRate = currentLevel switch
                                        {
                                            3 => 1.0m,
                                            2 => 0.8m,
                                            1 => 0.5m,
                                            _ => 1.0m
                                        };

                                        int deductedScore = (int)(overdueDays * deductionRate);
                                        var reason = $"Trừ điểm vì quá hạn thanh toán đơn hàng #{order?.OrderCode} ({overdueDays} ngày)";

                                        var scoreEntry = new AgencyScoreHistory
                                        {
                                            AgencyId = agency.AgencyId,
                                            ScoreChange = -deductedScore,
                                            Reason = reason,
                                            CreatedDate = vnNow
                                        };

                                        await agencyScoreRepo.AddScoreAsync(scoreEntry);
                                        await agencyScoreRepo.SaveChangesAsync();

                                        // ✅ 2. Cập nhật tổng điểm vào bảng AgencyAccount
                                        agency.AgencyScore = scoreEntry.ScoreChange;
                                        await agencyRepo.UpdateAsync(agency);

                                        await cacheService.SetAsync(penaltyKey, true, TimeSpan.FromDays(1));
                                        Console.WriteLine($"[{vnNow:yyyy-MM-dd HH:mm:ss}] Trừ {deductedScore} điểm cho đại lý {agency.AgencyName} (order {order?.OrderCode})");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"[{vnNow:yyyy-MM-dd HH:mm:ss}] Error sending debt reminders: {ex.Message}"
                        );
                    }

                    // Lên lịch lần reminder kế tiếp vào 08:00 VN ngày mai
                    nextDebtRun = nextDebtRun.AddDays(1);
                }

                // 2.3) Tính khoảng chờ: min(5 phút, thời gian đến nextDebtRun)
                var timeToNextExpired = TimeSpan.FromMinutes(3);
                var timeToDebt = nextDebtRun - vnNow;
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
