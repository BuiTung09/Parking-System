using Microsoft.EntityFrameworkCore;
using ParkingSystem.Models;

namespace ParkingSystem.Services
{
    public class MonthlyPassExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonthlyPassExpirationService> _logger;

        public MonthlyPassExpirationService(IServiceProvider serviceProvider, ILogger<MonthlyPassExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MonthlyPassExpirationService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // Cập nhật trạng thái những vé đã hết hạn
                        var expiredPasses = await context.MonthlyPasses
                            .Where(m => m.Status == "Active" && m.EndDate < DateTime.Now)
                            .ToListAsync(stoppingToken);

                        if (expiredPasses.Any())
                        {
                            foreach (var pass in expiredPasses)
                            {
                                pass.Status = "Expired";
                            }
                            await context.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"[BACKGROUND SERVICE] Đã tự động cập nhật {expiredPasses.Count} vé tháng sang trạng thái Hết hạn (Expired).");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét vé tháng hết hạn.");
                }

                // Chờ 1 giờ rồi chạy lại
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
