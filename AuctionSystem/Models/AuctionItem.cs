using System.ComponentModel.DataAnnotations;

namespace AuctionSystem.Models
{
	public enum AuctionStatus { Pending, Active, Ended }

	public class AuctionItem
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
		[StringLength(200)]
		public string Title { get; set; } = string.Empty;

		[Required(ErrorMessage = "Mô tả là bắt buộc")]
		public string Description { get; set; } = string.Empty;

		[Required]
		[Range(1000, double.MaxValue, ErrorMessage = "Giá khởi điểm tối thiểu 1,000 VNĐ")]
		public decimal StartingPrice { get; set; }

		public decimal CurrentPrice { get; set; }

		[Range(1000, double.MaxValue)]
		public decimal MinBidIncrement { get; set; } = 10000;

		public string? ImageUrl { get; set; }

		[Required]
		public DateTime StartTime { get; set; }

		[Required]
		public DateTime EndTime { get; set; }

		public AuctionStatus Status { get; set; } = AuctionStatus.Pending;

		// Admin duyệt trước khi hiển thị
		public bool IsApproved { get; set; } = false;

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		// Danh mục
		public int? CategoryId { get; set; }
		public AuctionCategory? Category { get; set; }

		public string? CreatedByUserId { get; set; }
		public ApplicationUser? CreatedByUser { get; set; }

		public string? WinnerUserId { get; set; }
		public ApplicationUser? Winner { get; set; }

		public List<Bid>? Bids { get; set; }
	}
}
