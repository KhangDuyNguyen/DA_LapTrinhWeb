using System.ComponentModel.DataAnnotations;

namespace AuctionSystem.Models
{
	public class AuctionCategory
	{
		public int Id { get; set; }

		[Required]
		[StringLength(100)]
		public string Name { get; set; } = string.Empty;

		public string? Icon { get; set; } = "fa-tag";

		public List<AuctionItem>? AuctionItems { get; set; }
	}
}
