using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using AuctionSystem.Models;
using AuctionSystem.Services;

namespace AuctionSystem.Controllers
{
	public class AuctionController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IProxyBidService _proxyBidService;

		public AuctionController(
			ApplicationDbContext context,
			UserManager<ApplicationUser> userManager,
			IProxyBidService proxyBidService)
		{
			_context = context;
			_userManager = userManager;
			_proxyBidService = proxyBidService;
		}

		// Trang chủ danh sách + tìm kiếm + lọc
		public async Task<IActionResult> Index(AuctionSearchViewModel search)
		{
			await UpdateAuctionStatuses();

			var query = _context.AuctionItems
				.Include(a => a.CreatedByUser)
				.Include(a => a.Category)
				.Include(a => a.Bids)
				.Where(a => a.IsApproved)
				.AsQueryable();

			// Tìm kiếm
			if (!string.IsNullOrWhiteSpace(search.Keyword))
				query = query.Where(a => a.Title.Contains(search.Keyword) || a.Description.Contains(search.Keyword));

			// Lọc danh mục
			if (search.CategoryId.HasValue)
				query = query.Where(a => a.CategoryId == search.CategoryId);

			// Lọc trạng thái
			if (!string.IsNullOrEmpty(search.Status))
			{
				if (Enum.TryParse<AuctionStatus>(search.Status, out var status))
					query = query.Where(a => a.Status == status);
			}

			// Lọc giá
			if (search.MinPrice.HasValue)
				query = query.Where(a => a.CurrentPrice >= search.MinPrice);
			if (search.MaxPrice.HasValue)
				query = query.Where(a => a.CurrentPrice <= search.MaxPrice);

			// Sắp xếp
			query = search.SortBy switch
			{
				"price_asc" => query.OrderBy(a => a.CurrentPrice),
				"price_desc" => query.OrderByDescending(a => a.CurrentPrice),
				"ending_soon" => query.Where(a => a.Status == AuctionStatus.Active).OrderBy(a => a.EndTime),
				"most_bids" => query.OrderByDescending(a => a.Bids!.Count),
				_ => query.OrderByDescending(a => a.CreatedAt)
			};

			search.TotalCount = await query.CountAsync();
			search.Results = await query.Take(50).ToListAsync();
			search.Categories = await _context.AuctionCategories.ToListAsync();

			return View(search);
		}

		// Chi tiết
		public async Task<IActionResult> Detail(int id)
		{
			await UpdateAuctionStatuses();
			var auction = await _context.AuctionItems
				.Include(a => a.CreatedByUser)
				.Include(a => a.Winner)
				.Include(a => a.Category)
				.Include(a => a.Bids!).ThenInclude(b => b.User)
				.FirstOrDefaultAsync(a => a.Id == id);

			if (auction == null) return NotFound();

			// Proxy bid của user hiện tại
			if (User.Identity?.IsAuthenticated == true)
			{
				var userId = _userManager.GetUserId(User);
				var myProxy = await _context.ProxyBids
					.Where(p => p.AuctionItemId == id && p.UserId == userId && p.IsActive)
					.FirstOrDefaultAsync();
				ViewBag.MyProxyBid = myProxy;
			}

			return View(auction);
		}

		// Tạo phiên
		[Authorize]
		public async Task<IActionResult> Create()
		{
			var categories = await _context.AuctionCategories.ToListAsync();
			return View(new CreateAuctionViewModel
			{
				StartTime = DateTime.Now.AddMinutes(5),
				EndTime = DateTime.Now.AddHours(1),
				Categories = new SelectList(categories, "Id", "Name")
			});
		}

		[Authorize]
		[HttpPost]
		public async Task<IActionResult> Create(CreateAuctionViewModel model, IFormFile? imageFile)
		{
			var categories = await _context.AuctionCategories.ToListAsync();
			model.Categories = new SelectList(categories, "Id", "Name");

			if (!ModelState.IsValid) return View(model);

			if (model.EndTime <= model.StartTime)
			{
				ModelState.AddModelError("EndTime", "Thời gian kết thúc phải sau thời gian bắt đầu");
				return View(model);
			}

			var user = await _userManager.GetUserAsync(User);
			string? imageUrl = null;

			// === FIX UPLOAD: tên file random + validate extension ===
			if (imageFile != null)
			{
				var ext = Path.GetExtension(imageFile.FileName).ToLower();
				var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

				if (!allowedExt.Contains(ext))
				{
					ModelState.AddModelError("", "Chỉ cho phép file ảnh: jpg, jpeg, png, webp, gif");
					return View(model);
				}

				if (imageFile.Length > 5 * 1024 * 1024)
				{
					ModelState.AddModelError("", "File ảnh không được vượt quá 5MB");
					return View(model);
				}

				var fileName = $"{Guid.NewGuid()}{ext}";
				var savePath = Path.Combine("wwwroot/images", fileName);
				using var stream = new FileStream(savePath, FileMode.Create);
				await imageFile.CopyToAsync(stream);
				imageUrl = "/images/" + fileName;
			}

			var auction = new AuctionItem
			{
				Title = model.Title,
				Description = model.Description,
				StartingPrice = model.StartingPrice,
				CurrentPrice = model.StartingPrice,
				MinBidIncrement = model.MinBidIncrement,
				StartTime = model.StartTime,
				EndTime = model.EndTime,
				ImageUrl = imageUrl,
				CategoryId = model.CategoryId,
				CreatedByUserId = user!.Id,
				IsApproved = false, // Chờ admin duyệt
				Status = AuctionStatus.Pending
			};

			_context.AuctionItems.Add(auction);
			await _context.SaveChangesAsync();

			TempData["Success"] = "Phiên đấu giá đã được tạo và đang chờ admin duyệt!";
			return RedirectToAction(nameof(MyAuctions));
		}

