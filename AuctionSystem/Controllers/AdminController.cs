using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AuctionSystem.Models;

namespace AuctionSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TotalAuctions = await _context.AuctionItems.CountAsync();
            ViewBag.PendingApproval = await _context.AuctionItems.CountAsync(a => !a.IsApproved);
            ViewBag.ActiveAuctions = await _context.AuctionItems.CountAsync(a => a.Status == AuctionStatus.Active);
            ViewBag.TotalBids = await _context.Bids.CountAsync();

            var recentAuctions = await _context.AuctionItems
                .Include(a => a.CreatedByUser)
                .Include(a => a.Bids)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync();

            return View(recentAuctions);
        }

        // Danh sách phiên chờ duyệt
        public async Task<IActionResult> PendingAuctions()
        {
            var auctions = await _context.AuctionItems
                .Include(a => a.CreatedByUser)
                .Include(a => a.Category)
                .Where(a => !a.IsApproved)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(auctions);
        }

        // Duyệt phiên
        [HttpPost]
        public async Task<IActionResult> ApproveAuction(int id)
        {
            var auction = await _context.AuctionItems.FindAsync(id);
            if (auction == null) return NotFound();

            auction.IsApproved = true;
            if (DateTime.Now >= auction.StartTime && DateTime.Now < auction.EndTime)
                auction.Status = AuctionStatus.Active;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã duyệt phiên đấu giá: {auction.Title}";
            return RedirectToAction(nameof(PendingAuctions));
        }

        // Từ chối phiên
        [HttpPost]
        public async Task<IActionResult> RejectAuction(int id)
        {
            var auction = await _context.AuctionItems.FindAsync(id);
            if (auction == null) return NotFound();

            _context.AuctionItems.Remove(auction);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa phiên đấu giá";
            return RedirectToAction(nameof(PendingAuctions));
        }

        // Tất cả phiên đấu giá
        public async Task<IActionResult> AllAuctions()
        {
            var auctions = await _context.AuctionItems
                .Include(a => a.CreatedByUser)
                .Include(a => a.Category)
                .Include(a => a.Bids)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(auctions);
        }

        // Xóa phiên
        [HttpPost]
        public async Task<IActionResult> DeleteAuction(int id)
        {
            var auction = await _context.AuctionItems.FindAsync(id);
            if (auction != null)
            {
                _context.AuctionItems.Remove(auction);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Đã xóa phiên đấu giá";
            return RedirectToAction(nameof(AllAuctions));
        }

        // Danh sách users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // Cấp quyền Admin
        [HttpPost]
        public async Task<IActionResult> MakeAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            await _userManager.AddToRoleAsync(user, "Admin");
            TempData["Success"] = $"Đã cấp quyền Admin cho {user.FullName}";
            return RedirectToAction(nameof(Users));
        }

        // Thu hồi quyền Admin
        [HttpPost]
        public async Task<IActionResult> RemoveAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userManager.RemoveFromRoleAsync(user, "Admin");
            TempData["Success"] = $"Đã thu hồi quyền Admin của {user.FullName}";
            return RedirectToAction(nameof(Users));
        }

        // Khóa tài khoản
        [HttpPost]
        public async Task<IActionResult> LockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            TempData["Success"] = $"Đã khóa tài khoản {user.FullName}";
            return RedirectToAction(nameof(Users));
        }

        // Mở khóa tài khoản
        [HttpPost]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["Success"] = $"Đã mở khóa tài khoản {user.FullName}";
            return RedirectToAction(nameof(Users));
        }

        // Quản lý danh mục
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.AuctionCategories.ToListAsync();
            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(string name, string icon)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _context.AuctionCategories.Add(new AuctionCategory
                {
                    Name = name,
                    Icon = string.IsNullOrWhiteSpace(icon) ? "fa-tag" : icon
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm danh mục";
            }
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _context.AuctionCategories.FindAsync(id);
            if (cat != null)
            {
                _context.AuctionCategories.Remove(cat);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Categories));
        }
    }
}
