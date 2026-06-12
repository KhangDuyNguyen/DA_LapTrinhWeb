using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using AuctionSystem.Models;
using AuctionSystem.Services;

namespace AuctionSystem.Controllers
{
	public class AccountController : Controller
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly IEmailService _emailService;
		private readonly IMemoryCache _cache;

		public AccountController(
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			IEmailService emailService,
			IMemoryCache cache)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_emailService = emailService;
			_cache = cache;
		}

		public IActionResult Register() => View();

		[HttpPost]
		public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
		{
			var email = req.Email?.Trim().ToLowerInvariant();

			if (string.IsNullOrWhiteSpace(email))
				return Json(new { success = false, message = "Email không hợp lệ" });

			var existing = await _userManager.FindByEmailAsync(email);
			if (existing != null)
				return Json(new { success = false, message = "Email này đã được đăng ký" });

			var otpKey = $"otp_{email}";
			var cooldownKey = $"otp_cooldown_{email}";

			if (_cache.TryGetValue<DateTimeOffset>(cooldownKey, out var nextAllowedTime))
			{
				var remainingSeconds = (int)Math.Ceiling((nextAllowedTime - DateTimeOffset.UtcNow).TotalSeconds);

				if (remainingSeconds > 0)
				{
					return Json(new
					{
						success = false,
						message = $"Vui lòng đợi {remainingSeconds} giây để gửi lại mã.",
						cooldownSeconds = remainingSeconds
					});
				}
			}

			var otp = Random.Shared.Next(100000, 1000000).ToString();

			_cache.Set(otpKey, otp, TimeSpan.FromMinutes(5));

			var nextSendTime = DateTimeOffset.UtcNow.AddMinutes(2);
			_cache.Set(cooldownKey, nextSendTime, TimeSpan.FromMinutes(2));

			await _emailService.SendOtpEmailAsync(email, otp);

			return Json(new
			{
				success = true,
				cooldownSeconds = 120,
				expiresInSeconds = 300
			});
		}

		[HttpPost]
		public async Task<IActionResult> VerifyOtpAndRegister([FromBody] VerifyOtpRequest req)
		{
			var email = req.Email?.Trim().ToLowerInvariant();

			if (string.IsNullOrWhiteSpace(email))
				return Json(new { success = false, message = "Email không hợp lệ" });

			if (string.IsNullOrWhiteSpace(req.Otp))
				return Json(new { success = false, message = "Vui lòng nhập mã OTP" });

			var cacheKey = $"otp_{email}";
			var cooldownKey = $"otp_cooldown_{email}";

			if (!_cache.TryGetValue(cacheKey, out string? savedOtp) || savedOtp != req.Otp)
				return Json(new { success = false, message = "Mã xác minh không đúng hoặc đã hết hạn" });

			_cache.Remove(cacheKey);
			_cache.Remove(cooldownKey);

			var user = new ApplicationUser
			{
				UserName = email,
				Email = email,
				FullName = req.FullName,
				EmailConfirmed = true
			};

			var result = await _userManager.CreateAsync(user, req.Password);

			if (!result.Succeeded)
			{
				var errors = string.Join(", ", result.Errors.Select(e => e.Description));
				return Json(new { success = false, message = errors });
			}

			await _signInManager.SignInAsync(user, isPersistent: false);

			return Json(new { success = true, redirectUrl = "/" });
		}

		public IActionResult Login(string? returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
		{
			if (!ModelState.IsValid)
				return View(model);

			var result = await _signInManager.PasswordSignInAsync(
				model.Email,
				model.Password,
				model.RememberMe,
				lockoutOnFailure: true);

			if (result.Succeeded)
			{
				if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
					return Redirect(returnUrl);

				return RedirectToAction("Index", "Home");
			}

			if (result.IsLockedOut)
				ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa tạm thời. Thử lại sau 15 phút.");
			else
				ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng");

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return RedirectToAction("Index", "Home");
		}

		[Microsoft.AspNetCore.Authorization.Authorize]
		public async Task<IActionResult> Profile()
		{
			var user = await _userManager.GetUserAsync(User);

			var model = new ProfileViewModel
			{
				FullName = user?.FullName ?? "",
				Email = user?.Email ?? "",
				PhoneNumber = user?.PhoneNumber ?? "",
				IsVerified = user?.EmailConfirmed ?? false
			};

			return View(model);
		}

		[Microsoft.AspNetCore.Authorization.Authorize]
		[HttpPost]
		public async Task<IActionResult> Profile(ProfileViewModel model)
		{
			if (!ModelState.IsValid)
				return View(model);

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return NotFound();

			user.FullName = model.FullName;
			user.PhoneNumber = model.PhoneNumber;

			await _userManager.UpdateAsync(user);

			TempData["Success"] = "Cập nhật thông tin thành công!";
			return RedirectToAction(nameof(Profile));
		}

		[Microsoft.AspNetCore.Authorization.Authorize]
		public IActionResult ChangePassword() => View();

		[Microsoft.AspNetCore.Authorization.Authorize]
		[HttpPost]
		public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
		{
			if (!ModelState.IsValid)
				return View(model);

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return NotFound();

			var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

			if (!result.Succeeded)
			{
				foreach (var error in result.Errors)
					ModelState.AddModelError(string.Empty, error.Description);

				return View(model);
			}

			TempData["Success"] = "Đổi mật khẩu thành công!";
			return RedirectToAction(nameof(Profile));
		}

		public IActionResult Documents() => View();

		public IActionResult VerifyIdentity() => View();
	}

	public class SendOtpRequest
	{
		public string Email { get; set; } = "";
	}

	public class VerifyOtpRequest
	{
		public string Email { get; set; } = "";
		public string FullName { get; set; } = "";
		public string Password { get; set; } = "";
		public string Otp { get; set; } = "";
	}
}