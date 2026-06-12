using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AuctionSystem.Models
{
	public class RegisterViewModel
	{
		[Required(ErrorMessage = "Họ tên là bắt buộc")]
		[Display(Name = "Họ và tên")]
		public string FullName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Email là bắt buộc")]
		[EmailAddress(ErrorMessage = "Email không hợp lệ")]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = "Mật khẩu là bắt buộc")]
		[StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
		[DataType(DataType.Password)]
		public string Password { get; set; } = string.Empty;

		[DataType(DataType.Password)]
		[Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
		public string ConfirmPassword { get; set; } = string.Empty;
	}

	public class LoginViewModel
	{
		[Required(ErrorMessage = "Email là bắt buộc")]
		[EmailAddress]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = "Mật khẩu là bắt buộc")]
		[DataType(DataType.Password)]
		public string Password { get; set; } = string.Empty;

		[Display(Name = "Ghi nhớ đăng nhập")]
		public bool RememberMe { get; set; }
	}

	public class ProfileViewModel
	{
		[Required(ErrorMessage = "Họ tên là bắt buộc")]
		public string FullName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string? PhoneNumber { get; set; }
		public string? IdentityNumber { get; set; }
		public string? TaxCode { get; set; }
		public string? Address { get; set; }
		public string? Province { get; set; }
		public string? District { get; set; }
		public DateTime? IdentityDate { get; set; }
		public string? IdentityPlace { get; set; }
		public string? CccdFrontUrl { get; set; }
		public string? CccdBackUrl { get; set; }
		public bool IsVerified { get; set; }
	}

	public class ChangePasswordViewModel
	{
		[Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
		[DataType(DataType.Password)]
		public string CurrentPassword { get; set; } = string.Empty;

		[Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
		[StringLength(100, MinimumLength = 6)]
		[DataType(DataType.Password)]
		public string NewPassword { get; set; } = string.Empty;

		[DataType(DataType.Password)]
		[Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
		public string ConfirmNewPassword { get; set; } = string.Empty;
	}

	public class CreateAuctionViewModel
	{
		[Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
		public string Title { get; set; } = string.Empty;

		[Required(ErrorMessage = "Mô tả là bắt buộc")]
		public string Description { get; set; } = string.Empty;

		[Required]
		[Range(1000, double.MaxValue, ErrorMessage = "Giá khởi điểm tối thiểu 1,000 VNĐ")]
		public decimal StartingPrice { get; set; }

		[Required]
		[Range(1000, double.MaxValue)]
		public decimal MinBidIncrement { get; set; } = 10000;

		[Required]
		public DateTime StartTime { get; set; } = DateTime.Now.AddMinutes(5);

		[Required]
		public DateTime EndTime { get; set; } = DateTime.Now.AddHours(1);

		public int? CategoryId { get; set; }
		public SelectList? Categories { get; set; }
	}

	public class SetProxyBidViewModel
	{
		public int AuctionItemId { get; set; }
		public string AuctionTitle { get; set; } = string.Empty;
		public decimal CurrentPrice { get; set; }
		public decimal MinBidIncrement { get; set; }

		[Required]
		[Range(1000, double.MaxValue)]
		public decimal MaxAmount { get; set; }
	}

	public class AuctionSearchViewModel
	{
		public string? Keyword { get; set; }
		public int? CategoryId { get; set; }
		public string? Status { get; set; }
		public decimal? MinPrice { get; set; }
		public decimal? MaxPrice { get; set; }
		public string? SortBy { get; set; } = "newest";
		public List<AuctionItem> Results { get; set; } = new();
		public List<AuctionCategory> Categories { get; set; } = new();
		public int TotalCount { get; set; }
	}
}