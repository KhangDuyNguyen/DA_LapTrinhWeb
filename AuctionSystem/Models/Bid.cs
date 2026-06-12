namespace AuctionSystem.Models
{
    public class Bid
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime BidTime { get; set; } = DateTime.Now;

        public int AuctionItemId { get; set; }
        public AuctionItem? AuctionItem { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
