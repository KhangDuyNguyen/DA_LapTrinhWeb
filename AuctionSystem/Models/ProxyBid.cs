using System.ComponentModel.DataAnnotations;

namespace AuctionSystem.Models
{
	// Model lưu proxy bid (đặt giá tối đa tự động)
	public class ProxyBid
	{
		public int Id { get; set; }
		public int AuctionItemId { get; set; }
		public AuctionItem? AuctionItem { get; set; }
		public string? UserId { get; set; }
		public ApplicationUser? User { get; set; }

		[Range(1000, double.MaxValue)]
		public decimal MaxAmount { get; set; }   // Giá tối đa user chấp nhận

		public bool IsActive { get; set; } = true;
		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}
