using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Models
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options) { }

		public DbSet<AuctionItem> AuctionItems { get; set; }
		public DbSet<Bid> Bids { get; set; }
		public DbSet<ProxyBid> ProxyBids { get; set; }
		public DbSet<AuctionCategory> AuctionCategories { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<AuctionItem>()
				.Property(a => a.StartingPrice).HasColumnType("decimal(18,2)");
			builder.Entity<AuctionItem>()
				.Property(a => a.CurrentPrice).HasColumnType("decimal(18,2)");
			builder.Entity<AuctionItem>()
				.Property(a => a.MinBidIncrement).HasColumnType("decimal(18,2)");
			builder.Entity<Bid>()
				.Property(b => b.Amount).HasColumnType("decimal(18,2)");
			builder.Entity<ProxyBid>()
				.Property(p => p.MaxAmount).HasColumnType("decimal(18,2)");

			builder.Entity<AuctionItem>()
				.HasOne(a => a.CreatedByUser)
				.WithMany(u => u.AuctionItems)
				.HasForeignKey(a => a.CreatedByUserId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<AuctionItem>()
				.HasOne(a => a.Winner)
				.WithMany()
				.HasForeignKey(a => a.WinnerUserId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Bid>()
				.HasOne(b => b.User)
				.WithMany(u => u.Bids)
				.HasForeignKey(b => b.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Bid>()
				.HasOne(b => b.AuctionItem)
				.WithMany(a => a.Bids)
				.HasForeignKey(b => b.AuctionItemId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<ProxyBid>()
				.HasOne(p => p.AuctionItem)
				.WithMany()
				.HasForeignKey(p => p.AuctionItemId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<ProxyBid>()
				.HasOne(p => p.User)
				.WithMany()
				.HasForeignKey(p => p.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			// Seed categories
			builder.Entity<AuctionCategory>().HasData(
				new AuctionCategory { Id = 1, Name = "Điện thoại", Icon = "fa-mobile-alt" },
				new AuctionCategory { Id = 2, Name = "Laptop", Icon = "fa-laptop" },
				new AuctionCategory { Id = 3, Name = "Đồng hồ", Icon = "fa-clock" },
				new AuctionCategory { Id = 4, Name = "Thời trang", Icon = "fa-tshirt" },
				new AuctionCategory { Id = 5, Name = "Xe cộ", Icon = "fa-car" },
				new AuctionCategory { Id = 6, Name = "Đồ cổ", Icon = "fa-gem" },
				new AuctionCategory { Id = 7, Name = "Khác", Icon = "fa-box" }
			);
		}
	}
}
