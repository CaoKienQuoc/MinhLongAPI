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
    public class ExpiredBatchBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ExpiredBatchBackgroundService(IServiceScopeFactory serviceScopeFactory)
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

                    var updatedCount = await batchService.UpdateExpiredBatchesAsync(nowVietnam);
                    Console.WriteLine($"[{nowVietnam:yyyy-MM-dd HH:mm:ss}] Updated {updatedCount} expired batches.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.UtcNow.AddHours(7):yyyy-MM-dd HH:mm:ss}] Error updating expired batches: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

}
