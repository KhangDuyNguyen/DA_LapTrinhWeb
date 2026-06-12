using Microsoft.EntityFrameworkCore;
using AuctionSystem.Models;

namespace AuctionSystem.Services
{
    public class AuctionBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuctionBackgroundService> _logger;

        public AuctionBackgroundService(IServiceProvider serviceProvider, ILogger<AuctionBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AuctionBackgroundService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAuctions();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AuctionBackgroundService");
                }

                // Chạy mỗi 30 giây
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task ProcessAuctions()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.Now;

            // Kích hoạt phiên đang chờ
            var pendingToActive = await context.AuctionItems
                .Where(a => a.Status == AuctionStatus.Pending && a.StartTime <= now && a.EndTime > now)
                .ToListAsync();

            foreach (var auction in pendingToActive)
            {
                auction.Status = AuctionStatus.Active;
                _logger.LogInformation($"Auction {auction.Id} activated: {auction.Title}");
            }

            // Đóng phiên hết hạn
            var expiredAuctions = await context.AuctionItems
                .Include(a => a.Winner)
                .Include(a => a.CreatedByUser)
                .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
                .ToListAsync();

            foreach (var auction in expiredAuctions)
            {
                auction.Status = AuctionStatus.Ended;
                _logger.LogInformation($"Auction {auction.Id} ended: {auction.Title}");

                // Gửi email thông báo
                if (auction.Winner != null && !string.IsNullOrEmpty(auction.Winner.Email))
                {
                    await emailService.SendWinnerEmailAsync(
                        auction.Winner.Email,
                        auction.Winner.FullName,
                        auction.Title,
                        auction.CurrentPrice
                    );
                }

                // Email thông báo cho người tạo
                if (auction.CreatedByUser != null && !string.IsNullOrEmpty(auction.CreatedByUser.Email))
                {
                    await emailService.SendAuctionEndedEmailAsync(
                        auction.CreatedByUser.Email,
                        auction.CreatedByUser.FullName,
                        auction.Title,
                        auction.CurrentPrice,
                        auction.Winner?.FullName
                    );
                }
            }

            if (pendingToActive.Any() || expiredAuctions.Any())
                await context.SaveChangesAsync();
        }
    }
}
