using Microsoft.AspNetCore.Identity;

namespace AuctionSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<AuctionItem>? AuctionItems { get; set; }
        public List<Bid>? Bids { get; set; }
    }
}
