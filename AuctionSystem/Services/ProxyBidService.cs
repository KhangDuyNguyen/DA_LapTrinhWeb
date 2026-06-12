using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AuctionSystem.Models;
using AuctionSystem.Hubs;

namespace AuctionSystem.Services
{
    public interface IProxyBidService
    {
        Task ProcessProxyBidsAsync(int auctionId, string lastBidderId, decimal currentPrice);
    }

    public class ProxyBidService : IProxyBidService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<AuctionHub> _hubContext;
        private readonly IEmailService _emailService;
        private readonly ILogger<ProxyBidService> _logger;

        public ProxyBidService(
            ApplicationDbContext context,
            IHubContext<AuctionHub> hubContext,
            IEmailService emailService,
            ILogger<ProxyBidService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task ProcessProxyBidsAsync(int auctionId, string lastBidderId, decimal currentPrice)
        {
            var auction = await _context.AuctionItems.FindAsync(auctionId);
            if (auction == null || auction.Status != AuctionStatus.Active) return;

            // Lấy proxy bid cao nhất của người khác (không phải người vừa bid)
            var bestProxy = await _context.ProxyBids
                .Include(p => p.User)
                .Where(p => p.AuctionItemId == auctionId
                         && p.IsActive
                         && p.UserId != lastBidderId
                         && p.MaxAmount > currentPrice)
                .OrderByDescending(p => p.MaxAmount)
                .FirstOrDefaultAsync();

            if (bestProxy == null) return;

            // Tính giá auto bid = currentPrice + bước giá
            var autoBidAmount = currentPrice + auction.MinBidIncrement;

            // Nếu giá auto bid vượt max thì đặt đúng max
            if (autoBidAmount > bestProxy.MaxAmount)
                autoBidAmount = bestProxy.MaxAmount;

            if (autoBidAmount <= currentPrice) return;

            // Đặt bid tự động
            var bid = new Bid
            {
                AuctionItemId = auctionId,
                UserId = bestProxy.UserId,
                Amount = autoBidAmount,
                BidTime = DateTime.Now
            };

            auction.CurrentPrice = autoBidAmount;
            auction.WinnerUserId = bestProxy.UserId;

            _context.Bids.Add(bid);
            await _context.SaveChangesAsync();

            // Thông báo realtime
            await _hubContext.Clients.Group($"auction_{auctionId}").SendAsync("BidPlaced", new
            {
                auctionId,
                amount = autoBidAmount,
                bidderName = (bestProxy.User?.FullName ?? "Ẩn danh") + " (tự động)",
                bidTime = bid.BidTime.ToString("HH:mm:ss dd/MM/yyyy"),
                currentPrice = autoBidAmount,
                newEndTime = (string?)null,
                extended = false
            });

            // Email thông báo người bị vượt giá
            var outbidUser = await _context.Users.FindAsync(lastBidderId);
            if (outbidUser?.Email != null)
            {
                var auctionItem = await _context.AuctionItems.FindAsync(auctionId);
                await _emailService.SendOutbidEmailAsync(
                    outbidUser.Email,
                    outbidUser.FullName,
                    auctionItem?.Title ?? "",
                    autoBidAmount
                );
            }

            _logger.LogInformation($"Proxy bid placed: {autoBidAmount:N0} by {bestProxy.UserId} on auction {auctionId}");
        }
    }
}
