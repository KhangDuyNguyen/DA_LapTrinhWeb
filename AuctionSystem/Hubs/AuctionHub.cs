using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using AuctionSystem.Models;

namespace AuctionSystem.Hubs
{
	[Authorize]
	public class AuctionHub : Hub
	{
		private readonly ApplicationDbContext _context;
		private readonly IMemoryCache _cache;

		public AuctionHub(ApplicationDbContext context, IMemoryCache cache)
		{
			_context = context;
			_cache = cache;
		}

		public async Task JoinAuction(int auctionId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"auction_{auctionId}");
		}

		public async Task LeaveAuction(int auctionId)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction_{auctionId}");
		}

		public async Task PlaceBid(int auctionId, decimal amount)
		{
			var userId = Context.UserIdentifier;
			if (userId == null)
			{
				await Clients.Caller.SendAsync("BidError", "Bạn chưa đăng nhập");
				return;
			}

			// === RATE LIMIT: 5 giây / lần ===
			var rateLimitKey = $"bid_rate_{userId}";
			if (_cache.TryGetValue(rateLimitKey, out _))
			{
				await Clients.Caller.SendAsync("BidError", "Đặt giá quá nhanh, vui lòng chờ 5 giây");
				return;
			}
			_cache.Set(rateLimitKey, true, TimeSpan.FromSeconds(5));

			// === VALIDATE INPUT ===
			if (amount <= 0 || amount > 999_000_000_000)
			{
				await Clients.Caller.SendAsync("BidError", "Giá thầu không hợp lệ");
				return;
			}

			// === TRANSACTION để tránh race condition ===
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				// Lock row với UPDLOCK để tránh race condition
				var auction = await _context.AuctionItems
					.FromSqlRaw("SELECT * FROM AuctionItems WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", auctionId)
					.Include(a => a.Bids)
					.FirstOrDefaultAsync();

				if (auction == null)
				{
					await Clients.Caller.SendAsync("BidError", "Phiên đấu giá không tồn tại");
					return;
				}

				var now = DateTime.Now;

				// Cập nhật status nếu cần
				if (now >= auction.StartTime && now < auction.EndTime)
					auction.Status = AuctionStatus.Active;
				else if (now >= auction.EndTime)
					auction.Status = AuctionStatus.Ended;

				if (auction.Status != AuctionStatus.Active)
				{
					await Clients.Caller.SendAsync("BidError", "Phiên đấu giá không còn hoạt động");
					return;
				}

				if (auction.CreatedByUserId == userId)
				{
					await Clients.Caller.SendAsync("BidError", "Bạn không thể đấu giá sản phẩm của chính mình");
					return;
				}

				var minValidBid = auction.CurrentPrice + auction.MinBidIncrement;
				if (amount < minValidBid)
				{
					await Clients.Caller.SendAsync("BidError",
						$"Giá thầu tối thiểu là {minValidBid:N0} VNĐ");
					return;
				}

				var user = await _context.Users.FindAsync(userId);

				// Lưu bid
				var bid = new Bid
				{
					AuctionItemId = auctionId,
					UserId = userId,
					Amount = amount,
					BidTime = now
				};

				auction.CurrentPrice = amount;
				auction.WinnerUserId = userId;

				// === AUTO-EXTEND: nếu bid trong 3 phút cuối, gia hạn thêm 5 phút ===
				var timeLeft = auction.EndTime - now;
				bool extended = false;
				if (timeLeft.TotalMinutes <= 3)
				{
					auction.EndTime = now.AddMinutes(5);
					extended = true;
				}

				_context.Bids.Add(bid);
				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				// Broadcast cho cả phòng
				await Clients.Group($"auction_{auctionId}").SendAsync("BidPlaced", new
				{
					auctionId,
					amount,
					bidderName = user?.FullName ?? user?.Email ?? "Ẩn danh",
					bidTime = bid.BidTime.ToString("HH:mm:ss dd/MM/yyyy"),
					currentPrice = auction.CurrentPrice,
					newEndTime = extended ? auction.EndTime.ToString("o") : (string?)null,
					extended
				});

				// Nếu gia hạn, thông báo cho cả phòng
				if (extended)
				{
					await Clients.Group($"auction_{auctionId}").SendAsync("AuctionExtended", new
					{
						newEndTime = auction.EndTime.ToString("o"),
						message = "⏱ Phiên đấu giá được gia hạn thêm 5 phút!"
					});
				}
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				await Clients.Caller.SendAsync("BidError", "Lỗi hệ thống, vui lòng thử lại");
			}
		}
	}
}
