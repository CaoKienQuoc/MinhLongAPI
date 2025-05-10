using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            // Tính thời điểm gửi debt reminder đầu tiên: 08:00 VN
            var nowVn = DateTime.UtcNow.AddHours(7);
            var nextDebtRun = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, 8, 0, 0);
            if (nowVn > nextDebtRun) nextDebtRun = nextDebtRun.AddDays(1);

            // Chạy vòng lặp chung
            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;
                var vietnamNow = utcNow.AddHours(7);

                // 1) Cập nhật expired batches every 5 minutes
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var batchService = scope.ServiceProvider.GetRequiredService<IBatchService>();
                    var updatedCount = await batchService.UpdateExpiredBatchesAsync(vietnamNow);
                    Console.WriteLine($"[{vietnamNow:yyyy-MM-dd HH:mm:ss}] Expired batches updated: {updatedCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{vietnamNow:yyyy-MM-dd HH:mm:ss}] Error updating expired batches: {ex.Message}");
                }

                // 2) Gửi debt reminders at 08:00 VN
                if (vietnamNow >= nextDebtRun)
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentHistoryService>();
                        await paymentService.SendDebtRemindersAsync();
                        Console.WriteLine($"[{vietnamNow:yyyy-MM-dd HH:mm:ss}] Debt reminders sent.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{vietnamNow:yyyy-MM-dd HH:mm:ss}] Error sending debt reminders: {ex.Message}");
                    }

                    // Lên lịch lần reminder kế tiếp vào 08:00 VN ngày mai
                    nextDebtRun = nextDebtRun.AddDays(1);
                }

                // Tính khoảng chờ: min(5 phút, thời gian đến nextDebtRun)
                var timeToNextExpired = TimeSpan.FromMinutes(5);
                var timeToDebt = nextDebtRun - vietnamNow;
                var delay = timeToDebt < timeToNextExpired ? timeToDebt : timeToNextExpired;

                if (delay < TimeSpan.FromSeconds(30))
                    delay = TimeSpan.FromSeconds(30);   // tránh vòng quá nhanh

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // shutdown
                }
            }
        }

    }

}
