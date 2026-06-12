using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuctionSystem.Models;

namespace AuctionSystem.Controllers
{
	public class HomeController : Controller
	{
		private readonly ApplicationDbContext _context;

		public HomeController(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			var categories = await _context.AuctionCategories.ToListAsync();

			var auctions = await _context.AuctionItems
				.Include(a => a.CreatedByUser)
				.Include(a => a.Category)
				.Include(a => a.Bids)
				.Where(a => a.IsApproved && a.Status == AuctionStatus.Active)
				.OrderBy(a => a.EndTime)
				.Take(20)
				.ToListAsync();

			var model = new AuctionSearchViewModel
			{
				Results = auctions,
				Categories = categories,
				TotalCount = auctions.Count,
				Status = "Active"
			};

			return View(model);
		}
	}
}