		// Đặt proxy bid
		[Authorize]
		public async Task<IActionResult> SetProxyBid(int id)
		{
			var auction = await _context.AuctionItems.FindAsync(id);
			if (auction == null || auction.Status != AuctionStatus.Active)
				return NotFound();

			var userId = _userManager.GetUserId(User);
			var existing = await _context.ProxyBids
				.Where(p => p.AuctionItemId == id && p.UserId == userId && p.IsActive)
				.FirstOrDefaultAsync();

			return View(new SetProxyBidViewModel
			{
				AuctionItemId = id,
				AuctionTitle = auction.Title,
				CurrentPrice = auction.CurrentPrice,
				MinBidIncrement = auction.MinBidIncrement,
				MaxAmount = existing?.MaxAmount ?? (auction.CurrentPrice + auction.MinBidIncrement * 5)
			});
		}

		[Authorize]
		[HttpPost]
		public async Task<IActionResult> SetProxyBid(SetProxyBidViewModel model)
		{
			if (!ModelState.IsValid) return View(model);

			var auction = await _context.AuctionItems.FindAsync(model.AuctionItemId);
			if (auction == null) return NotFound();

			if (model.MaxAmount <= auction.CurrentPrice)
			{
				ModelState.AddModelError("MaxAmount", "Giá tối đa phải lớn hơn giá hiện tại");
				model.AuctionTitle = auction.Title;
				model.CurrentPrice = auction.CurrentPrice;
				return View(model);
			}

			var userId = _userManager.GetUserId(User);

			// Hủy proxy bid cũ
			var existing = await _context.ProxyBids
				.Where(p => p.AuctionItemId == model.AuctionItemId && p.UserId == userId)
				.ToListAsync();
			foreach (var p in existing) p.IsActive = false;

			// Tạo proxy bid mới
			_context.ProxyBids.Add(new ProxyBid
			{
				AuctionItemId = model.AuctionItemId,
				UserId = userId,
				MaxAmount = model.MaxAmount,
				IsActive = true
			});

			await _context.SaveChangesAsync();

			TempData["Success"] = $"Đã đặt giá tự động tối đa {model.MaxAmount:N0} VNĐ!";
			return RedirectToAction(nameof(Detail), new { id = model.AuctionItemId });
		}

		// Lịch sử bid
		[Authorize]
		public async Task<IActionResult> MyBids()
		{
			var user = await _userManager.GetUserAsync(User);
			var bids = await _context.Bids
				.Include(b => b.AuctionItem).ThenInclude(a => a!.Category)
				.Where(b => b.UserId == user!.Id)
				.OrderByDescending(b => b.BidTime)
				.ToListAsync();
			return View(bids);
		}

		// Phiên của tôi
		[Authorize]
		public async Task<IActionResult> MyAuctions()
		{
			var user = await _userManager.GetUserAsync(User);
			var auctions = await _context.AuctionItems
				.Include(a => a.Bids)
				.Include(a => a.Category)
				.Where(a => a.CreatedByUserId == user!.Id)
				.OrderByDescending(a => a.CreatedAt)
				.ToListAsync();
			return View(auctions);
		}

		private async Task UpdateAuctionStatuses()
		{
			var now = DateTime.Now;
			var items = await _context.AuctionItems
				.Where(a => a.Status != AuctionStatus.Ended && a.IsApproved)
				.ToListAsync();
			bool changed = false;
			foreach (var a in items)
			{
				if (now >= a.StartTime && now < a.EndTime && a.Status == AuctionStatus.Pending)
				{ a.Status = AuctionStatus.Active; changed = true; }
				else if (now >= a.EndTime && a.Status != AuctionStatus.Ended)
				{ a.Status = AuctionStatus.Ended; changed = true; }
			}
			if (changed) await _context.SaveChangesAsync();
		}
	}
}
